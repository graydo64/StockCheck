using System;

namespace StockTake.Model
{
	public class SalesItem
	{
		public SalesItem ()
		{
		}
		
		public string Name { get; set; }
		
		public int LedgerCode { get; set; }
		
		public float ContainerSize { get; set; }
		
		public float UnitOfSale { get; set; }
		
		public float CostPerContainer { get; set; }
		
		public float CostPerUnitOfSale
		{
			get
			{
				if (this.CostPerContainer == 0)
				{
					return 0;
				}

				return this.CostPerContainer / (this.ContainerSize / this.UnitOfSale);
			}
		}
		
		public float TaxRate { get; set; }
		
		public float SalesPrice { get; set; }

		public float IdealGP
		{
			get
			{
				if (this.SalesPrice == 0)
				{
					return 0;
				}

				return (this.SalesPrice - this.CostPerUnitOfSale) / this.SalesPrice;
			}
		}
		
	}
}

