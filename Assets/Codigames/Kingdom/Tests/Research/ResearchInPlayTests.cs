using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Research
{
    // What the tree holds shut and what it raises, read where each thing is decided.
    public class ResearchInPlayTests
    {
        private static IBuildingDefinition Farm(params int[] caps)
            => new BuildingBuilder().WithId("Farm").WithLevelPrices(BuildingBuilder.Price("Gold", 10), BuildingBuilder.Price("Gold", 10))
                .WithMaxLevel(2).WithBuildSeconds(10).WithMaxCountPerTownhallLevel(caps).Build();

        [Test]
        public void ABuilding_ShouldWaitForTheTechnologyThatOpensIt()
        {
            var fixture = new CityFixture(new[] { new TechBuilder("Farming").Opens(UnlockKind.District, "Farm").Build() }, Farm());

            Assert.That(fixture.Construction.BuildRefusal("Farm"), Is.EqualTo(ConstructionRefusal.NeedsResearch));
            Assert.That(fixture.Construction.Offer("Farm").RequiredTech, Is.EqualTo("Farming"));

            fixture.ResearchState.Completed.Add("Farming");

            Assert.That(fixture.Construction.BuildRefusal("Farm"), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Construction.Offer("Farm").RequiredTech, Is.Null);
        }

        [Test]
        public void ALevel_ShouldWaitForItsTechnology()
        {
            var fixture = new CityFixture(new[] { new TechBuilder("Forestry").Opens(UnlockKind.DistrictLevel, "Townhall", 2).Build() });
            var hall = fixture.District("Townhall");

            Assert.That(fixture.Construction.UpgradeRefusal(hall), Is.EqualTo(ConstructionRefusal.NeedsResearch));
            Assert.That(fixture.Construction.UpgradeOffer(hall.Id).RequiredTech, Is.EqualTo("Forestry"));

            fixture.ResearchState.Completed.Add("Forestry");

            Assert.That(fixture.Construction.UpgradeRefusal(hall), Is.EqualTo(ConstructionRefusal.None));
        }

        [Test]
        public void OneMore_ShouldStandOnceItsTechnologyIsDone()
        {
            var fixture = new CityFixture(new[] { new TechBuilder("Granaries").Opens(UnlockKind.DistrictCount, "Farm").Build() }, Farm(1));
            Assert.That(fixture.Construction.Build("Farm", new Vector2Int(3, 0), 0), Is.EqualTo(ConstructionRefusal.None));
            fixture.Timeline.Advance(20_000);

            Assert.That(fixture.Construction.BuildRefusal("Farm"), Is.EqualTo(ConstructionRefusal.AtCap));

            fixture.ResearchState.Completed.Add("Granaries");

            Assert.That(fixture.Construction.BuildRefusal("Farm"), Is.EqualTo(ConstructionRefusal.None));
            Assert.That(fixture.Construction.Offer("Farm").Cap, Is.EqualTo(2));
        }

        [Test]
        public void BuildSpeed_ShouldDivideTheWait()
        {
            var fixture = new CityFixture(new[] { new TechBuilder("Carpentry").Moves(TechStats.BUILD_SPEED, EffectOp.Percent, 100).Build() }, Farm());
            var before = fixture.Construction.Offer("Farm").Seconds;

            fixture.ResearchState.Completed.Add("Carpentry");

            Assert.That(fixture.Construction.Offer("Farm").Seconds, Is.EqualTo(before / 2));
        }

        [Test]
        public void AGatedSource_ShouldRefuseATapAndSpendNoMana()
        {
            var fixture = new HarvestFixture(1, new TechBuilder("Forestry").Opens(UnlockKind.Harvest, "Forest").Build());
            var mana = fixture.Mana.Amount;

            var refused = fixture.Harvesting.Tap(HarvestFixture.TREE, 0);

            Assert.That(refused.Refusal, Is.EqualTo(TapRefusal.NeedsResearch));
            Assert.That(refused.RequiredTech, Is.EqualTo("Forestry"));
            Assert.That(fixture.Mana.Amount, Is.EqualTo(mana));
            Assert.That(fixture.Harvesting.Draw(HarvestFixture.TREE, 1, 0), Is.EqualTo(0), "no crew works it either");

            fixture.ResearchState.Completed.Add("Forestry");

            Assert.That(fixture.Harvesting.Tap(HarvestFixture.TREE, 0).Refusal, Is.EqualTo(TapRefusal.None));
        }

        [Test]
        public void HarvestYield_ShouldRaiseWhatATapTakesAndCarryTheFraction()
        {
            var fixture = new HarvestFixture(1, new TechBuilder("Sawpits I").Moves(TechStats.HARVEST_YIELD, EffectOp.Percent, 50,
                TargetKind.Harvest, "Forest").Build());
            fixture.ResearchState.Completed.Add("Sawpits I");

            var first = fixture.Harvesting.Tap(HarvestFixture.TREE, 0).Paid;
            var second = fixture.Harvesting.Tap(HarvestFixture.TREE, 0).Paid;

            Assert.That(first + second, Is.EqualTo(3), "1.5 a tap: one, then two with the half carried");
        }

        [Test]
        public void RegrowthSpeed_ShouldShortenTheWaitOfAnEmptiedCell()
        {
            var fixture = new HarvestFixture(1, new TechBuilder("Reforestation I").Moves(TechStats.REGROWTH_SPEED, EffectOp.Percent, 100,
                TargetKind.Harvest, "Forest").Build());
            fixture.ResearchState.Completed.Add("Reforestation I");

            for (var i = 0; i < 10 && !fixture.Harvesting.IsExhausted(HarvestFixture.TREE, 0); i++) fixture.Harvesting.Tap(HarvestFixture.TREE, 0);

            Assert.That(fixture.Harvesting.RecoversAt(HarvestFixture.TREE), Is.EqualTo(90_000));
        }
    
        private sealed class Economy : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        private sealed class HallFixture : CityFixture
        {
            public HallFixture(params ITechnology[] technologies) : base(technologies)
            {
                Stores = new Stores(City, Buildings, new Economy(), Treasury, Construction, Bonuses);
                Timeline.Register(Stores);
                Stores.WakeAll(0);
            }

            public Stores Stores { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithOwnGold(10).WithStorage(600).Build();
        }

        [Test]
        public void ARaise_ShouldNeverRepriceWhatAStoreAlreadyMade()
        {
            var fixture = new HallFixture(new TechBuilder("Exchequer").Moves(TechStats.OWN_GOLD, EffectOp.Percent, 100).Build());
            var hall = fixture.District("Townhall");

            fixture.Stores.SettleAll(30_000);
            fixture.ResearchState.Completed.Add("Exchequer");

            Assert.That(fixture.Stores.Held(hall, 60_000), Is.EqualTo(15), "five at ten a minute, then ten at twenty");
        }
    }
}
