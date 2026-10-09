using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Magic.State;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Sites
{
    public class LandmarksTests
    {
        private sealed class Landmark : ILandmarkSite
        {
            public string Id { get; set; }
            public string Kind { get; set; } = "StandingStones";
            public Vector2Int Anchor { get; set; }
            public int Size { get; set; } = 1;
            public double ClaimCost { get; set; }
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; set; } = new IAbandonedSite[0];
            public IReadOnlyList<ILandmarkSite> Landmarks { get; set; }
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

        private sealed class Mana : IManaSettings
        {
            public double BaseCap => 100;
            public double BasePerHour => 12;
            public double LandmarkCap => 10;
        }

        // The Fallen Stones two cells past the Townhall's ground; the fog cleared with a single tap a cell.
        private sealed class Fixture : CityFixture
        {
            public Fixture()
            {
                Fog = new FogOfWar(new FogState(), City, Map, Buildings, Settings, new FogSettings(), Treasury);
                Landmarks = new Landmarks(State, ProvinceSites, Fog, Treasury, new ResearchFixture.FakeKnowledgeSettings(), 2);
                Pool = new ManaPool(new ManaState(), Treasury, new Mana(), null, new SiteManaSources(State, City, new Mana()));
            }

            public SitesState State { get; } = new();
            public Sites ProvinceSites { get; } = new() { Landmarks = new ILandmarkSite[] { new Landmark { Id = "FallenStones", Anchor = new Vector2Int(3, 0), ClaimCost = 500 } } };
            public FogOfWar Fog { get; }
            public Landmarks Landmarks { get; }
            public ManaPool Pool { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        [Test]
        public void AClaim_ShouldWaitForTheFogAndThenPayOnce()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Landmarks.Claim("FallenStones", 0), Is.EqualTo(ClaimResult.NotRevealed));

            fixture.Fog.Tap(new Vector2Int(3, 0));
            var gold = fixture.Treasury.Get("Gold");
            Assert.That(fixture.Landmarks.Claim("FallenStones", 0), Is.EqualTo(ClaimResult.Claimed));

            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold - 500));
            Assert.That(fixture.Treasury.Get("Knowledge"), Is.EqualTo(3));
            Assert.That(fixture.Landmarks.Claim("FallenStones", 0), Is.EqualTo(ClaimResult.AlreadyClaimed));
        }

        [Test]
        public void AClaim_ShouldDiscoverItsRingWithoutRevealingIt()
        {
            var fixture = new Fixture();
            fixture.Fog.Tap(new Vector2Int(3, 0));
            var far = new Vector2Int(5, 2);
            Assert.That(fixture.Fog.VisibilityAt(far), Is.EqualTo(Visibility.Undiscovered));

            fixture.Landmarks.Claim("FallenStones", 0);

            Assert.That(fixture.Fog.VisibilityAt(far), Is.EqualTo(Visibility.Discovered));
            Assert.That(fixture.Fog.IsRevealed(far), Is.False);
        }

        [Test]
        public void AClaim_ShouldRaiseTheManaPoolForGood()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Pool.Cap, Is.EqualTo(100));
            fixture.Fog.Tap(new Vector2Int(3, 0));

            fixture.Landmarks.Claim("FallenStones", 0);

            Assert.That(fixture.Pool.Cap, Is.EqualTo(110));
        }

        [Test]
        public void ALandmark_ShouldHoldItsGround()
        {
            var fixture = new Fixture();
            var ground = new SiteGround(fixture.State, fixture.ProvinceSites, fixture.Buildings);

            Assert.That(ground.Holds(new Vector2Int(3, 0)), Is.True);
            Assert.That(fixture.Landmarks.At(new Vector2Int(3, 0))?.Id, Is.EqualTo("FallenStones"));
        }
    }
}
