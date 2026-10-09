using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Goods;
using Codigames.Kingdom.Goods.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Goods
{
    public class WorkshopsTests
    {
        private const double MINUTE = 60_000;

        private sealed class Settings : IEconomySettings, IWorkerSettings, IRushSettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
            public double MoveSpeedTilesPerSecond => 1;
            public double SecondsPerGem => 5;
        }

        private sealed class Good : IGoodDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Tier { get; set; } = 1;
            public IReadOnlyDictionary<string, double> Input { get; set; } = new Dictionary<string, double>();
            public double InputMana { get; set; }
            public string InputGood { get; set; }
            public int InputGoodAmount { get; set; }
            public double WorkSeconds { get; set; }
            public bool Precious { get; set; }
        }

        // A carpenter (Planks: 10 Wood, 10 minutes) with a queue of 3, a rune carver (Runestone: 2 Planks and 20 Mana, an
        // hour), and a crew of up to three each.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture() : base(1, new Codigames.Kingdom.Research.ITechnology[0],
                new BuildingBuilder().WithId("Carpenter").WithMaxLevel(2).WithBuildSeconds(10).WithCrew("", 3, 0).Producing("Planks", 3, 4)
                    .WithLevelPrices(BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10)).Build(),
                new BuildingBuilder().WithId("RuneCarver").WithMaxLevel(1).WithBuildSeconds(10).WithCrew("", 3, 0).Producing("Runestone", 3).Build())
            {
                Goods = new Catalog<IGoodDefinition>(new IGoodDefinition[]
                {
                    new Good { Id = "Planks", Input = new Dictionary<string, double> { ["Wood"] = 10 }, WorkSeconds = 600 },
                    new Good { Id = "Runestone", Tier = 2, InputMana = 20, InputGood = "Planks", InputGoodAmount = 2, WorkSeconds = 3600 },
                    new Good { Id = "Starmetal", Tier = 3, Precious = true },
                });
                var settings = new Settings();
                Stock = new Stockpile(State.Goods, Goods);
                Stores = new Stores(City, Buildings, settings, Treasury, Construction);
                Crews = new Workforce(City, Buildings, Revealed, Harvesting, Stores, settings);
                Workshops = new Workshops(State.Goods, City, Buildings, Goods, Stock, Treasury, Mana, Crews, settings);
                Timeline.Register(Workshops);
                City.Population = 6;
                Treasury.Add("Wood", 1000);
                Shop = Stand("Carpenter", new Vector2Int(3, -3));
                Carver = Stand("RuneCarver", new Vector2Int(-3, 3));
            }

            public KingdomState State { get; } = new();
            public Catalog<IGoodDefinition> Goods { get; }
            public Stockpile Stock { get; }
            public Stores Stores { get; }
            public Workforce Crews { get; }
            public Workshops Workshops { get; }
            public DistrictState Shop { get; }
            public DistrictState Carver { get; }

            public void Crew(DistrictState district, int n, double now)
            {
                for (var i = 0; i < n; i++) Crews.Assign(district.Id, now);
            }
        }

        [Test]
        public void AWorkshopWithNoCrew_ShouldNotAdvanceAtAll()
        {
            var fixture = new Fixture();
            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);

            fixture.Timeline.Advance(60 * MINUTE);

            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(0));
            Assert.That(fixture.Workshops.FrontSeconds(fixture.Shop.Id, 60 * MINUTE), Is.Null);
        }

        [Test]
        public void OneWorker_ShouldMakeOneItemInItsAuthoredTime_FromTheMomentItArrives()
        {
            var fixture = new Fixture();
            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);
            fixture.Crew(fixture.Shop, 1, 5 * MINUTE);

            fixture.Timeline.Advance(15 * MINUTE - 1);
            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(0));

            fixture.Timeline.Advance(15 * MINUTE);
            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(1));
        }

        [Test]
        public void TheCrew_ShouldShareTheQueue_AtOneItemTimePerWorker()
        {
            // Two on one item: half the time.
            var two = new Fixture();
            two.Workshops.Enqueue(two.Shop.Id, 0);
            two.Crew(two.Shop, 2, 0);
            two.Timeline.Advance(5 * MINUTE);
            Assert.That(two.Stock.Get("Planks"), Is.EqualTo(1));

            // Two on two items: both in the whole time.
            var pair = new Fixture();
            pair.Workshops.Enqueue(pair.Shop.Id, 0);
            pair.Workshops.Enqueue(pair.Shop.Id, 0);
            pair.Crew(pair.Shop, 2, 0);
            pair.Timeline.Advance(10 * MINUTE - 1);
            Assert.That(pair.Stock.Get("Planks"), Is.EqualTo(0));
            pair.Timeline.Advance(10 * MINUTE);
            Assert.That(pair.Stock.Get("Planks"), Is.EqualTo(2));

            // Three on two items: two thirds of the time.
            var three = new Fixture();
            three.Workshops.Enqueue(three.Shop.Id, 0);
            three.Workshops.Enqueue(three.Shop.Id, 0);
            three.Crew(three.Shop, 3, 0);
            three.Timeline.Advance(400_000);
            Assert.That(three.Stock.Get("Planks"), Is.EqualTo(2));
        }

        [Test]
        public void TheQueue_ShouldBeAsLongAsTheLevelSays()
        {
            var fixture = new Fixture();
            for (var i = 0; i < 3; i++) Assert.That(fixture.Workshops.Enqueue(fixture.Shop.Id, 0), Is.EqualTo(WorkshopRefusal.None));

            Assert.That(fixture.Workshops.Enqueue(fixture.Shop.Id, 0), Is.EqualTo(WorkshopRefusal.QueueFull));

            fixture.Shop.Level = 2;
            Assert.That(fixture.Workshops.Capacity(fixture.Shop), Is.EqualTo(4));
        }

        [Test]
        public void AnItem_ShouldBePaidWhenQueued_AndRefundedWholeOnCancel()
        {
            var fixture = new Fixture();
            var wood = fixture.Treasury.Get("Wood");

            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);
            Assert.That(fixture.Treasury.Get("Wood"), Is.EqualTo(wood - 10));

            fixture.Workshops.Cancel(fixture.Shop.Id, 0, 0);
            Assert.That(fixture.Treasury.Get("Wood"), Is.EqualTo(wood));
            Assert.That(fixture.Workshops.Queue(fixture.Shop.Id), Is.Empty);
        }

        [Test]
        public void TheSecondTierGood_ShouldTakeItsGoodsAndMana_AndBeRefusedWithoutThem()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Workshops.Enqueue(fixture.Carver.Id, 0), Is.EqualTo(WorkshopRefusal.NotEnoughGoods));

            fixture.Stock.Add("Planks", 2);
            var mana = fixture.Treasury.Get("Mana");
            Assert.That(fixture.Workshops.Enqueue(fixture.Carver.Id, 0), Is.EqualTo(WorkshopRefusal.None));
            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(0));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(mana - 20));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking()
        {
            var once = new Fixture();
            var stepped = new Fixture();
            foreach (var f in new[] { once, stepped })
            {
                for (var i = 0; i < 3; i++) f.Workshops.Enqueue(f.Shop.Id, 0);
                f.Crew(f.Shop, 2, 0);
            }

            once.Timeline.Advance(25 * MINUTE + 333);
            for (double t = 0; t <= 25 * MINUTE + 333; t += 7_777) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(25 * MINUTE + 333);

            Assert.That(once.Stock.Get("Planks"), Is.EqualTo(stepped.Stock.Get("Planks")));
            Assert.That(once.Workshops.Queue(once.Shop.Id).Select(i => i.WorkMs), Is.EqualTo(stepped.Workshops.Queue(stepped.Shop.Id).Select(i => i.WorkMs)));
        }

        [Test]
        public void Gems_ShouldBePricedOnTheTimeLeft_AndFinishOnlyTheFrontItem()
        {
            var fixture = new Fixture();
            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);
            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);
            fixture.Crew(fixture.Shop, 1, 0);

            Assert.That(fixture.Workshops.RushCost(fixture.Shop.Id, 0), Is.EqualTo(120));
            Assert.That(fixture.Workshops.Rush(fixture.Shop.Id, 0), Is.EqualTo(WorkshopRefusal.None));
            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(1));
            Assert.That(fixture.Workshops.Queue(fixture.Shop.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void ASpeedUp_ShouldAddTheCrewsWork_OrHandTheItemOverNow()
        {
            var fixture = new Fixture();
            fixture.Workshops.Enqueue(fixture.Shop.Id, 0);
            fixture.Crew(fixture.Shop, 1, 0);

            fixture.Workshops.Cut(fixture.Shop.Id, 300, 0);
            Assert.That(fixture.Workshops.FrontSeconds(fixture.Shop.Id, 0), Is.EqualTo(300));

            fixture.Workshops.Cut(fixture.Shop.Id, 3600, 0);
            Assert.That(fixture.Stock.Get("Planks"), Is.EqualTo(1));
        }

        [Test]
        public void APrice_ShouldLeavePreciousMaterialsOut_WhileTheyAreNotInPlay()
        {
            var fixture = new Fixture();
            var price = new Dictionary<string, double> { ["Planks"] = 2, ["Starmetal"] = 5 };

            Assert.That(fixture.Stock.Priced(price).Keys, Is.EqualTo(new[] { "Planks" }));
            fixture.Stock.Add("Planks", 2);
            Assert.That(fixture.Stock.CanAfford(price), Is.True);
        }
    }
}
