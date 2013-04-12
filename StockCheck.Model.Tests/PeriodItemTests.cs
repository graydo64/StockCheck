using System;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace StockCheck.Model.Tests
{
	[TestFixture()]
	public class WhenAPeriodItemIsConstructed
	{
		[Test()]
		public void TheItemsReceivedCollectionIsNotNull()
		{
			var periodItem = new PeriodItem();

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
			var periodItem = new PeriodItem();
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
		}}
}

