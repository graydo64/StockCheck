using System;
using System.Linq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using StockCheck.ModelFs;

namespace StockCheck.Model.Tests
{
	[TestFixture]
	public abstract class PeriodTestBase
	{
		protected Fixture _fixture;
		protected Period _target;

		public PeriodTestBase()
		{
			_fixture = new Fixture();
			_target = new Period();
		}
	}

	[TestFixture]
	public class WhenAPeriodIsInitialisedWithoutZeroCarriedItems: PeriodTestBase
	{
		[Test]
		public void TheItemsCollectionExcludesTheSalesItemCarryingNoStock()
		{
			var source = _fixture.Create<Period>();
			source.Items.First().OpeningStock = 0;
			source.Items.First().ClosingStock = 0;
			_target = Period.InitialiseWithoutZeroCarriedItems(source);
			
			Assert.AreEqual(source.Items.Count - 1, _target.Items.Count);
		}

		[Test]
		public void TheItemsCollectionOnlyContainsTheSalesItemsCarryingStock()
		{
			var source = _fixture.Create<Period>();
			source.Items.First().OpeningStock = 0;
			source.Items.First().ClosingStock = 0;
			source.Items.Last().OpeningStock = 0;
			source.Items.Last().ClosingStock = 0;
			_target = Period.InitialiseWithoutZeroCarriedItems(source);
			
			Assert.AreEqual(source.Items.Count - 2, _target.Items.Count);
		}

		[Test]
		public void ThePeriodStartDateIsADayAfterTheSourcePeriodEndDate()
		{
			var source = _fixture.Create<Period>();
			_target = Period.InitialiseWithoutZeroCarriedItems(source);
			
			DateTime nextDay = source.EndOfPeriod.AddDays(1);
			
			Assert.AreEqual(nextDay, _target.StartOfPeriod);
		}
	}
}

