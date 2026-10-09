using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Fog
{
    public class FogOfWarTests
    {
        private sealed class Settings : IFogSettings
        {
            public IReadOnlyList<double> CostPerRing => new double[] { 4, 8, 20, 85 };
            public double FallbackGrowth => 2;
            public int TapsToReveal => 5;
            public double MinCost => 1;
            public int CountStep => 10;
            public double CountGrowth => 1.05;
            public IReadOnlyList<int> ReachPerTownhallLevel => new[] { 3, 5 };
        }

        // A Townhall of 2 × 2 at the origin that reveals one ring round it (three from level 2) and sees two.
        private sealed class Fixture : CityFixture
        {
            public Fixture(int buildRevealRadius = 0)
                : base(new BuildingBuilder().WithId("Housing").WithLevelPrices(BuildingBuilder.Price("Gold", 10))
                    .WithBuildSeconds(1).WithFog(buildRevealRadius, 2).Build())
            {
                Fog = new FogOfWar(State, City, Map, Buildings, Settings, new Settings(), Treasury);
                Lift = new BuildingsLiftFog(Construction, Fog);
            }

            public FogState State { get; } = new();
            public FogOfWar Fog { get; }
            public BuildingsLiftFog Lift { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2, 1, 3).Build();
        }

        [Test]
        public void NewKingdom_ShouldRevealTheTownhallsSixteenCells()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Fog.RevealedCount, Is.EqualTo(16));
            Assert.That(fixture.Fog.VisibilityAt(new Vector2Int(-1, -1)), Is.EqualTo(Visibility.Revealed));
            Assert.That(fixture.Fog.VisibilityAt(new Vector2Int(-2, -2)), Is.EqualTo(Visibility.Discovered));
            Assert.That(fixture.Fog.VisibilityAt(new Vector2Int(-4, -4)), Is.EqualTo(Visibility.Undiscovered));
        }

        [Test]
        public void Tap_ShouldClearACellInFiveSharesThatSumToItsPrice()
        {
            var fixture = new Fixture();
            var cell = new Vector2Int(3, 0);
            var gold = fixture.Treasury.Get("Gold");
            var price = fixture.Fog.Cost(cell);

            for (var i = 0; i < 4; i++) Assert.That(fixture.Fog.Tap(cell), Is.EqualTo(RevealResult.Paid));
            Assert.That(fixture.Fog.Tap(cell), Is.EqualTo(RevealResult.Revealed));

            Assert.That(fixture.Fog.IsRevealed(cell), Is.True);
            Assert.That(gold - fixture.Treasury.Get("Gold"), Is.EqualTo(price));
        }

        [Test]
        public void Cost_ShouldGrowWithTheRingAndTheCountRevealed()
        {
            var fixture = new Fixture();

            // Ring 2 is 8 Gold; sixteen cells revealed is one step of ×1.05: 8.4 → 8.
            Assert.That(fixture.Fog.Cost(new Vector2Int(3, 0)), Is.EqualTo(8));
            Assert.That(fixture.Fog.Rings(new Vector2Int(5, 0)), Is.EqualTo(4));
            Assert.That(fixture.Fog.Cost(new Vector2Int(5, 0)), Is.EqualTo(89));
        }

        [Test]
        public void Tap_ShouldKeepTheFrontierConnected()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Fog.Tap(new Vector2Int(4, 0)), Is.EqualTo(RevealResult.NotReachable));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD));
        }

        [Test]
        public void Tap_ShouldStopAtTheTownhallsReach()
        {
            var fixture = new Fixture();
            foreach (var x in new[] { 3, 4 })
                for (var i = 0; i < 5; i++) fixture.Fog.Tap(new Vector2Int(x, 0));

            Assert.That(fixture.Fog.Tap(new Vector2Int(5, 0)), Is.EqualTo(RevealResult.OutOfReach));
        }

        [Test]
        public void Tap_ShouldBeRefusedWithoutTheGold()
        {
            var fixture = new Fixture();
            fixture.Treasury.TryPay(new Dictionary<string, double> { ["Gold"] = CityFixture.START_GOLD });

            Assert.That(fixture.Fog.Tap(new Vector2Int(3, 0)), Is.EqualTo(RevealResult.NotEnoughGold));
            Assert.That(fixture.Fog.TapsDone(new Vector2Int(3, 0)), Is.EqualTo(0));
        }

        [Test]
        public void AFinishedBuilding_ShouldSeeItsRing()
        {
            var fixture = new Fixture();
            var cell = new Vector2Int(2, 0);
            fixture.Construction.Build("Housing", cell, 0);
            fixture.Timeline.Advance(1000);

            Assert.That(fixture.Fog.IsRevealed(cell), Is.True);
            Assert.That(fixture.Fog.VisibilityAt(new Vector2Int(4, 2)), Is.EqualTo(Visibility.Discovered));
        }

        [Test]
        public void TheTownhallsSecondLevel_ShouldRevealThreeRings()
        {
            var fixture = new Fixture();
            var hall = fixture.City.Districts[0];
            fixture.City.Jobs.Add(new Codigames.Kingdom.City.State.ConstructionJob { Id = "job-x", DistrictId = hall.Id, TargetLevel = 2, StartedAt = 0, Seconds = 1 });

            fixture.Timeline.Advance(1000);

            Assert.That(fixture.Fog.IsRevealed(new Vector2Int(4, 4)), Is.True);
            Assert.That(fixture.Fog.RevealedCount, Is.EqualTo(64));
        }
    }
}
