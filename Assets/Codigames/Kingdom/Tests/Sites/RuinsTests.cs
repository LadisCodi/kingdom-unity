using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Sites
{
    public class RuinsTests
    {
        private sealed class Site : IAbandonedSite
        {
            public string Id { get; set; }
            public string District { get; set; }
            public Vector2Int Anchor { get; set; }
            public int Sight { get; set; } = 3;
            public string Name { get; set; }
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; set; }
            public IReadOnlyList<ILandmarkSite> Landmarks { get; set; } = new ILandmarkSite[0];
        }

        private sealed class Bag : IRepairItems
        {
            public Dictionary<string, int> Items { get; } = new();
            public int Held(string item) => Items.TryGetValue(item, out var n) ? n : 0;
            public void Take(string item) => Items[item] = Held(item) - 1;
        }

        // An old farm two cells east of the Townhall — the Farm is behind Farming — and an old watchtower that is
        // missing its lens.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(new[] { new TechBuilder("Farming").Opens(UnlockKind.District, "Farm").Build() },
                new BuildingBuilder().WithId("Farm").WithLevelPrices(BuildingBuilder.Price("Gold", 50)).WithBuildSeconds(60).WithRepair(5).Build(),
                new BuildingBuilder().WithId("Watchtower").WithLevelPrices(BuildingBuilder.Price("Gold", 200)).WithBuildSeconds(60)
                    .WithRepair(0, "WatchtowerLens").Build())
            {
                ProvinceSites = new Sites
                {
                    Abandoned = new IAbandonedSite[]
                    {
                        new Site { Id = "OldFarm", District = "Farm", Anchor = new Vector2Int(3, 0), Name = "The old farm" },
                        new Site { Id = "NorthWatch", District = "Watchtower", Anchor = new Vector2Int(-3, -3), Name = "The old watchtower" },
                    },
                };
                Ground2 = new SiteGround(State, ProvinceSites, Buildings);
                Ruins = new Ruins(State, Ground2, Buildings, Construction, Revealed, Bag);
                SitePlacement = new Placement(Map, Buildings, Settings, City, Ground, Revealed, Ground2);
            }

            public SitesState State { get; } = new();
            public Sites ProvinceSites { get; }
            public SiteGround Ground2 { get; }
            public Bag Bag { get; } = new();
            public Ruins Ruins { get; }
            public Placement SitePlacement { get; }
        }

        [Test]
        public void Repair_ShouldBuildItWhereItStandsAtItsOwnWaitAndAskNoTechnology()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Construction.BuildRefusal("Farm"), Is.EqualTo(ConstructionRefusal.NeedsResearch));

            Assert.That(fixture.Ruins.Repair("OldFarm", 0), Is.EqualTo(RepairRefusal.None));

            var farm = fixture.District("Farm");
            Assert.That(farm.Anchor, Is.EqualTo(new Vector2Int(3, 0)));
            Assert.That(farm.Ordinal, Is.EqualTo(1));
            Assert.That(fixture.City.Jobs.Single().Seconds, Is.EqualTo(5));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD - 50));
            Assert.That(fixture.Ruins.Standing.Select(s => s.Id), Is.EqualTo(new[] { "NorthWatch" }));
        }

        [Test]
        public void Repair_ShouldWaitForTheFogAndForAMissingPiece()
        {
            var fixture = new Fixture();
            fixture.Revealed.Fogged.Add(new Vector2Int(3, 0));

            Assert.That(fixture.Ruins.Refusal("OldFarm"), Is.EqualTo(RepairRefusal.NotRevealed));
            Assert.That(fixture.Ruins.Refusal("NorthWatch"), Is.EqualTo(RepairRefusal.MissingItem));

            fixture.Bag.Items["WatchtowerLens"] = 1;
            Assert.That(fixture.Ruins.Repair("NorthWatch", 0), Is.EqualTo(RepairRefusal.None));
            Assert.That(fixture.Bag.Held("WatchtowerLens"), Is.EqualTo(0), "the lens goes into it");
        }

        [Test]
        public void Repair_ShouldNeedAFreeBuilder()
        {
            var fixture = new Fixture();
            fixture.Ruins.Repair("OldFarm", 0);
            fixture.Bag.Items["WatchtowerLens"] = 1;

            Assert.That(fixture.Ruins.Refusal("NorthWatch"), Is.EqualTo(RepairRefusal.NoFreeBuilder));
        }

        [Test]
        public void ARuin_ShouldHoldItsGroundUntilItsRepairStarts()
        {
            var fixture = new Fixture();
            var house = new Vector2Int(3, 0);

            Assert.That(fixture.SitePlacement.Check("Farm", house), Is.EqualTo(PlacementProblem.Occupied));
            Assert.That(fixture.Ruins.At(house)?.Id, Is.EqualTo("OldFarm"));

            fixture.Ruins.Repair("OldFarm", 0);

            Assert.That(fixture.Ruins.At(house), Is.Null);
        }
    }
}
