using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Quests.State;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Quests
{
    public class QuestChainTests
    {
        private sealed class Quest : IQuestDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public GoalType GoalType { get; set; }
            public string GoalTarget { get; set; }
            public double GoalAmount { get; set; } = 1;
            public int GoalLevel { get; set; }
            public IReadOnlyDictionary<string, double> Reward { get; set; } = new Dictionary<string, double>();
            public double RewardKnowledge { get; set; }
            public IReadOnlyDictionary<string, double> RewardItems { get; set; } = new Dictionary<string, double>();
            public bool AutoClaim { get; set; }
            public double TutorialRentSeconds { get; set; }
        }

        // Absolute goals read from a scripted value; the relative ones count what the harvest, the stores and the
        // fog report.
        private sealed class Goals : IQuestGoals
        {
            public double Built { get; set; }
            public double Value(IQuestDefinition quest) => quest.GoalType == GoalType.BuildDistrict ? Built : 0;
        }

        private sealed class FogSettings : IFogSettings
        {
            public IReadOnlyList<double> CostPerRing => new double[] { 4, 8, 20, 85 };
            public double FallbackGrowth => 2;
            public int TapsToReveal => 1;
            public double MinCost => 1;
            public int CountStep => 10;
            public double CountGrowth => 1.05;
            public IReadOnlyList<int> ReachPerTownhallLevel => new[] { 6 };
        }

        private sealed class Economy : IEconomySettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
        }

        private sealed class Fixture : HarvestFixture
        {
            public Fixture(params Quest[] quests)
                : base(1, new Codigames.Kingdom.Research.ITechnology[0], new BuildingBuilder().WithId("Housing").WithHousing(2).WithStorage(600)
                    .WithLevelPrices(BuildingBuilder.Price("Gold", 10)).Build())
            {
                Fog = new FogOfWar(new FogState(), City, Map, Buildings, Settings, new FogSettings(), Treasury);
                Stores = new Stores(City, Buildings, new Economy(), Treasury, Construction);
                Chain = new QuestChain(State, new Catalog<IQuestDefinition>(quests), Goals, Treasury, City, Buildings, Stores, Harvesting, Fog,
                    Ground);
                Timeline.Register(Chain);
            }

            public QuestState State { get; } = new();
            public Goals Goals { get; } = new();
            public FogOfWar Fog { get; }
            public Stores Stores { get; }
            public QuestChain Chain { get; }

            protected override IBuildingDefinition MakeTownhall()
                => new BuildingBuilder().WithId("Townhall").WithMaxLevel(5).WithSize(2, 2).NotBuildable().WithFog(1, 2).Build();
        }

        private static Quest Build(string id = "ARoof") => new() { Id = id, GoalType = GoalType.BuildDistrict, GoalTarget = "Housing",
            Reward = new Dictionary<string, double> { ["Gold"] = 40, ["Mana"] = 30 }, RewardKnowledge = 2 };

        [Test]
        public void AnAbsoluteGoal_ShouldCountWhatWasDoneBeforeItArrived()
        {
            var fixture = new Fixture(Build());
            fixture.Goals.Built = 1;

            Assert.That(fixture.Chain.IsComplete(fixture.Chain.Active), Is.True);
        }

        [Test]
        public void Claim_ShouldPayEveryPurseAndActivateTheNext()
        {
            var fixture = new Fixture(Build(), Build("Second"));
            var gold = fixture.Treasury.Get("Gold");
            var mana = fixture.Treasury.Get("Mana");
            Assert.That(fixture.Chain.Claim(0), Is.EqualTo(QuestClaim.NotComplete));

            fixture.Goals.Built = 1;
            Assert.That(fixture.Chain.Claim(0), Is.EqualTo(QuestClaim.Claimed));

            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold + 40));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(mana + 30), "Mana may overfill the pool");
            Assert.That(fixture.Treasury.Get("Knowledge"), Is.EqualTo(2));
            Assert.That(fixture.Chain.Active.Id, Is.EqualTo("Second"));
        }

        [Test]
        public void ARelativeGoal_ShouldCountOnlyFromItsActivation()
        {
            var fixture = new Fixture(new Quest { Id = "Timber", GoalType = GoalType.CollectResource, GoalTarget = "Wood", GoalAmount = 2 });
            fixture.Harvesting.Tap(HarvestFixture.TREE, 0);

            Assert.That(fixture.Chain.Value(fixture.Chain.Active), Is.EqualTo(1));
            fixture.Harvesting.Tap(HarvestFixture.TREE, 0);
            Assert.That(fixture.Chain.IsComplete(fixture.Chain.Active), Is.True);
        }

        [Test]
        public void TapsAndFoundFeatures_ShouldCountForTheirGoals()
        {
            var taps = new Fixture(new Quest { Id = "Taps", GoalType = GoalType.CollectTaps, GoalAmount = 3 });
            taps.Harvesting.Tap(HarvestFixture.ROCK, 0);
            Assert.That(taps.Chain.Value(taps.Chain.Active), Is.EqualTo(1));

            var finds = new Fixture(new Quest { Id = "FirstSteps", GoalType = GoalType.DiscoverFeature, GoalTarget = "Trees", GoalAmount = 4 });
            finds.Revealed.Fogged.Add(HarvestFixture.TREE);
            finds.Ground.Features[new Vector2Int(3, 0)] = "Trees";
            finds.Fog.Tap(new Vector2Int(3, 0));
            Assert.That(finds.Chain.Value(finds.Chain.Active), Is.EqualTo(1));
        }

        [Test]
        public void AnAutoClaimQuest_ShouldClaimItselfOnceDone()
        {
            var auto = Build();
            auto.AutoClaim = true;
            var fixture = new Fixture(auto, Build("Next"));

            Assert.That(fixture.Chain.ClaimIfAuto(0), Is.False);
            fixture.Goals.Built = 1;
            Assert.That(fixture.Chain.ClaimIfAuto(0), Is.True);
            Assert.That(fixture.Chain.Active.Id, Is.EqualTo("Next"));
        }

        [Test]
        public void ARentRush_ShouldTopUpTheFirstHouseAtItsMoment()
        {
            var tax = new Quest { Id = "TaxDay", GoalType = GoalType.CollectResource, GoalTarget = "Gold", GoalAmount = 15, TutorialRentSeconds = 3 };
            var fixture = new Fixture(Build(), tax);
            fixture.Goals.Built = 1;
            fixture.City.Districts.Add(new Codigames.Kingdom.City.State.DistrictState { Id = "house", DefinitionId = "Housing", Level = 1, Built = true });
            fixture.Chain.Claim(1000);

            fixture.Timeline.Advance(3999);
            Assert.That(fixture.Stores.Held(fixture.City.Districts.Find(d => d.Id == "house"), 3999), Is.EqualTo(0));
            fixture.Timeline.Advance(4000);
            Assert.That(fixture.Stores.Held(fixture.City.Districts.Find(d => d.Id == "house"), 4000), Is.EqualTo(15));
        }
    }
}
