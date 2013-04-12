using System;
using System.Collections.Generic;

namespace StockCheck.Model
{
	public class PeriodItem
	{
		public PeriodItem()
		{
			this.ItemsReceived = new List<ItemReceived>();
		}

		public SalesItem SalesItem { get; set; }

		public float OpeningStock { get; set; }

		public float ClosingStock { get; set; }

		public ICollection<ItemReceived> ItemsReceived { get; private set; }

		public void ReceiveItems(DateTime receivedDate, int quantity, decimal invoicedAmountEx, decimal invoicedAmountInc)
		{
			this.ItemsReceived.Add(
				new ItemReceived{ 
				ReceivedDate = receivedDate, 
				Quantity = quantity, 
				InvoicedAmountEx = invoicedAmountEx, 
				InvoicedAmountInc = invoicedAmountInc}
			);
		}

		public PeriodItem CopyForNextPeriod()
		{
			var clone = new PeriodItem();
			clone.OpeningStock = this.ClosingStock;
			clone.SalesItem = this.SalesItem;
			return clone;
		}
	}
}

