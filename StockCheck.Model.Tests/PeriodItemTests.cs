using NUnit.Framework;
using Ploeh.AutoFixture;
using StockCheck.ModelFs;

namespace StockCheck.Model.Tests
{
    [TestFixture()]
    public class WhenAPeriodItemIsConstructed
    {
        [Test()]
        public void TheItemsReceivedCollectionIsNotNull()
        {
            var fixture = new Fixture();
            var periodItem = new PeriodItem(fixture.Create<SalesItem>());

            Assert.IsNotNull(periodItem.ItemsReceived);
        }
    }

    [TestFixture]
    public class WhenGoodsAreReceived
    {
        [Test]
        public void TheGoodsAreAddedToTheItemsReceivedCollection()
        {
            var fixture = new Fixture();
            var periodItem = new PeriodItem(fixture.Create<SalesItem>());
            var itemReceived = fixture.Create<ItemReceived>();

            periodItem.ReceiveItems(itemReceived.ReceivedDate,
                                    itemReceived.Quantity,
                                    itemReceived.InvoicedAmountEx,
                                    itemReceived.InvoicedAmountInc);

            Assert.AreEqual(1, periodItem.ItemsReceived.Count);
        }
    }

    [TestFixture]
    public class WhenAPeriodItemIsCopiedForTheNextPeriod
    {
        protected Fixture _fixture;

        public WhenAPeriodItemIsCopiedForTheNextPeriod()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void TheNewInstanceIsNotTheSourceInstance()
        {
            var periodItem1 = _fixture.Create<PeriodItem>();
            var periodItem2 = periodItem1.CopyForNextPeriod();

            Assert.AreNotSame(periodItem1, periodItem2);
        }

        [Test]
        public void TheNewInstanceOpeningStockMatchesTheSourceInstanceClosingStock()
        {
            var periodItem1 = _fixture.Create<PeriodItem>();
            var periodItem2 = periodItem1.CopyForNextPeriod();

            Assert.AreEqual(periodItem1.ClosingStock, periodItem2.OpeningStock);
        }

        [Test]
        public void TheNewInstanceSalesItemIsTheSameAsTheSourceInstanceSalesItem()
        {
            var periodItem1 = _fixture.Create<PeriodItem>();
            var periodItem2 = periodItem1.CopyForNextPeriod();

            Assert.AreSame(periodItem1.SalesItem, periodItem2.SalesItem);
        }
    }

    [TestFixture]
    public class WhenAPeriodItemHasDraughtItemsReceived
    {
        protected Fixture _fixture;
        protected PeriodItem _target;
        protected SalesItem _salesItem;

        public void WhenAPeriodItemHasItemsReceived()
        {
        }

        [Test]
        public void TheContainersReceivedAmountIsCorrect()
        {
            _fixture = new Fixture();
            _salesItem = _fixture.Create<SalesItem>();
            _target = new PeriodItem(_salesItem);
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountEx = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });

            Assert.AreEqual(2 + 1 + 1, _target.ContainersReceived);
        }

        [Test]
        public void TheInvoicedAmountExIsCorrect()
        {
            _fixture = new Fixture();
            _salesItem = _fixture.Create<SalesItem>();
            _target = new PeriodItem(_salesItem);
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountEx = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });

            Assert.AreEqual(212.34M + 109.3M + 109.3M, _target.PurchasesEx);
        }

        [Test]
        public void TheInvoicedAmountIncIsCorrect()
        {
            _fixture = new Fixture();
            _salesItem = _fixture.Create<SalesItem>();
            _target = new PeriodItem(_salesItem);
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountInc = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountInc = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountInc = 109.3M });

            Assert.AreEqual(212.34M + 109.3M + 109.3M, _target.PurchasesInc);
        }

        [Test]
        public void ThePurchasesTotalIsCorrect()
        {
            _fixture = new Fixture();
            _salesItem = _fixture.Create<SalesItem>();
            _salesItem.TaxRate = 0.2;
            _target = new PeriodItem(_salesItem);
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountEx = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountInc = 120M });

            Assert.AreEqual(212.34M + 109.3M + (120M/((decimal)(1.0 + _salesItem.TaxRate))), _target.PurchasesTotal);
        }

        [Test]
        public void TheSalesQuantityIsCorrect()
        {
            _fixture = new Fixture();
            _salesItem = _fixture.Create<SalesItem>();
            _salesItem.TaxRate = 0.2;
            _salesItem.ContainerSize = 11;
            _target = new PeriodItem(_salesItem);
            _target.OpeningStock = 23;
            _target.ClosingStock = 25;
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountEx = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountInc = 120M });

            Assert.AreEqual(23 + (4 * 11) - 25 , _target.Sales);
        }
    }
}