using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Fog
{
    public class SightingTests
    {
        private sealed class Landmark : ILandmarkSite
        {
            public string Id => "FallenStones";
            public string Kind => "StandingStones";
            public Vector2Int Anchor => new(6, 0);
            public int Size => 1;
            public double ClaimCost => 100;
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned => new IAbandonedSite[0];
            public IReadOnlyList<ILandmarkSite> Landmarks => new ILandmarkSite[] { new Landmark() };
        }

        private sealed class Sight : ISightSettings
        {
            public IReadOnlyList<int> MountainBySize => new[] { 0, 4, 5 };
            public int Landmark => 3;
        }

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

        // The Townhall's ground revealed to x = 2 and seen to x = 4; standing stones at x = 6, seen from 3 cells.
        private sealed class Fixture : CityFixture
        {
            public Fixture()
            {
                Fog = new FogOfWar(State, City, Map, Buildings, Settings, new FogSettings(), Treasury);
                var footprints = new Footprints(Map, new Catalog<IFeatureDefinition>(new IFeatureDefinition[0]));
                Sighting = new Sighting(State, Fog, Map, footprints, new Sites(), new SitesState(), Buildings, new Sight());
            }

            public FogState State { get; } = new();
            public FogOfWar Fog { get; }
            public Sighting Sighting { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        [Test]
        public void ATallThing_ShouldBeSightedOnceRevealedGroundIsCloseEnough()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Sighting.Things, Is.Empty, "four cells from the nearest revealed one");

            fixture.Fog.Tap(new Vector2Int(3, 0));

            Assert.That(fixture.Sighting.At(new Vector2Int(6, 0))?.Id, Is.EqualTo("FallenStones"));
        }

        [Test]
        public void ASightedThing_ShouldDrawAsItselfOnceSeen()
        {
            var fixture = new Fixture();
            fixture.Fog.Tap(new Vector2Int(3, 0));
            fixture.Fog.Tap(new Vector2Int(4, 0));

            Assert.That(fixture.Fog.VisibilityAt(new Vector2Int(5, 0)), Is.EqualTo(Visibility.Discovered));
            fixture.Fog.Tap(new Vector2Int(5, 0));

            Assert.That(fixture.Sighting.At(new Vector2Int(6, 0)), Is.Null, "discovered now: drawn as itself");
        }
    }
}
