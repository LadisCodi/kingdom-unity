using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Bag.State;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Bag
{
    public class BagTests
    {
        private sealed class Item : IItemDefinition
        {
            public string Id { get; set; }
            public ItemKind Kind { get; set; }
            public string Coin { get; set; }
            public double Seconds { get; set; }
            public int Tier { get; set; } = 1;
            public SpeedupKind? Speeds { get; set; }
            public BoostKind? Boost { get; set; }
            public double Value { get; set; }
        }

        private sealed class Production : IProduction
        {
            public Dictionary<string, double> PerSecond { get; } = new();
            public double MakesPerSecond(string currency) => PerSecond.TryGetValue(currency, out var v) ? v : 0;
        }

        private sealed class Settings : IBagSettings
        {
            public double ChestFloorPerHour => 60;
        }

        private sealed class Economy : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        private static readonly Item[] ITEMS =
        {
            new() { Id = "GoldChest1h", Kind = ItemKind.Chest, Coin = "Gold", Seconds = 3600 },
            new() { Id = "ChoiceChest1h", Kind = ItemKind.Choice, Seconds = 3600 },
            new() { Id = "RentBoost8h", Kind = ItemKind.Boost, Boost = BoostKind.Rent, Seconds = 28800, Value = 25 },
            new() { Id = "ManaFlaskSmall", Kind = ItemKind.Flask, Value = 25 },
            new() { Id = "KnowledgeTome", Kind = ItemKind.Tome, Value = 5 },
            new() { Id = "GeneralSpeedup5m", Kind = ItemKind.Speedup, Speeds = SpeedupKind.General, Seconds = 300 },
            new() { Id = "GeneralSpeedup1h", Kind = ItemKind.Speedup, Speeds = SpeedupKind.General, Seconds = 3600 },
            new() { Id = "ConstructionSpeedup15m", Kind = ItemKind.Speedup, Speeds = SpeedupKind.Construction, Seconds = 900 },
            new() { Id = "TrainingSpeedup1h", Kind = ItemKind.Speedup, Speeds = SpeedupKind.Training, Seconds = 3600 },
        };

        // A House waiting on its builder for an hour, a crop source, the harvest's fixture's treasury and pool.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture()
                : base(1, new Codigames.Kingdom.Research.ITechnology[0], new BuildingBuilder().WithId("Housing").WithHousing(2).WithStorage(600)
                    .WithLevelPrices(BuildingBuilder.Price("Gold", 10)).WithBuildSeconds(3600).Build())
            {
                Items = new Catalog<IItemDefinition>(ITEMS);
                Boosts = new Boosts(State);
                Bag = new Codigames.Kingdom.Bag.Bag(State, Items, Treasury, Production, new Settings(), Boosts, Mana);
                Speedups = new Speedups(Bag, Items, Construction, new VillagerTraining(City, null, Treasury,
                    new Stores(City, Buildings, new Economy(), Treasury, Construction)), City);
            }

            public BagState State { get; } = new();
            public Production Production { get; } = new();
            public Catalog<IItemDefinition> Items { get; }
            public Boosts Boosts { get; }
            public Codigames.Kingdom.Bag.Bag Bag { get; }
            public Speedups Speedups { get; }
        }

        [Test]
        public void AChest_ShouldPayItsHoursOfWhatTheCityMakes_Floored()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("GoldChest1h", 2);
            Assert.That(fixture.Bag.ChestValue("GoldChest1h")["Gold"], Is.EqualTo(60), "the floor: a city making nothing");

            fixture.Production.PerSecond["Gold"] = 0.5;
            var before = fixture.Treasury.Get("Gold");
            Assert.That(fixture.Bag.Use("GoldChest1h", 2, 0), Is.EqualTo(UseItemResult.Used));

            Assert.That(fixture.Treasury.Get("Gold") - before, Is.EqualTo(2 * 1800));
            Assert.That(fixture.Bag.Count("GoldChest1h"), Is.EqualTo(0));
        }

        [Test]
        public void AChoiceChest_ShouldAskForACoin()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("ChoiceChest1h", 1);

            Assert.That(fixture.Bag.Use("ChoiceChest1h", 1, 0), Is.EqualTo(UseItemResult.NeedsACoin));
            Assert.That(fixture.Bag.Use("ChoiceChest1h", 1, 0, "Wood"), Is.EqualTo(UseItemResult.Used));
            Assert.That(fixture.Treasury.Get("Wood"), Is.EqualTo(60));
        }

        [Test]
        public void ASpeedup_ShouldNotBeUsedFromTheBagAlone()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("GeneralSpeedup5m", 1);
            Assert.That(fixture.Bag.Use("GeneralSpeedup5m", 1, 0), Is.EqualTo(UseItemResult.UsedElsewhere));
            Assert.That(fixture.Bag.Use("GoldChest1h", 1, 0), Is.EqualTo(UseItemResult.NotHeld));
        }

        [Test]
        public void ABoost_ShouldRunExtendAndEndOnItsBoundary()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("RentBoost8h", 2);

            fixture.Bag.Use("RentBoost8h", 1, 0);
            Assert.That(fixture.Boosts.Multiplier(BoostKind.Rent), Is.EqualTo(1.25));
            fixture.Bag.Use("RentBoost8h", 1, 1000);

            var end = 2 * 28800 * 1000.0;
            Assert.That(fixture.Boosts.NextBoundary(0), Is.EqualTo(end), "a second extends the first, never stacks");
            fixture.Boosts.ApplyDue(end);
            Assert.That(fixture.Boosts.Multiplier(BoostKind.Rent), Is.EqualTo(1));
        }

        [Test]
        public void AFlask_ShouldFillThePoolButNotPastItsCap()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("ManaFlaskSmall", 4);
            fixture.Mana.TrySpend(50, 0);

            fixture.Bag.Use("ManaFlaskSmall", 4, 0);

            Assert.That(fixture.Mana.Amount, Is.EqualTo(fixture.Mana.Cap));
        }

        [Test]
        public void ATome_ShouldPayItsKnowledge()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("KnowledgeTome", 2);

            fixture.Bag.Use("KnowledgeTome", 2, 0);

            Assert.That(fixture.Treasury.Get("Knowledge"), Is.EqualTo(10));
        }

        [Test]
        public void AGrant_ShouldBeFreshAndCountTowardsTheOrb()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("GoldChest1h", 3);

            Assert.That(fixture.Bag.IsFresh("GoldChest1h"), Is.True);
            Assert.That(fixture.Bag.Badge, Is.EqualTo(3));
            fixture.Bag.MarkSeen("GoldChest1h");
            fixture.Bag.MarkOpened();
            Assert.That(fixture.Bag.IsFresh("GoldChest1h"), Is.False);
            Assert.That(fixture.Bag.Badge, Is.EqualTo(0));
        }

        [Test]
        public void ASpeedup_ShouldCutABuildButNeverPastNow()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("ConstructionSpeedup15m", 1);
            fixture.Bag.Grant("GeneralSpeedup1h", 1);
            fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0);
            var job = SpeedJob.Construction(fixture.City.Jobs[0].Id);

            Assert.That(fixture.Speedups.For(job).Select(i => i.Id), Is.EqualTo(new[] { "ConstructionSpeedup15m", "GeneralSpeedup1h" }),
                "typed first, then General");
            fixture.Speedups.Use(job, "ConstructionSpeedup15m", 1, 0);
            Assert.That(fixture.Speedups.RemainingSeconds(job, 0), Is.EqualTo(2700));

            fixture.Speedups.Use(job, "GeneralSpeedup1h", 1, 600 * 1000);
            Assert.That(fixture.City.Jobs, Is.Empty, "finished at once");
            Assert.That(fixture.City.Districts.Last().Built, Is.True);
        }

        [Test]
        public void ATrainingSpeedup_ShouldNotFitABuild()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("TrainingSpeedup1h", 1);
            fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0);
            var job = SpeedJob.Construction(fixture.City.Jobs[0].Id);

            Assert.That(fixture.Speedups.Use(job, "TrainingSpeedup1h", 1, 0), Is.EqualTo(SpeedupRefusal.DoesNotFit));
        }

        [Test]
        public void Auto_ShouldPickTheFewestThatFinish()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("GeneralSpeedup5m", 10);
            fixture.Bag.Grant("GeneralSpeedup1h", 1);
            fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0);
            var job = SpeedJob.Construction(fixture.City.Jobs[0].Id);

            Assert.That(fixture.Speedups.AutoPlan(job, 0), Is.EqualTo(new[] { ("GeneralSpeedup1h", 1) }));
            Assert.That(fixture.Speedups.AutoPlan(job, 3000 * 1000), Is.EqualTo(new[] { ("GeneralSpeedup1h", 1) }),
                "ten minutes left: one item beats two");
        }

        [Test]
        public void Auto_ShouldTopUpWithTheSmallestThatCovers()
        {
            var fixture = new Fixture();
            fixture.Bag.Grant("GeneralSpeedup5m", 10);
            fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0);
            var job = SpeedJob.Construction(fixture.City.Jobs[0].Id);

            Assert.That(fixture.Speedups.AutoPlan(job, 2880 * 1000), Is.EqualTo(new[] { ("GeneralSpeedup5m", 3) }),
                "twelve minutes left: three, the last one covering the two over");
        }

        // A House of two, a Rent boost for half an hour: the rent it piles up over two hours is the same replayed in one
        // advance as ticked a minute at a time — the boost ends on its own boundary either way.
        private sealed class RentFixture : HarvestFixture
        {
            public RentFixture()
                : base(1, new Codigames.Kingdom.Research.ITechnology[0], new BuildingBuilder().WithId("Housing").WithHousing(2)
                    .WithStorage(100000).WithLevelPrices(BuildingBuilder.Price("Gold", 10)).Build())
            {
                Boosts = new Boosts(State);
                Stores = new Stores(City, Buildings, new Economy(), Treasury, Construction, Bonuses, Boosts);
                new BoostEffects(Boosts, Stores, Mana);
                Timeline.Register(Boosts);
                Timeline.Register(Stores);
                City.Districts.Add(new DistrictState { Id = "house", DefinitionId = "Housing", Ordinal = 1, Level = 1, Anchor = new Vector2Int(4, 0), Built = true });
                City.Population = 2;
                Stores.WakeAll(0);
                Boosts.Start(BoostKind.Rent, 1.25, 1800, 0);
            }

            public BagState State { get; } = new();
            public Boosts Boosts { get; }
            public Stores Stores { get; }
        }

        [Test]
        public void ARentBoost_ShouldReplayTheSameInOneAdvanceAsStepByStep()
        {
            const double END = 2 * 3600 * 1000;
            var once = new RentFixture();
            once.Timeline.Advance(END);

            var stepped = new RentFixture();
            for (var t = 60 * 1000.0; t <= END; t += 60 * 1000) stepped.Timeline.Advance(t);

            var house = once.City.Districts.First(d => d.Id == "house");
            var steppedHouse = stepped.City.Districts.First(d => d.Id == "house");
            Assert.That(once.Stores.Held(house, END), Is.EqualTo(stepped.Stores.Held(steppedHouse, END)).Within(1e-6));
            Assert.That(once.Stores.Held(house, END), Is.GreaterThan(stepped.Stores.GoldPerMinute(steppedHouse) * 120), "the boosted half hour paid more");
        }

        [Test]
        public void Tabs_ShouldSortItemsByWhatTheyAre()
        {
            var fixture = new Fixture();
            foreach (var id in new[] { "GoldChest1h", "ChoiceChest1h", "RentBoost8h", "ManaFlaskSmall", "GeneralSpeedup5m" })
                fixture.Bag.Grant(id, 1);

            Assert.That(fixture.Bag.In(BagTab.Resources).Select(i => i.Id), Is.EqualTo(new[] { "GoldChest1h", "ChoiceChest1h" }));
            Assert.That(fixture.Bag.In(BagTab.SpeedUps).Select(i => i.Id), Is.EqualTo(new[] { "GeneralSpeedup5m" }));
            Assert.That(fixture.Bag.In(BagTab.Other).Select(i => i.Id), Is.EqualTo(new[] { "ManaFlaskSmall" }));
            Assert.That(fixture.Bag.IsFresh(BagTab.Boosts), Is.True);

            fixture.Bag.MarkSeen("RentBoost8h");
            Assert.That(fixture.Bag.IsFresh(BagTab.Boosts), Is.False);
        }

        [Test]
        public void FirstJobFor_ShouldFindARunningTimerTheSpeedupFits()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Speedups.FirstJobFor("GeneralSpeedup5m", 0), Is.Null);

            fixture.Construction.Build("Housing", new Vector2Int(4, 0), 0);

            Assert.That(fixture.Speedups.FirstJobFor("GeneralSpeedup5m", 0)?.JobId, Is.EqualTo(fixture.City.Jobs[0].Id));
            Assert.That(fixture.Speedups.FirstJobFor("TrainingSpeedup1h", 0), Is.Null);
        }
    }
}
