using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class VillagerTrainingTests
    {
        private sealed class Settings : ITrainingSettings, IEconomySettings
        {
            public IReadOnlyList<double> CostFirst => new double[] { 5, 10, 20 };
            public double CostGrowth => 1.1;
            public double Seconds => 20;
            public double SecondsGrowth => 1.07;
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        // Two built houses of two beds each, Food to spend.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(new BuildingBuilder().WithId("Housing").WithLevelPrices(BuildingBuilder.Price("Gold", 10))
                .WithBuildSeconds(1).WithHousing(2).WithStorage(3600).Build())
            {
                City.Builders = 2;
                Construction.Build("Housing", new Vector2Int(3, 3), 0);
                Construction.Build("Housing", new Vector2Int(-3, -3), 0);
                Timeline.Advance(1000);

                var settings = new Settings();
                Stores = new Stores(City, Buildings, settings, Treasury, Construction);
                Training = new VillagerTraining(City, settings, Treasury, Stores);
                Timeline.Register(Training);
                Timeline.Register(Stores);
                Treasury.Add("Food", 1000);
            }

            public Stores Stores { get; }
            public VillagerTraining Training { get; }
        }

        [Test]
        public void Train_ShouldPayTheNextVillagerAndStartTheirClock()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Training.Train(1000), Is.EqualTo(TrainRefusal.None));
            Assert.That(fixture.Treasury.Get("Food"), Is.EqualTo(995));
            Assert.That(fixture.Training.Current.ArrivesAt, Is.EqualTo(21_000));
        }

        [Test]
        public void Queue_ShouldPriceEachAsIfThoseAheadHadArrived()
        {
            var fixture = new Fixture();
            fixture.Training.Train(1000);
            fixture.Training.Train(1000);

            Assert.That(fixture.Treasury.Get("Food"), Is.EqualTo(1000 - 5 - 10));
            Assert.That(fixture.City.Trainees[1].StartedAt, Is.Null);
        }

        [Test]
        public void Villagers_ShouldArriveOneAtATimeEachSlower()
        {
            var fixture = new Fixture();
            fixture.Training.Train(1000);
            fixture.Training.Train(1000);

            fixture.Timeline.Advance(21_000);
            Assert.That(fixture.City.Population, Is.EqualTo(1));
            Assert.That(fixture.Training.Current.Seconds, Is.EqualTo(21));

            fixture.Timeline.Advance(42_000);
            Assert.That(fixture.City.Population, Is.EqualTo(2));
            Assert.That(fixture.City.Trainees, Is.Empty);
        }

        [Test]
        public void Arrived_ShouldFindTheNextVillagerAlreadyOnTheirWay()
        {
            var fixture = new Fixture();
            fixture.Training.Train(1000);
            fixture.Training.Train(1000);
            double? next = null;
            fixture.Training.Arrived += () => next = fixture.Training.Current?.ArrivesAt;

            fixture.Timeline.Advance(21_000);

            Assert.That(next, Is.EqualTo(21_000 + 21_000));
        }

        [Test]
        public void Train_ShouldBeRefusedWithoutABed()
        {
            var fixture = new Fixture();
            for (var i = 0; i < 4; i++) fixture.Training.Train(1000);

            Assert.That(fixture.Training.Train(1000), Is.EqualTo(TrainRefusal.NoRoom));
        }

        [Test]
        public void CostAt_ShouldGrowPastTheAuthoredList()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Training.CostAt(2), Is.EqualTo(20));
            Assert.That(fixture.Training.CostAt(3), Is.EqualTo(22));
            Assert.That(fixture.Training.CostAt(4), Is.EqualTo(24), "20 × 1.1², three significant figures");
        }

        [Test]
        public void AVillagerMovingIn_ShouldStartTheirHousePayingRent()
        {
            var fixture = new Fixture();
            var house = fixture.City.Districts[1];
            fixture.Training.Train(1000);

            fixture.Timeline.Advance(21_000 + 60_000);

            Assert.That(fixture.Stores.Held(house, 81_000), Is.EqualTo(30));
        }

        [Test]
        public void Plan_ShouldPriceAnOrderWholeAndStopAllAtTheBedsOrTheFood()
        {
            var fixture = new Fixture();
            var room = fixture.Training.Room;

            Assert.That(fixture.Training.Plan(TrainAmount.Ten).Cost, Is.EqualTo(fixture.Training.BatchCost(10)));
            Assert.That(fixture.Training.Plan(TrainAmount.All).Count, Is.EqualTo(room));

            fixture.Treasury.TryPay(new Dictionary<string, double> { ["Food"] = 1000 - 15 });
            Assert.That(fixture.Training.Plan(TrainAmount.All).Count, Is.EqualTo(2), "5 + 10 Food, and not 20 more");
        }

        [Test]
        public void TrainCount_ShouldQueueAllOrNone()
        {
            var fixture = new Fixture();
            var room = fixture.Training.Room;

            Assert.That(fixture.Training.Train(room + 1, 0), Is.EqualTo(TrainRefusal.NoRoom));
            Assert.That(fixture.City.Trainees, Is.Empty);

            Assert.That(fixture.Training.Train(room, 0), Is.EqualTo(TrainRefusal.None));
            Assert.That(fixture.City.Trainees, Has.Count.EqualTo(room));
        }

        [Test]
        public void FinishLine_ShouldPayGemsAndBringTheWholeLineIn()
        {
            var fixture = new Fixture();
            fixture.Training.Train(2, 0);
            var gems = fixture.Treasury.Get("Gems");
            var rush = new GemRush(fixture.Construction, fixture.Training, fixture.Treasury, new Rush());
            var cost = rush.LineCost(0);

            Assert.That(rush.FinishLine(0), Is.True);
            Assert.That(fixture.City.Trainees, Is.Empty);
            Assert.That(fixture.Treasury.Get("Gems"), Is.EqualTo(gems - cost));
        }

        private sealed class Rush : IRushSettings
        {
            public double SecondsPerGem => 5;
        }
    }
}
