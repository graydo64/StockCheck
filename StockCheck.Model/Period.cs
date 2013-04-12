using System;
using System.Collections.Generic;

namespace StockCheck.Model
{
	public class Period
	{
		public Period()
		{
			this.Items = new List<PeriodItem>();
		}

		public string PeriodName { get; set; }

		public DateTime StartOfPeriod { get; set; }

		public DateTime EndOfPeriod { get; set; }

		public ICollection<PeriodItem> Items { get; set; }

		public static Period InitialiseFromClone(Period source)
		{
			var period = Period.InitialiseFrom(source);
			foreach(var item in source.Items)
			{
				period.Items.Add(item.CopyForNextPeriod());
			}

			return period;
		}

		public static Period InitialiseWithoutZeroCarriedItems(Period source)
		{
			var period = Period.InitialiseFrom(source);
			foreach(var item in source.Items)
			{
				if(item.OpeningStock > 0 && item.ClosingStock > 0)
				{
					period.Items.Add(item.CopyForNextPeriod());
				}
			}

			return period;
		}

		private static Period InitialiseFrom(Period source)
		{
			var period = new Period();
			period.StartOfPeriod = source.EndOfPeriod.AddDays(1);
			return period;
		}
	}
}

