using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Lairs.State;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Tutorial
{
    public class CampaignConditionsTests
    {
        private sealed class Item : IItemDefinition
        {
            public string Id { get; set; }
            public ItemKind Kind { get; set; }
            public string Coin => null;
            public double Seconds => 0;
            public int Tier => 1;
            public SpeedupKind? Speeds => null;
            public BoostKind? Boost => null;
            public double Value => 0;
        }

        private static (KingdomState State, CampaignConditions Conditions) Make()
        {
            var state = new KingdomState();
            var items = new Catalog<IItemDefinition>(new IItemDefinition[]
            {
                new Item { Id = "WoodChest1h", Kind = ItemKind.Chest },
                new Item { Id = "ConstructionSpeedup15m", Kind = ItemKind.Speedup },
            });
            return (state, new CampaignConditions(state, items));
        }

        private static bool Holds(CampaignConditions conditions, ConditionKind kind, string target = "", double amount = 0)
        {
            Assert.That(conditions.TryHolds(new Condition(kind, target, amount), 0, out var holds), Is.True);
            return holds;
        }

        [Test]
        public void TheLairs_ShouldBeFoundBeatenAndClearedInTurn()
        {
            var (state, conditions) = Make();
            Assert.That(Holds(conditions, ConditionKind.LairFound), Is.False);

            state.Lairs.Lairs["Orcs"] = new LairState();
            Assert.That(Holds(conditions, ConditionKind.LairFound, "Orcs"), Is.True);
            Assert.That(Holds(conditions, ConditionKind.LairFound, "Harpies"), Is.False);
            Assert.That(Holds(conditions, ConditionKind.LairDefeated, "Orcs"), Is.False);

            state.Lairs.Lairs["Orcs"].Defeated = true;
            Assert.That(Holds(conditions, ConditionKind.LairDefeated, "Orcs"), Is.True);
            Assert.That(Holds(conditions, ConditionKind.LairCleared), Is.False);

            state.Lairs.Lairs["Orcs"].Cleared = true;
            Assert.That(Holds(conditions, ConditionKind.LairCleared), Is.True);
        }

        [Test]
        public void TheArmy_ShouldCountStandingOrInTraining()
        {
            var (state, conditions) = Make();
            Assert.That(Holds(conditions, ConditionKind.Troops), Is.False);

            state.Army.Lines.Add(new Kingdom.Army.State.HallItem { Kind = Kingdom.Army.State.HallItemKind.Recruit, Troop = "Warrior" });
            Assert.That(Holds(conditions, ConditionKind.Troops), Is.True);
            Assert.That(Holds(conditions, ConditionKind.Wounded), Is.False);

            state.Army.Wounded["Warrior"] = 2;
            Assert.That(Holds(conditions, ConditionKind.Wounded), Is.True);
        }

        [Test]
        public void TheBag_ShouldAnswerByItemOrByKind_AndAnItemUsedOnceNoneIsLeft()
        {
            var (state, conditions) = Make();
            state.Bag.Held["WoodChest1h"] = 1;
            state.Bag.Held["ConstructionSpeedup15m"] = 2;

            Assert.That(Holds(conditions, ConditionKind.HoldsItem, "WoodChest1h"), Is.True);
            Assert.That(Holds(conditions, ConditionKind.HoldsItem, "speedup", 2), Is.True);
            Assert.That(Holds(conditions, ConditionKind.ItemUsed, "WoodChest1h"), Is.False);

            state.Bag.Held["WoodChest1h"] = 0;
            Assert.That(Holds(conditions, ConditionKind.ItemUsed, "WoodChest1h"), Is.True);
        }

        [Test]
        public void HeroesAndRelics_ShouldBeCounted()
        {
            var (state, conditions) = Make();
            Assert.That(Holds(conditions, ConditionKind.Heroes), Is.False);

            state.Heroes.Owned.Add("Warden");
            state.Relics.Hosts["district-9"] = "DowsingRod";
            Assert.That(Holds(conditions, ConditionKind.Heroes), Is.True);
            Assert.That(Holds(conditions, ConditionKind.RelicHosted, "DowsingRod"), Is.True);
            Assert.That(Holds(conditions, ConditionKind.RelicHosted, "GildedLedger"), Is.False);
        }
    }
}
