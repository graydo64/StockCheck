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
		
		public decimal CostPerContainer { get; set; }
		
		public decimal CostPerUnitOfSale {
			get {
				if (this.CostPerContainer == 0) {
					return 0;
				}

				return (decimal)(Convert.ToSingle(this.CostPerContainer) / (this.ContainerSize / this.UnitOfSale));
			}
		}
		
		public decimal TaxRate { get; set; }
		
		public decimal SalesPrice { get; set; }

		public float IdealGP {
			get {
				if (this.SalesPrice == 0) {
					return 0;
				}

				return ((float)this.SalesPrice - (float)this.CostPerUnitOfSale) / (float)this.SalesPrice;
			}
		}

		public int UllagePerContainer { get; set; }

	}
}

