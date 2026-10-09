using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class PlantingTests
    {
        private static readonly Vector2Int PLOT = new(4, 0);

        // A crop plot that plants Crops: 15 Gold for the first, dearer for each standing, five seconds to grow.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture()
                : base(1, new Codigames.Kingdom.Research.ITechnology[0], new BuildingBuilder().WithId("FarmLands").WithSize(1, 1)
                    .Planting("Crops").WithLevelPrices(BuildingBuilder.Price("Gold", 15)).WithInstanceGrowth(0.5, 1)
                    .WithMaxCountPerTownhallLevel(2).Build())
            {
                Planting = new Construction(City, Treasury, Placement, Buildings, Settings, Gates, Bonuses, Harvesting);
            }

            public Construction Planting { get; }
        }

        [Test]
        public void APlot_ShouldPlantItsFeatureGrowing_WithNoDistrictAndNoBuilder()
        {
            var fixture = new Fixture();
            fixture.City.Builders = 0;
            var districts = fixture.City.Districts.Count;

            var refusal = fixture.Planting.Build("FarmLands", PLOT, 1000);

            Assert.That(refusal, Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Ground.Features[PLOT], Is.EqualTo("Crops"));
            Assert.That(fixture.City.Districts.Count, Is.EqualTo(districts), "no district");
            Assert.That(fixture.Harvesting.IsGrowing(PLOT, 1000), Is.True);
            Assert.That(fixture.Harvesting.Tap(PLOT, 1000).Refusal, Is.EqualTo(TapRefusal.Exhausted), "a growing plot cannot be tapped");
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(CityFixture.START_GOLD - 15));
        }

        [Test]
        public void APlot_ShouldBeFullOnceGrown()
        {
            var fixture = new Fixture();
            fixture.Timeline.Register(fixture.Planting);
            fixture.Planting.Build("FarmLands", PLOT, 1000);

            Assert.That(fixture.Harvesting.Regrowth(PLOT, 3500), Is.EqualTo(0.5).Within(1e-9));
            fixture.Timeline.Advance(6000);

            Assert.That(fixture.Harvesting.IsGrowing(PLOT, 6000), Is.False);
            Assert.That(fixture.Harvesting.UnitsAt(PLOT), Is.EqualTo(10));
            Assert.That(fixture.Harvesting.Tap(PLOT, 6000).Refusal, Is.EqualTo(TapRefusal.None));
        }

        [Test]
        public void ThePlotsStanding_ShouldPriceAndCapTheNext()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Planting.Offer("FarmLands").Ordinal, Is.EqualTo(1));

            fixture.Planting.Build("FarmLands", PLOT, 0);
            var second = fixture.Planting.Offer("FarmLands");
            fixture.Planting.Build("FarmLands", new Vector2Int(4, 1), 0);

            Assert.That(second.Ordinal, Is.EqualTo(2));
            Assert.That(second.Price["Gold"], Is.GreaterThan(15));
            Assert.That(fixture.Planting.BuildRefusal("FarmLands"), Is.EqualTo(ConstructionRefusal.AtCap));
        }

        [Test]
        public void ARuinedPlot_ShouldBeRepairedByPlantingIt()
        {
            var fixture = new Fixture();

            fixture.Planting.Repair("FarmLands", PLOT, 30, 0);

            Assert.That(fixture.Ground.Features[PLOT], Is.EqualTo("Crops"));
            Assert.That(fixture.City.Districts.Any(d => d.DefinitionId == "FarmLands"), Is.False);
            Assert.That(fixture.Harvesting.Regrowth(PLOT, 2500), Is.EqualTo(0.5).Within(1e-9), "its growth, not the repair's wait");
        }
    }
}
