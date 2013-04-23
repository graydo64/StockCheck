using NUnit.Framework;
using Ploeh.AutoFixture;
using StockCheck.ModelFs;

namespace StockCheck.Model.Tests
{
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
    public class WhenAPeriodItemHasDraughtItemsReceived
    {
        protected Fixture _fixture;
        protected PeriodItem _target;
        protected SalesItem _salesItem;
        protected decimal _containerPrice;

        public void WhenAPeriodItemHasItemsReceived()
        {
            _fixture = new Fixture();
        }


        [Test]
        public void TheSalesIncIsCorrect()
        {
            _fixture = new Fixture();
            initialiseSalesItem();
            initialisePeriodItem();
            _target.ClosingStock = _target.OpeningStock;
            Assert.AreEqual(4 * 11 * 8 * 3.25M, _target.SalesInc);
        }

        [Test]
        public void TheSalesExIsCorrect()
        {
            _fixture = new Fixture();
            initialiseSalesItem();
            initialisePeriodItem();
            _target.ClosingStock = _target.OpeningStock;
            Assert.AreEqual((4 * 11 * 8 * 3.25M)/(1 + (decimal)_salesItem.TaxRate), _target.SalesEx);
        }

        [Test]
        public void TheCostOfSalesIsCorrect()
        {
            _fixture = new Fixture();
            initialiseSalesItem();
            initialisePeriodItem();

            var sales = _target.OpeningStock + _target.TotalUnits - _target.ClosingStock;
            Assert.AreEqual((decimal)(sales / _salesItem.ContainerSize * (float)_salesItem.CostPerContainer), _target.CostOfSalesEx);
        }

        private void initialiseSalesItem()
        {
            _salesItem = _fixture.Create<SalesItem>();
            _salesItem.TaxRate = 0.2;
            _salesItem.ContainerSize = 11;
            _salesItem.UnitOfSale = 1f / 8;
            _salesItem.SalesPrice = 3.25M;
        }

        private void initialisePeriodItem()
        {
            _target = new PeriodItem(_salesItem);
            _target.OpeningStock = 23;
            _target.ClosingStock = 25;
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 2, InvoicedAmountEx = 212.34M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
            _target.ItemsReceived.Add(new ItemReceived { Quantity = 1, InvoicedAmountEx = 109.3M });
        }
    }
}