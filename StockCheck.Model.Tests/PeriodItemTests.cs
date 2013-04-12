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
}

