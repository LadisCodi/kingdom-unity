using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Heroes;
using Codigames.Kingdom.Heroes.State;
using Codigames.Kingdom.Tests.Battles;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Heroes
{
    // The banners' calls (Docs/features/10-heroes.md §6) against the web's: gacha-golden.txt is what the web's gacha
    // paid on a run of calls — single, batched and free — from a new kingdom on a fixed seed (Tools/WebData/
    // gacha-golden.ts). The same calls here must pay the same, call for call, and leave the same kingdom.
    public class GachaTests
    {
        private sealed class Items : IItemHoldings
        {
            public readonly SortedDictionary<string, int> Held = new(StringComparer.Ordinal);
            public void Grant(string itemId, int count) => Held[itemId] = Count(itemId) + count;
            public int Count(string itemId) => Held.TryGetValue(itemId, out var n) ? n : 0;

            public bool Take(string itemId, int count)
            {
                if (count < 1 || Count(itemId) < count) return false;
                Held[itemId] = Count(itemId) - count;
                return true;
            }
        }

        private sealed class FakeBanner : IBannerDefinition
        {
            public string Id { get; set; }
            public string Key { get; set; }
            public double KeyGemCost { get; set; }
            public IReadOnlyList<double> HeroChanceByOwned { get; set; }
            public int SoftPityAt { get; set; }
            public int HardPityAt { get; set; }
            public int LegendaryPityAt { get; set; }
            public double[] Weights { get; set; }
            public double Weight(HeroRarity rarity) => Weights[(int)rarity];
            public int DuplicateFragments { get; set; } = 10;
            public int FreePerDay { get; set; }
            public double FreeCooldownSeconds { get; set; }
            public bool ShowsHero { get; set; }
            public string FeaturedHero { get; set; }
            public double ExtraHeroSlotChance { get; set; } = 0.2;
            public IReadOnlyList<BannerLoot> Loot { get; set; }
        }

        private static readonly double[] CHANCE = { 0.3, 0.265, 0.23, 0.195, 0.16, 0.125, 0.09, 0.055, 0.02 };

        private static BannerLoot F(HeroRarity rarity, double weight) => new(LootReward.Fragments, rarity, "", 1, weight);
        private static BannerLoot C(LootReward reward, int amount, double weight) => new(reward, null, "", amount, weight);
        private static BannerLoot I(string item, double weight) => new(LootReward.Item, null, item, 1, weight);

        // The web's banners.json.
        private static IBannerDefinition[] Banners() => new IBannerDefinition[]
        {
            new FakeBanner
            {
                Id = "basic", Key = "SilverKey", KeyGemCost = 500, HeroChanceByOwned = CHANCE, SoftPityAt = 40, HardPityAt = 60, LegendaryPityAt = 0,
                Weights = new double[] { 55, 45, 0 }, FreePerDay = 5, FreeCooldownSeconds = 300,
                Loot = new[]
                {
                    F(HeroRarity.Common, 25), F(HeroRarity.Rare, 20.16), C(LootReward.Stardust, 10, 10), C(LootReward.Stardust, 25, 9),
                    C(LootReward.Stardust, 100, 2), C(LootReward.HeroXp, 50, 10.01), C(LootReward.HeroXp, 200, 10),
                    I("ConstructionSpeedup5m", 1.33), I("TrainingSpeedup5m", 1.33), I("WorkshopSpeedup5m", 1.33), I("FoodChest10m", 2),
                    I("WoodChest10m", 2), I("StoneChest10m", 2), I("GoldChest10m", 2),
                },
            },
            new FakeBanner
            {
                Id = "advanced", Key = "GoldKey", KeyGemCost = 1500, HeroChanceByOwned = CHANCE, SoftPityAt = 30, HardPityAt = 50, LegendaryPityAt = 40,
                Weights = new double[] { 0, 75, 25 }, FreePerDay = 1, FreeCooldownSeconds = 0, ShowsHero = true,
                Loot = new[]
                {
                    F(HeroRarity.Legendary, 15), F(HeroRarity.Rare, 30.02), C(LootReward.Stardust, 10, 3), C(LootReward.Stardust, 25, 8),
                    C(LootReward.Stardust, 100, 10), C(LootReward.HeroXp, 200, 10.03), C(LootReward.HeroXp, 500, 10),
                    I("ConstructionSpeedup1h", 1.33), I("TrainingSpeedup1h", 1.33), I("WorkshopSpeedup1h", 1.33), I("FoodChest1h", 2),
                    I("WoodChest1h", 2), I("StoneChest1h", 2), I("GoldChest1h", 2),
                },
            },
        };

        private sealed class Fixture : CityFixture
        {
            public Fixture(IEnumerable<IHeroDefinition> heroes, uint seed)
            {
                Heroes = new Codigames.Kingdom.Heroes.Heroes(State, new Catalog<IHeroDefinition>(heroes), new HeroLadder(new BattleGoldenTests.Ladder()),
                    Treasury, Stockpile, City, Buildings, Bonuses);
                Gacha = new Gacha(Calls, Heroes, new Catalog<IBannerDefinition>(Banners()), Items, Treasury, seed, Bonuses);
            }

            public HeroesState State { get; } = new();
            public GachaState Calls { get; } = new();
            public Items Items { get; } = new();
            public Codigames.Kingdom.Heroes.Heroes Heroes { get; }
            public Gacha Gacha { get; }
        }

        [Test]
        public void TheCalls_ShouldPayAsTheWebsDo()
        {
            var lines = File.ReadAllLines(Golden());
            var heroes = lines.Where(l => l.StartsWith("hero|")).Select(l => l.Split('|')).Select(p => new BattleGoldenTests.FakeHero
            {
                Id = p[1], Rarity = Enum.Parse<HeroRarity>(p[2]), BagRank = p[3] == "-" ? null : int.Parse(p[3]), UnitType = "Warrior",
            }).ToList();
            var seedLine = lines.Single(l => l.StartsWith("seed|")).Split('|');
            var t0 = double.Parse(seedLine[2]);
            var fixture = new Fixture(heroes, uint.Parse(seedLine[1]));
            fixture.Items.Grant("SilverKey", 400);
            fixture.Items.Grant("GoldKey", 400);
            var gacha = fixture.Gacha;

            var got = new List<string>();
            void Line(string tag, string banner, PullResult p) => got.Add(Describe(tag, banner, p));
            void Free(string banner, double seconds)
            {
                var tag = "free@" + seconds * 1000;
                var outcome = gacha.ClaimFreePull(banner, t0 + seconds * 1000, out var pull);
                got.Add(outcome == FreePullOutcome.Pulled ? Describe(tag, banner, pull) : $"{tag}|{banner}|{outcome}");
            }

            Line("pull", "basic", gacha.Pull("basic"));
            Line("pull", "advanced", gacha.Pull("advanced"));
            gacha.PullMany("basic", 10, out var many);
            foreach (var p in many) Line("many", "basic", p);
            foreach (var s in new double[] { 0, 100, 301, 602, 903, 1204, 1505 }) Free("basic", s);
            Free("advanced", 0);
            Free("advanced", 1);
            Free("advanced", 86_400);
            for (var i = 0; i < 70; i++) Line("pull", "advanced", gacha.Pull("advanced"));
            for (var i = 0; i < 120; i++) Line("pull", "basic", gacha.Pull("basic"));
            gacha.PullMany("advanced", 10, out many);
            foreach (var p in many) Line("many", "advanced", p);

            var calls = lines.Where(l => l.StartsWith("pull|") || l.StartsWith("many|") || l.StartsWith("free@")).ToList();
            Assert.That(got.Count, Is.EqualTo(calls.Count));
            for (var i = 0; i < calls.Count; i++) Assert.That(got[i], Is.EqualTo(calls[i]), $"call {i}");

            string Expect(string tag) => lines.Single(l => l.StartsWith(tag + "|")).Substring(tag.Length + 1);
            Assert.That(string.Join(",", fixture.Heroes.Owned), Is.EqualTo(Expect("owned")));
            Assert.That(string.Join(",", fixture.State.Fragments.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}:{kv.Value}")), Is.EqualTo(Expect("fragments")));
            Assert.That($"{fixture.Treasury.Get("Stardust")}|{fixture.Treasury.Get("HeroXp")}", Is.EqualTo(Expect("wallet")));
            Assert.That(string.Join(",", fixture.Items.Held.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}:{kv.Value}")), Is.EqualTo(Expect("bag")));
        }

        [Test]
        public void TheFirstStandardCall_ShouldBeFreeAndBringAHero()
        {
            var fixture = new Fixture(BattleGoldenTests.Heroes().Items, 7);

            Assert.That(fixture.Gacha.PullPrice("basic").Amount, Is.EqualTo(0));
            var pull = fixture.Gacha.Pull("basic");

            Assert.That(pull.Outcome, Is.EqualTo(PullOutcome.Pulled));
            Assert.That(pull.HeroId, Is.Not.Null);
            Assert.That(fixture.Gacha.PullPrice("basic").Amount, Is.EqualTo(1));
            Assert.That(fixture.Gacha.Pull("basic").Outcome, Is.EqualTo(PullOutcome.NotEnoughKeys));
        }

        [Test]
        public void ABatch_ShouldBeAllOrNothing()
        {
            var fixture = new Fixture(BattleGoldenTests.Heroes().Items, 7);
            fixture.Items.Grant("GoldKey", 9);

            Assert.That(fixture.Gacha.PullMany("advanced", 10, out var pulls), Is.EqualTo(PullOutcome.NotEnoughKeys));
            Assert.That(pulls, Is.Empty);
            Assert.That(fixture.Items.Count("GoldKey"), Is.EqualTo(9));
        }

        [Test]
        public void HardPity_ShouldBeAPromise()
        {
            var fixture = new Fixture(BattleGoldenTests.Heroes().Items, 7);

            Assert.That(fixture.Gacha.HeroChanceAt("basic", 59), Is.EqualTo(1));
            Assert.That(fixture.Gacha.HeroChanceAt("basic", 39), Is.EqualTo(0.3));
            Assert.That(fixture.Gacha.HeroChanceAt("basic", 50), Is.EqualTo(0.3 + 0.7 * 0.5).Within(1e-12));
            Assert.That(fixture.Gacha.PullsToLegendary("basic"), Is.Null);
            Assert.That(fixture.Gacha.PullsToLegendary("advanced"), Is.EqualTo(40));
        }

        [Test]
        public void AGuaranteedCall_ShouldMoveNoCounter()
        {
            var fixture = new Fixture(BattleGoldenTests.Heroes().Items, 7);
            var id = fixture.Heroes.All[0].Id;

            fixture.Gacha.CallGuaranteed("advanced", id);
            var again = fixture.Gacha.CallGuaranteed("advanced", id);

            Assert.That(again.Duplicate, Is.True);
            Assert.That(fixture.Heroes.Fragments(id), Is.EqualTo(10));
            Assert.That(fixture.Gacha.PullCount("advanced"), Is.EqualTo(0));
        }

        private static string Describe(string tag, string banner, PullResult p)
        {
            var loot = string.Join(";", p.Loot.Select(l => l.Kind switch
            {
                LootKind.Fragments => $"f:{l.Id}:{l.Amount}",
                LootKind.Currency => $"c:{l.Id}:{l.Amount}",
                _ => $"i:{l.Id}:{l.Amount}",
            }));
            static string B(bool b) => b ? "true" : "false";
            return $"{tag}|{banner}|{p.Outcome}|{p.HeroId ?? "-"}|{(p.Rarity?.ToString() ?? "-")}|{B(p.Duplicate)}|{p.Fragments}|{p.FragmentsOf ?? "-"}|"
                   + $"{B(p.Guaranteed)}|{B(p.GuaranteedLegendary)}|{loot}";
        }

        // Beside this file, found from wherever the runner stands.
        private static string Golden()
        {
            const string RELATIVE = "Assets/Codigames/Kingdom/Tests/Heroes/gacha-golden.txt";
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, RELATIVE))) return Path.Combine(dir.FullName, RELATIVE);
            for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, RELATIVE))) return Path.Combine(dir.FullName, RELATIVE);
            throw new FileNotFoundException(RELATIVE);
        }
    }
}
