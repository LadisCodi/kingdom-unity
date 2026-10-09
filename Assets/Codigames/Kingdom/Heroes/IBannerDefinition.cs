using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Heroes
{
    // What a loot row pays: a Fragment (the hero slot), Stardust or Hero XP (the hero-goods slot), an item (the
    // supplies slot).
    public enum LootReward
    {
        Fragments,
        Stardust,
        HeroXp,
        Item,
    }

    // One row of a banner's loot table: what it pays, of which rarity (Fragments) or item, how much, how often.
    public readonly struct BannerLoot
    {
        public BannerLoot(LootReward reward, HeroRarity? rarity, string item, int amount, double weight)
        {
            Reward = reward;
            Rarity = rarity;
            Item = item;
            Amount = amount;
            Weight = weight;
        }

        public LootReward Reward { get; }
        public HeroRarity? Rarity { get; }
        public string Item { get; }
        public int Amount { get; }
        public double Weight { get; }
    }

    // A banner (Docs/features/10-heroes.md §6): the key a call costs, the hero chance by heroes owned, the pity
    // counters, the rarity weights, what a duplicate pays, the free calls a day, the season hero it leans toward
    // and its loot table.
    public interface IBannerDefinition : IIdentifiable
    {
        string Key { get; }
        double KeyGemCost { get; }
        IReadOnlyList<double> HeroChanceByOwned { get; }
        int SoftPityAt { get; }
        int HardPityAt { get; }
        // 0: no Legendary guarantee.
        int LegendaryPityAt { get; }
        // A weight of 0 keeps a rarity off this banner.
        double Weight(HeroRarity rarity);
        int DuplicateFragments { get; }
        int FreePerDay { get; }
        double FreeCooldownSeconds { get; }
        // The card shows a hero rather than a chest.
        bool ShowsHero { get; }
        // A season hero this banner leans toward; null for none.
        string FeaturedHero { get; }
        double ExtraHeroSlotChance { get; }
        IReadOnlyList<BannerLoot> Loot { get; }
    }
}
