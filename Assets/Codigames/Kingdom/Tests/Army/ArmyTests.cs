using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Army.State;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Army
{
    public class ArmyTests
    {
        private sealed class Settings : IArmySettings, IRushSettings
        {
            public double WoundedShare => 0.1;
            public double HealCostShare => 0.3;
            public double HealTimeShare => 0.25;
            public double SecondsPerGem => 5;
        }

        private sealed class Unit : IUnitDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; } = "";
            public IReadOnlyList<string> Tags { get; set; } = new[] { "Melee" };
            public int SquadSize { get; set; } = 100;
            public int Frontage { get; set; } = 15;
            public int Cooldown { get; set; } = 10;
            public int Speed { get; set; } = 10;
            public int Range { get; set; } = 90;
            public IReadOnlyList<UnitRank> Ranks { get; set; }
        }

        private sealed class HallRules : IAdjacencyRules
        {
            public IReadOnlyList<AdjacencyRule> Rules { get; } = new[] { new AdjacencyRule("AnyHall", "AnyHall", AdjacencyStat.TrainTime, -0.1) };
        }

        // Warriors (I: 100 Gold, 15 s; II: 200 Gold, 23 s from hall level 3) trained at Barracks of 150, 250, 400 a level,
        // and an Infirmary of 30 beds.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(
                new BuildingBuilder().WithId("Barracks").WithMaxLevel(3).WithSize(2, 2).WithBuildSeconds(10).Training("Warrior", 150, 250, 400).Build(),
                new BuildingBuilder().WithId("Infirmary").WithMaxLevel(1).WithSize(2, 2).WithBuildSeconds(10).WithBeds(30).Build())
            {
                Units = new Catalog<IUnitDefinition>(new IUnitDefinition[]
                {
                    new Unit
                    {
                        Id = "Warrior", Name = "Warrior", Ranks = new[]
                        {
                            new UnitRank(4, 5, 6, 60, 3, new Dictionary<string, double> { ["Gold"] = 100 }, 15, 1),
                            new UnitRank(6, 8, 8, 96, 5, new Dictionary<string, double> { ["Gold"] = 200 }, 23, 3),
                        },
                    },
                });
                Adjacency = new Adjacency(City, Buildings, new HallRules(), new BuildingGroups(Buildings));
                Army = new Codigames.Kingdom.Army.Army(State, City, Buildings, Units, Treasury, new Settings(), new Settings(), Gates, Bonuses, Adjacency);
                Timeline.Register(Army);
                Treasury.Add("Gold", 1_000_000);
                Hall = Stand("Barracks", new Vector2Int(3, -3));
            }

            public ArmyState State { get; } = new();
            public Catalog<IUnitDefinition> Units { get; }
            public Adjacency Adjacency { get; }
            public Codigames.Kingdom.Army.Army Army { get; }
            public DistrictState Hall { get; }
        }

        [Test]
        public void AFirstHall_ShouldAllowAHundredAndAHalf_AndTheCapIsATotalPerLevel()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Army.Cap, Is.EqualTo(150));

            fixture.Hall.Level = 3;
            Assert.That(fixture.Army.Cap, Is.EqualTo(400));
        }

        [Test]
        public void Train_ShouldPayNow_AndCountTheQueuedAgainstTheCap()
        {
            var fixture = new Fixture();
            var gold = fixture.Treasury.Get("Gold");

            Assert.That(fixture.Army.Train(fixture.Hall.Id, "Warrior", 100, 0), Is.EqualTo(ArmyRefusal.None));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold - 10_000));
            Assert.That(fixture.Army.Committed, Is.EqualTo(100));
            Assert.That(fixture.Army.Train(fixture.Hall.Id, "Warrior", 51, 0), Is.EqualTo(ArmyRefusal.AtCapacity));
        }

        [Test]
        public void AHall_ShouldTrainOneRankAtATime_AndARankAsksItsHallLevel()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Army.Refusal(fixture.Hall.Id, "Warrior_e2", 1), Is.EqualTo(ArmyRefusal.HallLevel));

            fixture.Hall.Level = 3;
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 1, 0);
            Assert.That(fixture.Army.Train(fixture.Hall.Id, "Warrior_e2", 1, 0), Is.EqualTo(ArmyRefusal.OtherRank));
        }

        [Test]
        public void ALine_ShouldTrainOneSoldierAfterAnother_EachStartingWhenTheLastFinishes()
        {
            var fixture = new Fixture();
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 3, 0);

            fixture.Timeline.Advance(29_999);
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(1));

            fixture.Timeline.Advance(45_000);
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(3));
            Assert.That(fixture.Army.Line(fixture.Hall.Id), Is.Empty);
        }

        [Test]
        public void AMilitaryQuarter_ShouldTrainFaster()
        {
            var fixture = new Fixture();
            fixture.Stand("Barracks", new Vector2Int(5, -3));

            Assert.That(fixture.Army.TrainSeconds("Warrior", fixture.Hall), Is.EqualTo(14));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking()
        {
            var once = new Fixture();
            var stepped = new Fixture();
            foreach (var f in new[] { once, stepped }) f.Army.Train(f.Hall.Id, "Warrior", 40, 0);

            once.Timeline.Advance(333_333);
            for (double t = 0; t <= 333_333; t += 4_321) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(333_333);

            Assert.That(once.Army.Count("Warrior"), Is.EqualTo(stepped.Army.Count("Warrior")));
            Assert.That(once.Army.Line(once.Hall.Id)[0].StartedAt, Is.EqualTo(stepped.Army.Line(stepped.Hall.Id)[0].StartedAt));
        }

        [Test]
        public void Gems_ShouldFinishTheWholeLine_PricedOnItsTimeLeft()
        {
            var fixture = new Fixture();
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 10, 0);

            Assert.That(fixture.Army.RushCost(fixture.Hall.Id, 0), Is.EqualTo(30));
            Assert.That(fixture.Army.Rush(fixture.Hall.Id, 0), Is.EqualTo(ArmyRefusal.None));
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(10));
        }

        [Test]
        public void ASpeedUp_ShouldSpillFromTheHeadOntoTheNext()
        {
            var fixture = new Fixture();
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 3, 0);

            fixture.Army.Cut(fixture.Hall.Id, 20, 0);

            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(1));
            Assert.That(fixture.Army.RemainingSeconds(fixture.Hall.Id, 0), Is.EqualTo(25));
        }

        [Test]
        public void TheFallen_ShouldReachABed_AShareOfThem_WhileBedsAreFree()
        {
            var fixture = new Fixture();
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 100, 0);
            fixture.Army.Rush(fixture.Hall.Id, 0);

            Assert.That(fixture.Army.Lose(new Dictionary<string, int> { ["Warrior"] = 50 }, 0.1), Is.EqualTo(0), "no Infirmary, no beds");

            fixture.Stand("Infirmary", new Vector2Int(-3, 3));
            Assert.That(fixture.Army.Lose(new Dictionary<string, int> { ["Warrior"] = 40 }, 0.1), Is.EqualTo(4));
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(10));
            Assert.That(fixture.Army.Wounded("Warrior"), Is.EqualTo(4));
        }

        [Test]
        public void Mending_ShouldCostAShareOfTraining_AndBringTheWholeBatchBack_EvenFinishedWithGems()
        {
            var fixture = new Fixture();
            fixture.Stand("Infirmary", new Vector2Int(-3, 3));
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 100, 0);
            fixture.Army.Rush(fixture.Hall.Id, 0);
            fixture.Army.Lose(new Dictionary<string, int> { ["Warrior"] = 100 }, 0.1);

            Assert.That(fixture.Army.HealCost("Warrior", 10)["Gold"], Is.EqualTo(300));
            Assert.That(fixture.Army.HealSeconds("Warrior", 10), Is.EqualTo(38));
            Assert.That(fixture.Army.Heal("Warrior", 0), Is.EqualTo(ArmyRefusal.None));
            Assert.That(fixture.Army.Wounded("Warrior"), Is.EqualTo(0));

            fixture.Army.Rush(fixture.Army.Infirmary.Id, 0);
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(10));
        }

        [Test]
        public void ALineRunningDry_ShouldSaySo_OnceAndNotForMending()
        {
            var fixture = new Fixture();
            var done = new List<string>();
            fixture.Army.LineDone += (hall, troop, at) => done.Add(hall.Id + ":" + at);
            fixture.Army.Train(fixture.Hall.Id, "Warrior", 2, 0);

            fixture.Timeline.Advance(60_000);

            Assert.That(done, Is.EqualTo(new[] { fixture.Hall.Id + ":30000" }));
        }
    }
}
