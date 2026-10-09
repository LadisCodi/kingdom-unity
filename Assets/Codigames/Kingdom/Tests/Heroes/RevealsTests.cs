using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Heroes;
using Codigames.Kingdom.Heroes.State;
using Codigames.Kingdom.Tests.Battles;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Heroes
{
    // What a call hands the reveal (Docs/features/10-heroes.md §8.3), as the web's gachaPrizes, openReveal and
    // groupPrizes make it.
    public class RevealsTests
    {
        private sealed class Item : IItemDefinition
        {
            public string Id { get; set; }
            public ItemKind Kind { get; set; }
            public string Coin { get; set; }
            public double Seconds { get; set; }
            public int Tier { get; set; } = 1;
            public SpeedupKind? Speeds { get; set; }
            public BoostKind? Boost { get; set; }
            public double Value { get; set; }
        }

        private sealed class Fixture : CityFixture
        {
            public Fixture()
            {
                Heroes = new Codigames.Kingdom.Heroes.Heroes(State, BattleGoldenTests.Heroes(), new HeroLadder(new BattleGoldenTests.Ladder()), Treasury,
                    Stockpile, City, Buildings, Bonuses);
                Reveals = new Reveals(Heroes, new Catalog<IItemDefinition>(new IItemDefinition[]
                {
                    new Item { Id = "ConstructionSpeedup5m", Kind = ItemKind.Speedup },
                    new Item { Id = "TrainingSpeedup5m", Kind = ItemKind.Speedup },
                    new Item { Id = "WoodChest10m", Kind = ItemKind.Chest },
                }));
            }

            public HeroesState State { get; } = new();
            public Codigames.Kingdom.Heroes.Heroes Heroes { get; }
            public Reveals Reveals { get; }
        }

        private static PullResult Pull(string hero = null, bool duplicate = false, params CallLoot[] loot)
            => new()
            {
                Outcome = PullOutcome.Pulled, HeroId = hero, Duplicate = duplicate, Fragments = duplicate ? 10 : 0, FragmentsOf = duplicate ? hero : null,
                Loot = loot,
            };

        private static CallLoot Dust(int n) => new(LootKind.Currency, "Stardust", n);
        private static CallLoot Frag(string hero) => new(LootKind.Fragments, hero, 1);
        private static CallLoot Item_(string item) => new(LootKind.Item, item, 1);

        [Test]
        public void TheSameThing_ShouldBeOnePrizeWithACount_HeroesLast()
        {
            var prizes = Reveals.FromPulls(new[] { Pull("Warden", false, Dust(25)), Pull(null, false, Dust(25), Frag("Rogue")), Pull("Bard", true) });

            Assert.That(prizes.Select(p => p.Kind), Is.EqualTo(new[] { PrizeKind.Currency, PrizeKind.Fragments, PrizeKind.Fragments, PrizeKind.Hero }));
            Assert.That(prizes[0].Amount, Is.EqualTo(50));
            Assert.That(prizes[2].Id, Is.EqualTo("Bard"), "a duplicate pays its fragments, not a hero");
            Assert.That(prizes[2].Amount, Is.EqualTo(10));
            Assert.That(prizes[3].Id, Is.EqualTo("Warden"));
        }

        [Test]
        public void FragmentsThatReachThePrice_ShouldRecruitOnTheSpot()
        {
            var fixture = new Fixture();
            // The call has already paid the fragment that brings them to the price.
            fixture.Heroes.AddFragments("Rogue", 15);

            var reveal = fixture.Reveals.Open(Gacha.STANDARD, new[] { Pull(null, false, Frag("Rogue")) });

            var rogue = reveal.Prizes.Single(p => p.Kind == PrizeKind.Fragments);
            Assert.That(fixture.Heroes.Owns("Rogue"), Is.True);
            Assert.That(rogue.Progress.TowardRecruit, Is.True);
            Assert.That(rogue.Progress.Recruited, Is.True);
            Assert.That((rogue.Progress.From, rogue.Progress.To, rogue.Progress.Goal), Is.EqualTo((14, 15, 15)));
            Assert.That(reveal.Chest, Is.EqualTo(RevealChest.Common));
        }

        [Test]
        public void ATenCall_ShouldBeAHandfulOfCards()
        {
            var fixture = new Fixture();
            fixture.Heroes.Grant("Warden");
            var pulls = new List<PullResult>
            {
                Pull("Cleric", false, Dust(10), Item_("ConstructionSpeedup5m")),
                Pull(null, false, Frag("Warden"), Dust(25), Item_("WoodChest10m")),
                Pull(null, false, Frag("Rogue"), Item_("TrainingSpeedup5m")),
            };

            var reveal = fixture.Reveals.Open("advanced", pulls);

            Assert.That(reveal.Chest, Is.EqualTo(RevealChest.Golden));
            Assert.That(reveal.Calls, Is.EqualTo(3));
            Assert.That(reveal.Prizes.Select(p => p.Kind),
                Is.EqualTo(new[] { PrizeKind.Currency, PrizeKind.Supplies, PrizeKind.Supplies, PrizeKind.Bag, PrizeKind.Hero }));
            Assert.That(reveal.Prizes[0].Amount, Is.EqualTo(35));
            Assert.That(reveal.Prizes[1].Family, Is.EqualTo(SupplyFamily.Speedup));
            Assert.That(reveal.Prizes[1].Items.Count, Is.EqualTo(2));
            Assert.That(reveal.Prizes[3].Rows.Select(r => r.HeroId), Is.EquivalentTo(new[] { "Warden", "Rogue" }));
            Assert.That(reveal.Prizes[3].Rows.First(r => r.HeroId == "Warden").Progress.TowardRecruit, Is.False);
        }
    }
}
