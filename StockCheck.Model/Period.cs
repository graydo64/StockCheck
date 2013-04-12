using System;

namespace StockCheck.Model
{
	public class Period
	{
		public Period()
		{
		}

		public string PeriodName { get; set; }

		public DateTime StartOfPeriod { get; set; }

		public DateTime EndOfPeriod { get; set; }
	}
}

