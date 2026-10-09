using System.Collections.Generic;

namespace Codigames.Kingdom.Relics
{
    // The relics' rules (relics.json): a city relic's level cycle and its windows; the fragments — how rare the
    // keystone is, what a level costs in Stardust, the store's pack, every how many treasures one turns up and what a
    // lair pays by tier; and the Shrine ladder — how many are built for materials and the Gems of each after.
    public interface IRelicSettings
    {
        IReadOnlyList<RelicAxis> Cycle { get; }
        IReadOnlyList<int> WindowMinutes { get; }
        int KeystoneOneIn { get; }
        double LevelStardustBase { get; }
        double LevelStardustGrowth { get; }
        int FragmentPackGems { get; }
        int FragmentPackSize { get; }
        int TreasureEvery { get; }
        IReadOnlyList<int> PerLairTier { get; }
        int ShrineMaterialBuilds { get; }
        IReadOnlyList<int> ShrinePremiumGems { get; }
    }
}
