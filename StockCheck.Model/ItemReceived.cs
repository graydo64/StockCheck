using System;

namespace StockCheck.Model
{
	public class ItemReceived
	{
		public ItemReceived()
		{
		}

		public DateTime ReceivedDate { get; set; }

		public int Quantity { get; set; }

		public decimal InvoicedAmountEx { get; set; }

		public decimal InvoicedAmountInc { get; set; }
	}
}

