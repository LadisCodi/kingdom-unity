using System.Collections.Generic;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Tutorial
{
    public class MapTargetsTests
    {
        private sealed class FogSettings : IFogSettings
        {
            public IReadOnlyList<double> CostPerRing => new double[] { 4, 8, 20, 85 };
            public double FallbackGrowth => 2;
            public int TapsToReveal => 1;
            public double MinCost => 1;
            public int CountStep => 10;
            public double CountGrowth => 1.05;
            public IReadOnlyList<int> ReachPerTownhallLevel => new[] { 6 };
        }

        private sealed class Settings : IEconomySettings, IWorkerSettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
            public double MoveSpeedTilesPerSecond => 1;
        }

        private sealed class NoSites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned => new IAbandonedSite[0];
            public IReadOnlyList<ILandmarkSite> Landmarks => new ILandmarkSite[0];
        }

        // The Townhall (2 × 2 at the origin) reveals its first ring and sees its second; a tree waits in the fog at
        // (3, 1), on the second, next to revealed ground. (3, 3) touches it only by a corner: it cannot be bought.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture()
            {
                Fog = new FogOfWar(FogState, City, Map, Buildings, Settings, new FogSettings(), Treasury);
                Fog.RevealAroundAll();
                var settings = new Settings();
                var stores = new Stores(City, Buildings, settings, Treasury, Construction);
                var crews = new Workforce(City, Buildings, Revealed, Harvesting, stores, settings);
                var state = new KingdomState { City = City, Ground = Ground, Fog = FogState };
                Targets = new MapTargets(state, Map, Fog, Harvesting, Buildings, new NoSites(), crews, stores, Placement);
                Ground.Features.Remove(TREE);
                Ground.Features[FOGGED_TREE] = "Trees";
            }

            public static readonly Vector2Int FOGGED_TREE = new(3, 1);

            public FogState FogState { get; } = new();
            public FogOfWar Fog { get; }
            public MapTargets Targets { get; }

            protected override Codigames.Kingdom.City.IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        [Test]
        public void FeatureInTheFog_ShouldBeTheNearestThePlayerCanPayFor()
        {
            var fixture = new Fixture();
            fixture.Ground.Features[new Vector2Int(5, 5)] = "Trees";

            var target = fixture.Targets.Resolve("feature:TreesFog", null, 0);

            Assert.That(target?.Anchor, Is.EqualTo(Fixture.FOGGED_TREE));
        }

        [Test]
        public void FeatureInTheFog_ShouldMoveOnOnceCleared()
        {
            var fixture = new Fixture();
            var first = fixture.Targets.Resolve("feature:TreesFog", null, 0);

            fixture.Fog.Tap(Fixture.FOGGED_TREE);
            var next = fixture.Targets.Resolve("feature:TreesFog", first, 0);

            Assert.That(first?.Anchor, Is.EqualTo(Fixture.FOGGED_TREE));
            Assert.That(next?.Anchor, Is.Not.EqualTo(Fixture.FOGGED_TREE));
        }

        [Test]
        public void FeatureDeepInTheFog_ShouldPointAtTheStepTowardsIt()
        {
            var fixture = new Fixture();
            fixture.Ground.Features.Remove(Fixture.FOGGED_TREE);
            fixture.Ground.Features[new Vector2Int(5, 5)] = "Trees";

            var target = fixture.Targets.Resolve("feature:TreesFog", null, 0);

            Assert.That(target.HasValue, Is.True);
            Assert.That(fixture.Fog.IsPayable(target.Value.Anchor), Is.True);
            var step = target.Value.Anchor;
            Assert.That(System.Math.Max(5 - step.X, 5 - step.Y), Is.EqualTo(3), "a payable cell as near the tree as any");
        }

        [Test]
        public void ATreasure_ShouldBePointedAt()
        {
            var fixture = new Fixture();
            fixture.FogState.Treasures[new Vector2Int(2, -1)] = new Treasure { N = 1, Coin = "Gold" };

            Assert.That(fixture.Targets.Resolve("treasure", null, 0)?.Anchor, Is.EqualTo(new Vector2Int(2, -1)));
        }

        [Test]
        public void ADistrict_ShouldBeItsWholePlot()
        {
            var fixture = new Fixture();
            var townhall = fixture.District("Townhall");

            var target = fixture.Targets.Resolve("district:Townhall", null, 0);

            Assert.That(target?.Anchor, Is.EqualTo(townhall.Anchor));
            Assert.That(target?.Width, Is.EqualTo(2));
            Assert.That(target.Value.Contains(new Vector2Int(townhall.Anchor.X + 1, townhall.Anchor.Y + 1)), Is.True);
        }

        [Test]
        public void ControlsAndTheWayBack_ShouldNotBeMapPoints()
        {
            Assert.That(MapTargets.IsMapPoint("ui:nav:build"), Is.False);
            Assert.That(MapTargets.IsMapPoint("quest"), Is.False);
            Assert.That(MapTargets.IsMapPoint("back"), Is.False);
            Assert.That(MapTargets.IsMapPoint("cell:2,3"), Is.True);
            Assert.That(new Fixture().Targets.Resolve("cell:2,-3", null, 0)?.Anchor, Is.EqualTo(new Vector2Int(2, -3)));
        }
    }
}
