using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Heroes;
using Codigames.Kingdom.Heroes.State;
using Codigames.Kingdom.Modifiers;
using Codigames.Kingdom.Tests.Battles;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Heroes
{
    // The hero ladder and the collection (Docs/features/10-heroes.md §2–§4), against the web's numbers.
    public class HeroesTests
    {
        private const double HOUR = 3_600_000;

        private sealed class Fixture : CityFixture
        {
            public Fixture() : base(new BuildingBuilder().WithId("Tavern").WithMaxLevel(5).WithHeroXpBonus(10, 20, 30, 40, 50).Build())
            {
                var catalog = BattleGoldenTests.Heroes().Items.Concat(new IHeroDefinition[]
                {
                    new BattleGoldenTests.FakeHero
                    {
                        Id = "Pharao", Rarity = HeroRarity.Legendary, UnitType = "Archer", Skill = "WarCry", SkillValue = 15, Atk = 4, Dmg = 60, Def = 3,
                        Hp = 900, Cooldown = 12, BoonStat = "buildSpeed", BoonValue = 1.2,
                    },
                    new BattleGoldenTests.FakeHero
                    {
                        Id = "Merchant", Rarity = HeroRarity.Common, UnitType = "Warrior", Skill = "Plunder", SkillValue = 15, Atk = 3, Dmg = 24, Def = 3,
                        Hp = 1152, Cooldown = 12,
                    },
                });
                Catalog = new Catalog<IHeroDefinition>(catalog);
                Heroes = new Codigames.Kingdom.Heroes.Heroes(State, Catalog, new HeroLadder(new BattleGoldenTests.Ladder()), Treasury, Stockpile, City,
                    Buildings, Bonuses);
                Stack = new ModifierStack(new IModifierSource[] { Heroes }, () => 0);
            }

            public HeroesState State { get; } = new();
            public Catalog<IHeroDefinition> Catalog { get; }
            public Codigames.Kingdom.Heroes.Heroes Heroes { get; }
            public ModifierStack Stack { get; }
        }

        [Test]
        public void TheLadder_ShouldPriceLevelsAndStarsAsTheWebDoes()
        {
            var ladder = new HeroLadder(new BattleGoldenTests.Ladder());

            Assert.That(ladder.XpLevelCost(1), Is.EqualTo(20));
            Assert.That(ladder.XpLevelCost(309), Is.EqualTo(3140));
            Assert.That(Enumerable.Range(1, 309).Sum(ladder.XpLevelCost), Is.EqualTo(192_333).Within(400));
            Assert.That(ladder.LevelCap(0), Is.EqualTo(10));
            Assert.That(ladder.LevelCap(6), Is.EqualTo(70));
            Assert.That(ladder.LevelCap(30), Is.EqualTo(310));
            Assert.That(Enumerable.Range(0, 30).Sum(a => ladder.AscensionFragmentCost(a)), Is.EqualTo(186));
            Assert.That(Enumerable.Range(0, 30).Sum(a => ladder.AscensionStardustCost(a)), Is.EqualTo(744));
        }

        [Test]
        public void ALevel_ShouldCostHeroXp_AndStopAtTheAscensionsCap()
        {
            var fixture = new Fixture();
            fixture.Heroes.Grant("Warden");
            fixture.Treasury.Add("HeroXp", 100_000);

            for (var i = 0; i < 9; i++) Assert.That(fixture.Heroes.LevelUp("Warden"), Is.EqualTo(HeroLevelResult.Levelled));

            Assert.That(fixture.Heroes.Level("Warden"), Is.EqualTo(10));
            Assert.That(fixture.Heroes.LevelUp("Warden"), Is.EqualTo(HeroLevelResult.AscensionCapped));
            Assert.That(fixture.Heroes.LevelUp("Rogue"), Is.EqualTo(HeroLevelResult.NotOwned));
        }

        [Test]
        public void Fragments_ShouldRecruitAHeroWithEveryStarEmpty_AndAscendIt()
        {
            var fixture = new Fixture();
            fixture.Heroes.AddFragments("Rogue", 17);
            fixture.Treasury.Add("Stardust", 10);

            Assert.That(fixture.Heroes.Recruit("Rogue"), Is.EqualTo(HeroRecruitResult.Recruited));
            Assert.That(fixture.Heroes.Fragments("Rogue"), Is.EqualTo(2));
            Assert.That(fixture.Heroes.Ascension("Rogue"), Is.EqualTo(0));

            Assert.That(fixture.Heroes.Ascend("Rogue"), Is.EqualTo(HeroAscendResult.Ascended));
            Assert.That(fixture.Heroes.Ascend("Rogue"), Is.EqualTo(HeroAscendResult.Ascended));
            Assert.That(fixture.Heroes.Ascend("Rogue"), Is.EqualTo(HeroAscendResult.NotEnoughFragments));
            Assert.That(fixture.Treasury.Get("Stardust"), Is.EqualTo(2));
            Assert.That(fixture.Heroes.LevelCap("Rogue"), Is.EqualTo(30));
        }

        [Test]
        public void ADuplicate_ShouldTurnIntoFragments()
        {
            var fixture = new Fixture();

            Assert.That(fixture.Heroes.Grant("Bard"), Is.True);
            Assert.That(fixture.Heroes.Grant("Bard", 5), Is.False);
            Assert.That(fixture.Heroes.Fragments("Bard"), Is.EqualTo(5));
        }

        [Test]
        public void ASkillRank_ShouldUnlockAtItsLevel_AndBeBoughtWithStardustAndItsMaterial()
        {
            var fixture = new Fixture();
            fixture.Heroes.Grant("Warden");
            fixture.Treasury.Add("Stardust", 1000);

            Assert.That(fixture.Heroes.SkillRankRefusal("Warden"), Is.EqualTo(SkillRankBlock.LevelTooLow));
            Assert.That(fixture.Heroes.NextSkillRankLevel("Warden"), Is.EqualTo(71));

            var state = fixture.State;
            state.Levels["Warden"] = 71;
            Assert.That(fixture.Heroes.SkillRankRefusal("Warden"), Is.EqualTo(SkillRankBlock.NotEnoughMaterial));
            // A Shield is the Moonglass family's.
            fixture.Stockpile.Add("Moonglass", 2);
            Assert.That(fixture.Heroes.SkillRankPrice("Warden").Value.Stardust, Is.EqualTo(100));
            Assert.That(fixture.Heroes.BuySkillRank("Warden"), Is.EqualTo(SkillRankBlock.None));
            Assert.That(fixture.Heroes.SkillRank("Warden"), Is.EqualTo(2));
            Assert.That(fixture.Treasury.Get("Stardust"), Is.EqualTo(900));
            Assert.That(fixture.Stockpile.Get("Moonglass"), Is.EqualTo(0));
            // Rank 2 is rank 1 and a quarter: the Warden's Shield, 15% → 18.75%, as per-mille.
            Assert.That(fixture.Heroes.Ladder.SlotSkill(fixture.Catalog.Get("Warden"), 2, 100).Amount, Is.EqualTo(188));
        }

        [Test]
        public void HeroSlots_ShouldBeOneFree_ThenGems_UpToThree()
        {
            var fixture = new Fixture();
            fixture.Treasury.Add("Gems", 10_000);

            Assert.That(fixture.Heroes.Slots, Is.EqualTo(1));
            Assert.That(fixture.Heroes.SlotGemCost, Is.EqualTo(2500));
            Assert.That(fixture.Heroes.BuySlot(), Is.EqualTo(HeroSlotResult.Purchased));
            Assert.That(fixture.Heroes.SlotGemCost, Is.EqualTo(5000));
            Assert.That(fixture.Heroes.BuySlot(), Is.EqualTo(HeroSlotResult.Purchased));
            Assert.That(fixture.Heroes.BuySlot(), Is.EqualTo(HeroSlotResult.AtMax));
            Assert.That(fixture.Heroes.Slots, Is.EqualTo(3));
        }

        [Test]
        public void HeroXp_ShouldBeRaisedByEveryTavernLevel()
        {
            var fixture = new Fixture();
            fixture.Stand("Tavern", new Vector2Int(3, 3), 2);

            Assert.That(fixture.Heroes.AddXp(500), Is.EqualTo(600));
            Assert.That(fixture.Treasury.Get("HeroXp"), Is.EqualTo(600));
        }

        [Test]
        public void AWound_ShouldMendOnItsOwn_AndAnExhaustedHeroRestsUntilWhole()
        {
            var fixture = new Fixture();
            fixture.Heroes.Grant("Warden");
            var max = fixture.Heroes.MaxHp("Warden");

            fixture.Heroes.SetHp("Warden", max / 2, 0);
            Assert.That(fixture.Heroes.Hp("Warden", 0), Is.EqualTo(max / 2).Within(1));
            Assert.That(fixture.Heroes.HpShare("Warden", 4 * HOUR), Is.EqualTo(1));

            fixture.Heroes.SetHp("Warden", 0, 0);
            Assert.That(fixture.Heroes.CanFight("Warden", 7 * HOUR), Is.False);
            Assert.That(fixture.Heroes.RestEndsAt("Warden", 0), Is.EqualTo(8 * HOUR));
            Assert.That(fixture.Heroes.CanFight("Warden", 8 * HOUR), Is.True);
        }

        [Test]
        public void ALegendary_ShouldBeABoonWhileOwned()
        {
            var fixture = new Fixture();
            Assert.That(fixture.Stack.Resolve("buildSpeed", 1), Is.EqualTo(1));

            fixture.Heroes.Grant("Pharao");

            Assert.That(fixture.Stack.Resolve("buildSpeed", 1), Is.EqualTo(1.2).Within(1e-9));
            Assert.That(fixture.Stack.Resolve("manaRegen", 10), Is.EqualTo(10));
        }
    }
}
