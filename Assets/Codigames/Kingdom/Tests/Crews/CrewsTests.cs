using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Crews.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Crews
{
    public class CrewsTests
    {
        private sealed class Settings : IEconomySettings, IWorkerSettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
            public double MoveSpeedTilesPerSecond => 1;
        }

        // A Sawmill at (3, 1), two cells from the tree at (3, 3), with a crew of two and a store of 3 units.
        private sealed class Fixture : HarvestFixture
        {
            public static readonly Vector2Int MILL = new(3, 1);

            public Fixture()
            {
                var settings = new Settings();
                Stores = new Stores(City, Buildings, settings, Treasury, Construction);
                Crews = new Codigames.Kingdom.Crews.Workforce(City, Buildings, Revealed, Harvesting, Stores, settings);
                Timeline.Register(Stores);
                Timeline.Register(Crews);

                City.Districts.Add(new DistrictState { Id = "mill", DefinitionId = "Sawmill", Ordinal = 1, Level = 1, Anchor = MILL, Built = true });
                City.Population = 3;
            }

            public Stores Stores { get; }
            public Codigames.Kingdom.Crews.Workforce Crews { get; }
            public DistrictState Mill => City.Districts.First(d => d.Id == "mill");

            protected override IBuildingDefinition[] ExtraBuildings() => new[]
            {
                new BuildingBuilder().WithId("Sawmill").WithCrew("Forest", 2, 2).WithStorage(3).Build(),
            };
        }

        [Test]
        public void Assign_ShouldTakeAFreeVillagerUpToTheLimit()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Crews.Assign("mill", 0), Is.True);
            Assert.That(fixture.Crews.Assign("mill", 0), Is.True);
            Assert.That(fixture.Crews.Assign("mill", 0), Is.False, "the limit is two");
            Assert.That(fixture.Crews.FreeVillagers, Is.EqualTo(1));
        }

        [Test]
        public void AWorker_ShouldWalkStrikeAndBringTheLoadHome()
        {
            var fixture = new Fixture();
            fixture.Crews.Assign("mill", 0);
            var worker = fixture.City.Workers[0];

            Assert.That(worker.Activity, Is.EqualTo(WorkerActivity.MovingToCell));
            Assert.That(worker.ClaimedCell, Is.EqualTo(HarvestFixture.TREE));

            // Two cells there, ten seconds of swing, two cells back.
            fixture.Timeline.Advance(13_999);
            Assert.That(fixture.Stores.Held(fixture.Mill, 13_999), Is.EqualTo(0));

            fixture.Timeline.Advance(14_000);
            Assert.That(fixture.Stores.Held(fixture.Mill, 14_000), Is.EqualTo(1));
            Assert.That(fixture.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(9));
        }

        [Test]
        public void AFullStore_ShouldKeepTheCrewAtTheDoorUntilCollected()
        {
            var fixture = new Fixture();
            fixture.Crews.Assign("mill", 0);
            var worker = fixture.City.Workers[0];

            fixture.Timeline.Advance(3_600_000);

            Assert.That(fixture.Stores.Held(fixture.Mill, 3_600_000), Is.EqualTo(3));
            Assert.That(worker.Activity, Is.EqualTo(WorkerActivity.Idle));

            fixture.Stores.Collect(fixture.Mill, 3_600_000);
            fixture.Timeline.Advance(3_600_000 + 14_000);
            Assert.That(fixture.Stores.Held(fixture.Mill, 3_600_000 + 14_000), Is.EqualTo(1));
        }

        [Test]
        public void TwoWorkers_ShouldNeverHoldTheSameCell()
        {
            var fixture = new Fixture();
            fixture.Ground.Features[new Vector2Int(4, 3)] = "Trees";
            fixture.Crews.Assign("mill", 0);
            fixture.Crews.Assign("mill", 0);

            var cells = fixture.City.Workers.Select(w => w.ClaimedCell).ToList();
            Assert.That(cells.Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void OneAdvance_ShouldEqualSteppedTicking()
        {
            var once = new Fixture();
            var stepped = new Fixture();
            foreach (var f in new[] { once, stepped })
            {
                f.Ground.Features[new Vector2Int(4, 3)] = "Trees";
                f.Crews.Assign("mill", 0);
                f.Crews.Assign("mill", 0);
            }

            once.Timeline.Advance(600_000);
            for (var t = 0; t <= 600_000; t += 777) stepped.Timeline.Advance(t);
            stepped.Timeline.Advance(600_000);

            Assert.That(once.Stores.Held(once.Mill, 600_000), Is.EqualTo(stepped.Stores.Held(stepped.Mill, 600_000)));
            Assert.That(once.Harvesting.UnitsAt(HarvestFixture.TREE), Is.EqualTo(stepped.Harvesting.UnitsAt(HarvestFixture.TREE)));
            Assert.That(once.City.Workers.Select(w => w.Activity), Is.EqualTo(stepped.City.Workers.Select(w => w.Activity)));
        }
    }
}
