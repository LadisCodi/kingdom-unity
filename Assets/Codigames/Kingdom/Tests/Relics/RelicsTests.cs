using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Magic.State;
using Codigames.Kingdom.Relics;
using Codigames.Kingdom.Relics.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Relics
{
    public class RelicsTests
    {
        private sealed class Relic : IRelicDefinition
        {
            public string Id { get; set; }
            public RelicKind Kind { get; set; } = RelicKind.City;
            public string Door { get; set; }
            public IReadOnlyList<RelicStat> Stats { get; set; } = new RelicStat[0];
            public double PassiveBase { get; set; } = 1.1;
            public double PassivePerLevel { get; set; } = 0.1;
            public int ActivationMana { get; set; } = 20;
            public int ActivationRadius { get; set; } = 2;
            public bool Pending { get; set; }
        }

        private sealed class Settings : IRelicSettings
        {
            public IReadOnlyList<RelicAxis> Cycle { get; } = new[] { RelicAxis.Window, RelicAxis.Radius, RelicAxis.Effect };
            public IReadOnlyList<int> WindowMinutes { get; } = new[] { 30, 60, 120, 240, 480 };
            public int KeystoneOneIn => 8;
            public double LevelStardustBase => 100;
            public double LevelStardustGrowth => 1.5;
            public int FragmentPackGems => 900;
            public int FragmentPackSize => 5;
            public int TreasureEvery => 6;
            public IReadOnlyList<int> PerLairTier { get; } = new[] { 1, 1, 2, 2, 3 };
            public int ShrineMaterialBuilds => 1;
            public IReadOnlyList<int> ShrinePremiumGems { get; } = new[] { 3000, 5000 };
        }

        private sealed class Mana : IManaSettings
        {
            public double BaseCap => 100;
            public double BasePerHour => 12;
            public double LandmarkCap => 10;
        }

        private sealed class Ruin : IAbandonedSite
        {
            public string Id { get; set; }
            public string District { get; set; }
            public Vector2Int Anchor { get; set; }
            public int Sight { get; set; } = 3;
            public string Name { get; set; }
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; set; } = new IAbandonedSite[0];
            public IReadOnlyList<ILandmarkSite> Landmarks { get; set; } = new ILandmarkSite[0];
        }

        // Two city relics behind two lairs — the Crown raising tax, the Staff a node's stock — a world relic, and a
        // Shrine of 1 × 1 to host them in.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(Shrine())
            {
                Catalog = new Catalog<IRelicDefinition>(new IRelicDefinition[]
                {
                    new Relic { Id = "Crown", Door = "Goblins", Stats = new[] { new RelicStat(RelicStats.TAX_RATE, false) } },
                    new Relic { Id = "Staff", Door = "Orcs", Stats = new[] { new RelicStat(RelicStats.HARVEST_STOCK, false) }, PassiveBase = 1.2, PassivePerLevel = 0.2 },
                    new Relic { Id = "Orb", Kind = RelicKind.World, Door = "portal", ActivationMana = 0, ActivationRadius = 0 },
                });
                Relics = new Kingdom.Relics.Relics(State, Catalog, RelicSettings, Treasury, 7);
                Pool = new ManaPool(new ManaState(), Treasury, new Mana());
                Shrines = new Shrines(State, Relics, City, Buildings, Pool);
                Timeline.Register(Shrines);
            }

            public RelicsState State { get; } = new();
            public Settings RelicSettings { get; } = new();
            public Catalog<IRelicDefinition> Catalog { get; }
            public Kingdom.Relics.Relics Relics { get; }
            public ManaPool Pool { get; }
            public Shrines Shrines { get; }

            // Six distinct fragments of a relic, found.
            public void Set(string relic, int times = 1)
            {
                if (!State.Held.TryGetValue(relic, out var f)) State.Held[relic] = f = new RelicFragments();
                for (var s = 0; s < Kingdom.Relics.Relics.SLOTS; s++) f.Found[s] += times;
            }

            private static IBuildingDefinition Shrine()
                => new BuildingBuilder().WithId("Shrine").WithSize(1, 1).WithLevelPrices(BuildingBuilder.Price("Gold", 100))
                    .WithBuildSeconds(10).HostingRelic().Build();
        }

        [Test]
        public void ADoor_ShouldHandOverTheFirstPieceOfEveryRelicBehindIt_Once()
        {
            var fixture = new Fixture();

            var first = fixture.Relics.OpenDoor("Goblins");
            var again = fixture.Relics.OpenDoor("Goblins");

            Assert.That(first.Select(d => (d.Relic, d.Slot)), Is.EqualTo(new[] { ("Crown", 0) }));
            Assert.That(again, Is.Empty);
            Assert.That(fixture.Relics.IsMet("Crown"), Is.True);
            Assert.That(fixture.Relics.IsMet("Staff"), Is.False);
        }

        [Test]
        public void ADrop_ShouldRollOnlyMetRelicsOfItsKind_AndTheSameEventTheSameFragments()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Relics.Drop(RelicKind.City, 3, "lair", "Goblins"), Is.Empty);

            fixture.Relics.OpenDoor("Goblins");
            var drops = fixture.Relics.Drop(RelicKind.City, 20, "lair", "Goblins");
            var twin = new Fixture();
            twin.Relics.OpenDoor("Goblins");

            Assert.That(drops.All(d => d.Relic == "Crown"), Is.True);
            Assert.That(twin.Relics.Drop(RelicKind.City, 20, "lair", "Goblins").Select(d => d.Slot), Is.EqualTo(drops.Select(d => d.Slot)));
            Assert.That(drops.Count(d => d.Slot == Kingdom.Relics.Relics.KEYSTONE), Is.LessThan(drops.Count));
        }

        [Test]
        public void ALair_ShouldPayItsTiersShare_AndEveryNthTreasureOne()
        {
            var fixture = new Fixture();

            var claim = fixture.Relics.ForLair("Goblins", 3);

            Assert.That(claim.Count, Is.EqualTo(1 + 2));
            Assert.That(fixture.Relics.ForTreasure(5), Is.Empty);
            Assert.That(fixture.Relics.ForTreasure(6).Count, Is.EqualTo(1));
        }

        [Test]
        public void Restore_ShouldTakeSixDistinct_AndALevelASetAndItsStardust()
        {
            var fixture = new Fixture();
            fixture.Relics.OpenDoor("Goblins");

            Assert.That(fixture.Relics.Restore("Crown"), Is.EqualTo(RelicRestoreResult.Missing));

            fixture.Set("Crown", 2);
            Assert.That(fixture.Relics.Restore("Crown"), Is.EqualTo(RelicRestoreResult.Restored));
            Assert.That(fixture.Relics.LevelUp("Crown"), Is.EqualTo(RelicLevelResult.NotEnoughStardust));

            fixture.Treasury.Add("Stardust", 100);
            Assert.That(fixture.Relics.LevelUp("Crown"), Is.EqualTo(RelicLevelResult.Levelled));
            Assert.That(fixture.Relics.Level("Crown"), Is.EqualTo(2));
            Assert.That(fixture.Treasury.Get("Stardust"), Is.EqualTo(0));
            Assert.That(fixture.Relics.LevelStardust(2), Is.EqualTo(150));
            Assert.That(fixture.Relics.SlotCount("Crown", 0), Is.EqualTo(1));
            Assert.That(fixture.Relics.LevelUpBlock("Crown"), Is.EqualTo(RelicLevelResult.MissingFragments));
        }

        [Test]
        public void ACityRelicsLevels_ShouldCycleWindowRadiusEffect_AndAWindowPastTheLastRaiseTheNumber()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Relics.Steps("Crown", 1), Is.EqualTo((0, 0, 0)));
            Assert.That(fixture.Relics.Steps("Crown", 4), Is.EqualTo((1, 1, 1)));
            Assert.That(fixture.Relics.NextAxis("Crown", 4), Is.EqualTo(RelicAxis.Window));
            // Five windows: the fifth window step (level 14) has nowhere to go and lifts the number.
            Assert.That(fixture.Relics.Steps("Crown", 14), Is.EqualTo((4, 4, 5)));
            Assert.That(fixture.Relics.WindowMs("Crown", 14), Is.EqualTo(480 * 60_000.0));
            Assert.That(fixture.Relics.Value("Crown", 4), Is.EqualTo(1.2).Within(1e-9));
            Assert.That(fixture.Relics.Steps("Orb", 3), Is.EqualTo((0, 0, 2)));
        }

        [Test]
        public void AnActivatedRelic_ShouldReachItsAuraForItsWindow_AndThenStop()
        {
            var fixture = new Fixture();
            fixture.Relics.Give("Crown");
            var shrine = fixture.Stand("Shrine", new Vector2Int(3, 0));
            var near = new Vector2Int(2, 0);
            var far = new Vector2Int(-4, 0);

            Assert.That(fixture.Shrines.Block("Crown"), Is.EqualTo(ActivateBlock.NotHosted));
            Assert.That(fixture.Shrines.Host("Crown", shrine.Id), Is.EqualTo(HostResult.Hosted));
            Assert.That(fixture.Shrines.At(RelicStats.TAX_RATE, near), Is.EqualTo(1));

            var mana = fixture.Pool.Amount;
            Assert.That(fixture.Shrines.Activate("Crown", 0), Is.EqualTo(ActivateBlock.None));
            Assert.That(fixture.Pool.Amount, Is.EqualTo(mana - 20));
            Assert.That(fixture.Shrines.At(RelicStats.TAX_RATE, near), Is.EqualTo(1.1).Within(1e-9));
            Assert.That(fixture.Shrines.At(RelicStats.TAX_RATE, far), Is.EqualTo(1));
            Assert.That(fixture.Shrines.At(RelicStats.HARVEST_STOCK, near), Is.EqualTo(1));
            Assert.That(fixture.Shrines.Over(RelicStats.TAX_RATE, fixture.City.Districts[0]), Is.EqualTo(1.1).Within(1e-9));
            Assert.That(fixture.Shrines.Activate("Crown", 1), Is.EqualTo(ActivateBlock.Active));

            fixture.Timeline.Advance(30 * 60_000 - 1);
            Assert.That(fixture.Shrines.IsAwake("Crown"), Is.True);
            fixture.Timeline.Advance(30 * 60_000);
            Assert.That(fixture.Shrines.IsAwake("Crown"), Is.False);
            Assert.That(fixture.Shrines.At(RelicStats.TAX_RATE, near), Is.EqualTo(1));
        }

        [Test]
        public void HostingElsewhere_ShouldCloseTheWindowItHad()
        {
            var fixture = new Fixture();
            fixture.Relics.Give("Crown");
            var a = fixture.Stand("Shrine", new Vector2Int(4, 0));
            var b = fixture.Stand("Shrine", new Vector2Int(-4, 0));
            fixture.Shrines.Host("Crown", a.Id);
            fixture.Shrines.Activate("Crown", 0);

            fixture.Shrines.Host("Crown", b.Id);

            Assert.That(fixture.Shrines.IsAwake("Crown"), Is.False);
            Assert.That(fixture.Shrines.Hosted(a), Is.Null);
            Assert.That(fixture.Shrines.HostOf("Crown"), Is.SameAs(b));
            Assert.That(fixture.Shrines.Host("Orb", b.Id), Is.EqualTo(HostResult.NotRestored));
        }

        [Test]
        public void TheShrineLadder_ShouldAskTheRuinFirst_ThenMaterials_ThenGems_ThenEnd()
        {
            var fixture = new Fixture();
            var sites = new SitesState();
            var province = new Sites { Abandoned = new IAbandonedSite[] { new Ruin { Id = "OldShrine", District = "Shrine", Anchor = new Vector2Int(-4, -4) } } };
            var ladder = new ShrineLadder(fixture.State, fixture.RelicSettings, sites, province, fixture.Buildings, fixture.City);
            var shrine = fixture.Buildings.Get("Shrine");

            Assert.That(ladder.Refusal(shrine), Is.EqualTo(ConstructionRefusal.RuinFirst));

            sites.Repaired.Add("OldShrine");
            fixture.Stand("Shrine", new Vector2Int(-4, -4));
            Assert.That(ladder.Refusal(shrine), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(ladder.Price(shrine), Is.Null);

            ladder.Built(shrine);
            fixture.Stand("Shrine", new Vector2Int(4, 0));
            Assert.That(ladder.Price(shrine)["Gems"], Is.EqualTo(3000));

            ladder.Built(shrine);
            fixture.Stand("Shrine", new Vector2Int(4, 2));
            Assert.That(ladder.Price(shrine)["Gems"], Is.EqualTo(5000));

            ladder.Built(shrine);
            fixture.Stand("Shrine", new Vector2Int(4, 4));
            Assert.That(ladder.Refusal(shrine), Is.EqualTo(ConstructionRefusal.AtCap));
        }
    }
}
