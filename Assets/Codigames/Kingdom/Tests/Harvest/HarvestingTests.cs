using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Harvest
{
    public class HarvestingTests
    {
        private static void TapTimes(HarvestFixture fixture, Vector2Int cell, int times, double now = 0)
        {
            for (var i = 0; i < times; i++) fixture.Harvesting.Tap(cell, now);
        }

        [Test]
        public void Tap_ShouldPayTenSecondsOfWorkForOneMana()
        {
            var fixture = new HarvestFixture();

            var result = fixture.Harvesting.Tap(HarvestFixture.TREE, 0);

            Assert.That(result.Refusal, Is.EqualTo(TapRefusal.None));
            Assert.That(result.Currency, Is.EqualTo("Wood"));
            Assert.That(fixture.Treasury.Get("Wood"), Is.EqualTo(1));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(99));
            Assert.That(fixture.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(9));
        }

        [Test]
        public void Tap_ShouldCarryTheFractionToALaterTap()
        {
            var fixture = new HarvestFixture();
            fixture.Ground.Features[HarvestFixture.TREE] = "Crops";

            TapTimes(fixture, HarvestFixture.TREE, 4);

            // 1.25 a tap: 1, 1, 1, then 2 with the three quarters carried.
            Assert.That(fixture.Treasury.Get("Food"), Is.EqualTo(5));
        }

        [Test]
        public void Tap_ShouldPayAtLeastOneUnitOfSlowGround()
        {
            var fixture = new HarvestFixture();

            fixture.Harvesting.Tap(HarvestFixture.ROCK, 0);

            Assert.That(fixture.Treasury.Get("Stone"), Is.EqualTo(1));
            Assert.That(fixture.Harvesting.UnitsAt(HarvestFixture.ROCK), Is.EqualTo(0), "bedrock keeps no depot");
        }

        [Test]
        public void Tap_ShouldTakeNothingFromBareGroundOrABuilding()
        {
            var fixture = new HarvestFixture();

            Assert.That(fixture.Harvesting.Tap(new Vector2Int(4, -1), 0).Refusal, Is.EqualTo(TapRefusal.NothingThere));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(100));
        }

        [Test]
        public void Tap_ShouldBeRefusedWithoutMana()
        {
            var fixture = new HarvestFixture();
            fixture.Treasury.TryPay(new System.Collections.Generic.Dictionary<string, double> { ["Mana"] = 100 });

            Assert.That(fixture.Harvesting.Tap(HarvestFixture.TREE, 0).Refusal, Is.EqualTo(TapRefusal.NoMana));
            Assert.That(fixture.Treasury.Get("Wood"), Is.EqualTo(0));
        }

        [Test]
        public void Ground_ShouldScaleWhatACellHolds()
        {
            var fixture = new HarvestFixture(grasslandWood: 1.25);

            // 10 × 1.25 = 12.5, rounded half up: a grassland tree holds 13.
            Assert.That(fixture.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(13));
        }

        [Test]
        public void Tree_ShouldGrowBackFullAfterItsRecovery()
        {
            var fixture = new HarvestFixture();
            TapTimes(fixture, HarvestFixture.TREE, 10);

            Assert.That(fixture.Harvesting.IsExhausted(HarvestFixture.TREE, 0), Is.True);
            Assert.That(fixture.Harvesting.Tap(HarvestFixture.TREE, 1000).Refusal, Is.EqualTo(TapRefusal.Exhausted));

            fixture.Timeline.Advance(180_000);

            Assert.That(fixture.Harvesting.IsExhausted(HarvestFixture.TREE, 180_000), Is.False);
            Assert.That(fixture.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(10));
        }

        [Test]
        public void Bush_ShouldBeEatenAndComeBackNextToWhereItStood()
        {
            var fixture = new HarvestFixture();
            TapTimes(fixture, HarvestFixture.BUSH, 10);

            Assert.That(fixture.Ground.Features.ContainsKey(HarvestFixture.BUSH), Is.False);

            fixture.Timeline.Advance(120_000);

            var back = 0;
            foreach (var cell in Modules.Grid.GridMath.Neighbours(HarvestFixture.BUSH))
            {
                if (fixture.Ground.Features.TryGetValue(cell, out var feature) && feature == "BerryBush") back++;
            }
            Assert.That(back, Is.EqualTo(1));
        }

        [Test]
        public void Pool_ShouldGainOneUnitEveryFiveMinutesUntilFull()
        {
            var fixture = new HarvestFixture();
            TapTimes(fixture, HarvestFixture.ROCK, 3);

            fixture.Timeline.Advance(300_000 * 2);

            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(99));

            fixture.Timeline.Advance(300_000 * 10);

            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(100));
            Assert.That(fixture.Mana.NextUnitAt, Is.Null);
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking()
        {
            var once = new HarvestFixture();
            var stepped = new HarvestFixture();
            foreach (var f in new[] { once, stepped })
            {
                TapTimes(f, HarvestFixture.TREE, 10);
                TapTimes(f, HarvestFixture.BUSH, 10);
            }

            once.Timeline.Advance(3_600_000);
            for (var t = 0; t <= 3_600_000; t += 7_000) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(3_600_000);

            Assert.That(once.Treasury.Get("Mana"), Is.EqualTo(stepped.Treasury.Get("Mana")));
            Assert.That(once.Ground.Features, Is.EquivalentTo(stepped.Ground.Features));
            Assert.That(once.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(stepped.Harvesting.UnitsAt(HarvestFixture.TREE)));
        }
    }
}
