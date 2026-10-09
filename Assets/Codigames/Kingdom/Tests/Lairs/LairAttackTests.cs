using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Army.State;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Lairs;
using Codigames.Kingdom.Lairs.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Tests.Battles;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Lairs
{
    // A lair's path of fights and its claim (Docs/features/18-garrisons-and-raids.md §5): Mana on the way in, the fight
    // resolved on entry, who fell read off it, a step on the path a win, and the claim paying for the lair.
    public class LairAttackTests
    {
        private const double T0 = 86_400_000 + 12 * 3_600_000;

        private sealed class Settings : IEconomySettings, IWorkerSettings, ILairSettings, IArmySettings, IRushSettings
        {
            public double GoldPerPopulationPerMinute => 30;
            public double CollectSeconds => 30;
            public double MoveSpeedTilesPerSecond => 1;
            public IReadOnlyList<Garrison> Garrisons { get; } = new[]
            {
                new Garrison(3, 300, 500, new Dictionary<string, int> { ["ConstructionSpeedup15m"] = 1, ["WatchtowerLens"] = 1 }),
            };
            public double TakeFractionMax => 0.5;
            public int RaidsPerDay => 3;
            public double WindowStartHour => 9;
            public double WindowEndHour => 23;
            public double FirstFightPower => 0.5;
            public double FirstClearKnowledge => 3;
            public double WoundedShare => 0.1;
            public double HealCostShare => 0.3;
            public double HealTimeShare => 0.25;
            public double SecondsPerGem => 5;
        }

        private sealed class Orcs : ILairSite
        {
            public string Id => "Orcs";
            public string Name => "Orc Lair";
            public string Description => "";
            public string Flavour => "";
            public Vector2Int Anchor => new(-4, -4);
            public int Size => 2;
            public int Tier => 1;
            public int Radius => 1;
            public int Sight => 3;
            public string Threat => "Warrior";
            public int Power => 60;
            public double WarningMinutes => 30;
            public IReadOnlyDictionary<string, double> Mix { get; } = new Dictionary<string, double> { ["Warrior"] = 4, ["Lancer"] = 1 };
            public string RollKey => "HollowBarrow";
        }

        private sealed class Sites : IProvinceSites
        {
            public IReadOnlyList<IAbandonedSite> Abandoned { get; } = new IAbandonedSite[0];
            public IReadOnlyList<ILandmarkSite> Landmarks { get; } = new ILandmarkSite[0];
            public IReadOnlyList<ILairSite> Lairs { get; } = new ILairSite[] { new Orcs() };
        }

        private sealed class Grants : IItemGrants
        {
            public Dictionary<string, int> Granted { get; } = new();
            public void Grant(string itemId, int count) => Granted[itemId] = (Granted.TryGetValue(itemId, out var n) ? n : 0) + count;
        }

        // The orcs found, thirty Warriors at home and an Infirmary of 30 beds; the kingdom's seed is the web test's 42.
        private sealed class Fixture : HarvestFixture
        {
            public Fixture()
            {
                var settings = new Settings();
                Units = BattleGoldenTests.Units();
                Stores = new Stores(City, Buildings, settings, Treasury, Construction);
                Held = new LairGround(new Sites(), State, Revealed);
                var crews = new Workforce(City, Buildings, Revealed, Harvesting, Stores, settings, null, Held);
                Lairs = new Codigames.Kingdom.Lairs.Lairs(State, Held, settings, City, Stores, crews, 42, Bonuses);
                Army = new Codigames.Kingdom.Army.Army(ArmyState, City, Buildings, Units, Treasury, settings, settings, Gates, Bonuses);
                Combat = new Combat(Units, new BattleGoldenTests.Settings());
                Attack = new LairAttack(Lairs, Combat, new EnemyGenerator(Combat), Army, Treasury, 42, 6, Items, Bonuses);
                Stand("Infirmary", new Vector2Int(-3, 3));
                ArmyState.Troops["Warrior"] = 30;
                Lairs.ApplyDue(T0);
            }

            public Catalog<IUnitDefinition> Units { get; }
            public LairsState State { get; } = new();
            public ArmyState ArmyState { get; } = new();
            public Stores Stores { get; }
            public LairGround Held { get; }
            public Codigames.Kingdom.Lairs.Lairs Lairs { get; }
            public Codigames.Kingdom.Army.Army Army { get; }
            public Combat Combat { get; }
            public LairAttack Attack { get; }
            public Grants Items { get; } = new();
            public ILairSite Orc => Lairs.Site("Orcs");

            protected override IBuildingDefinition[] ExtraBuildings() => new[]
            {
                new BuildingBuilder().WithId("Infirmary").WithMaxLevel(1).WithSize(2, 2).WithBeds(30).Build(),
            };
        }

        private static IReadOnlyList<SquadSpec> Party(int warriors) => new[] { new SquadSpec("Warrior", warriors) };

        [Test]
        public void TheFirstFight_ShouldBeTheGarrisonTheWebRolls_AtHalfItsPower()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Lairs.FightPower(fixture.Orc, 0), Is.EqualTo(30));
            Assert.That(fixture.Attack.Formation(fixture.Orc).Select(s => $"{s.Troop}x{s.Count}"), Is.EqualTo(new[] { "Warriorx8", "Lancerx1" }));
            Assert.That(fixture.Attack.Board(fixture.Orc, 2).Slots.Select(s => $"{s.Troop}x{s.Count}"), Is.EqualTo(new[] { "Warriorx16", "Lancerx3" }));
        }

        [Test]
        public void TheChainsCompany_ShouldWalkThePath_PayingItsManaAndLosingItsFallen()
        {
            var fixture = new Fixture();
            var mana = fixture.Treasury.Get("Mana");

            var first = fixture.Attack.Attack("Orcs", Party(30), T0);
            Assert.That(first.Result, Is.EqualTo(LairResult.Won));
            Assert.That(first.HeroXp, Is.EqualTo(167));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(mana - 20));
            var home = fixture.Army.Count("Warrior");
            Assert.That(home + first.Losses.Values.Sum(), Is.EqualTo(30));
            Assert.That(fixture.Army.Wounded("Warrior"), Is.EqualTo(first.Wounded));

            var second = fixture.Attack.Attack("Orcs", Party(home), T0);
            Assert.That(second.Result, Is.EqualTo(LairResult.Won));
            var last = fixture.Attack.Attack("Orcs", Party(fixture.Army.Count("Warrior")), T0);

            Assert.That(last.Result, Is.EqualTo(LairResult.Cleared));
            Assert.That(last.Knowledge, Is.EqualTo(3));
            Assert.That(fixture.Lairs.AwaitsClaim("Orcs"), Is.True);
            Assert.That(fixture.Treasury.Get("HeroXp"), Is.EqualTo(334));
        }

        [Test]
        public void ARepulse_ShouldCostTheMana_AndTheFallen_AndLeaveTheLairStanding()
        {
            var fixture = new Fixture();

            var report = fixture.Attack.Attack("Orcs", Party(1), T0);

            Assert.That(report.Result, Is.EqualTo(LairResult.Repelled));
            Assert.That(report.Losses["Warrior"], Is.EqualTo(1));
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(29));
            Assert.That(fixture.Lairs.Won(fixture.Orc), Is.EqualTo(0));
            Assert.That(fixture.Treasury.Get("Mana"), Is.EqualTo(80));
        }

        [Test]
        public void AnAttack_ShouldBeRefused_BeforeAnythingIsSpent()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Attack.Block("Orcs", new SquadSpec[0]), Is.EqualTo(LairBlock.EmptyParty));
            Assert.That(fixture.Attack.Block("Orcs", Party(31)), Is.EqualTo(LairBlock.NotEnoughUnits));
            Assert.That(fixture.Attack.Block("Orcs", Enumerable.Range(0, 7).Select(_ => new SquadSpec("Warrior", 1)).ToList()), Is.EqualTo(LairBlock.TooManySlots));
            Assert.That(fixture.Attack.Block("Nowhere", Party(5)), Is.EqualTo(LairBlock.LairNotFound));
            fixture.Treasury.TryPay(new Dictionary<string, double> { ["Mana"] = fixture.Treasury.Get("Mana") });
            var report = fixture.Attack.Attack("Orcs", Party(30), T0);
            Assert.That(report.Block, Is.EqualTo(LairBlock.NotEnoughSupplies));
            Assert.That(fixture.Army.Count("Warrior"), Is.EqualTo(30));
        }

        [Test]
        public void TheClaim_ShouldPayTheHoard_TheLastFightsXp_TheKnowledge_AndTheItems()
        {
            var fixture = new Fixture();
            fixture.State.Lairs["Orcs"].Hoard["Gold"] = 500;
            fixture.ArmyState.Troops["Warrior"] = 300;
            for (var i = 0; i < 3; i++) fixture.Attack.Attack("Orcs", Party(100), T0);
            var gold = fixture.Treasury.Get("Gold");
            var xp = fixture.Treasury.Get("HeroXp");

            var claim = fixture.Attack.Claim("Orcs");

            Assert.That(claim.Hoard["Gold"], Is.EqualTo(500));
            Assert.That(fixture.Treasury.Get("Gold"), Is.EqualTo(gold + 500));
            Assert.That(fixture.Treasury.Get("HeroXp"), Is.EqualTo(xp + 167));
            Assert.That(fixture.Treasury.Get("Knowledge"), Is.EqualTo(3));
            Assert.That(fixture.Items.Granted, Is.EquivalentTo(new Dictionary<string, int> { ["ConstructionSpeedup15m"] = 1, ["WatchtowerLens"] = 1 }));
            Assert.That(fixture.Attack.Claim("Orcs"), Is.Null);
            Assert.That(fixture.Attack.Block("Orcs", Party(5)), Is.EqualTo(LairBlock.AlreadyCleared));
        }
    }
}
