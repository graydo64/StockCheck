using System;
using NUnit.Framework;
using StockCheck.Model;

namespace StockCheck.Model.Tests
{
	[TestFixture]
	public class WhenTheCostPerContainerIsZero
	{
		[Test]
		public void TheCostPerUnitOfSaleIsZero ()
		{
			var salesItem = new SalesItem ();
			salesItem.CostPerContainer = 0;

			Assert.AreEqual (0, salesItem.CostPerUnitOfSale);
		}
	}

	[TestFixture]
	public class WhenTheItemIsDraughtAndTheCostPerContainerIsGreaterThanZero
	{
		[Test]
		public void TheCostPerUnitOfSaleIsCalculated ()
		{
			var salesItem = new SalesItem ();
			float containerSize = 11f;
			float unitOfSale = (1f / 8);
			decimal costPerContainer = 110m;

			salesItem.ContainerSize = containerSize;
			salesItem.UnitOfSale = unitOfSale;
			salesItem.CostPerContainer = costPerContainer;

			Assert.AreEqual ((110f / (11f / (1f / 8))), salesItem.CostPerUnitOfSale);
		}
	}

	[TestFixture]
	public class WhenTheItemIsSpiritAndTheCostPerContainerIsGreaterThanZero
	{
		[Test]
		public void TheCostPerUnitOfSaleIsCalculated ()
		{
			var salesItem = new SalesItem ();
			float containerSize = 0.7f;
			float unitOfSale = 0.035f;
			decimal costPerContainer = 22m;
			
			salesItem.ContainerSize = containerSize;
			salesItem.UnitOfSale = unitOfSale;
			salesItem.CostPerContainer = costPerContainer;

			Assert.AreEqual (22 / (0.7f / 0.035f), salesItem.CostPerUnitOfSale);
		}
	}

	[TestFixture]
	public class WhenTheSalesPriceIsZero
	{
		[Test]
		public void TheGPIsZero ()
		{
			var salesItem = new SalesItem ();

			salesItem.SalesPrice = 0;

			Assert.AreEqual (0, salesItem.IdealGP);
		}
	}

	[TestFixture]
	public class WhenTheSalesPriceIsDoubleTheCostPrice
	{
		[Test]
		public void TheGPIsFiftyPercent ()
		{
			var salesItem = new SalesItem ();
			
			salesItem.SalesPrice = 2.00m;
			salesItem.ContainerSize = 1;
			salesItem.UnitOfSale = 1;
			salesItem.CostPerContainer = 1;
			
			Assert.AreEqual (0.5, salesItem.IdealGP);
		}
	}
}

