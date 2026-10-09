using Codigames.Kingdom.City;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Goods
{
    public class GoodsPricesTests
    {
        // A Well that asks a block of cut stone to build; houses whose level 2 asks two planks.
        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(
                new BuildingBuilder().WithId("Well").WithMaxLevel(1).WithBuildSeconds(10).WithHarmony(6)
                    .WithLevelPrices(BuildingBuilder.Price("Gold", 10, "CutStone", 1)).Build(),
                new BuildingBuilder().WithId("Housing").WithMaxLevel(2).WithBuildSeconds(10).WithHousing(2, 4)
                    .WithLevelPrices(BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10, "Planks", 2)).Build())
            {
                City.Builders = 3;
            }
        }

        [Test]
        public void ABuild_ShouldBeRefusedWithoutItsGoods_AndPayThemWhenItStarts()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Construction.BuildRefusal("Well"), Is.EqualTo(ConstructionRefusal.NotEnoughGoods));
            Assert.That(fixture.Construction.Build("Well", new Vector2Int(3, 3), 0), Is.EqualTo(ConstructionRefusal.NotEnoughGoods));

            fixture.Stockpile.Add("CutStone", 1);
            Assert.That(fixture.Construction.Build("Well", new Vector2Int(3, 3), 0), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Stockpile.Get("CutStone"), Is.EqualTo(0));
        }

        [Test]
        public void ALevel_ShouldSayWhichPurseIsShort_AndSpendExactlyItsGoods()
        {
            var fixture = new Fixture();
            var house = fixture.Stand("Housing", new Vector2Int(3, 3));

            var offer = fixture.Construction.UpgradeOffer(house.Id);
            Assert.That(offer.Refusal, Is.EqualTo(ConstructionRefusal.NotEnoughGoods));
            Assert.That(offer.Goods["Planks"], Is.EqualTo(2));

            fixture.Stockpile.Add("Planks", 3);
            Assert.That(fixture.Construction.Upgrade(house.Id, 0), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Stockpile.Get("Planks"), Is.EqualTo(1));
        }

        [Test]
        public void ALevelFreeOfGoods_ShouldLeaveTheStockpileAlone()
        {
            var fixture = new Fixture();
            fixture.Stockpile.Add("Planks", 3);

            fixture.Construction.Build("Housing", new Vector2Int(3, 3), 0);

            Assert.That(fixture.Stockpile.Get("Planks"), Is.EqualTo(3));
        }
    }
}
