using System.Collections.Generic;

namespace Codigames.Kingdom.Heroes
{
    // The hero ladder (Docs/features/10-heroes.md §4) and the party's hero slots (§3).
    public interface IHeroLadderSettings
    {
        int AscensionStars { get; }
        int AscensionStepsPerStar { get; }
        // Fragments that recruit a hero outright, by rarity.
        int RecruitFragments(HeroRarity rarity);
        double FragmentsPerStepBase { get; }
        double FragmentsPerStepGrowth { get; }
        double AscensionStardustBase { get; }
        double AscensionStardustGrowth { get; }
        double XpLevelCostBase { get; }
        double XpLevelCostGrowth { get; }
        int HeroLevelsPerStar { get; }
        int HeroLevelsPerAscension { get; }
        double StatsPerAscension { get; }
        int HeroMaxLevel { get; }
        // A skill rank's unlock level, Stardust and material, from rank 2; and what each rank adds to rank 1's value.
        IReadOnlyList<int> SkillRankLevels { get; }
        IReadOnlyList<double> SkillRankStardust { get; }
        IReadOnlyList<double> SkillRankMaterial { get; }
        double SkillRankStep { get; }
        int HeroSlots { get; }
        double HeroSlotGemCostBase { get; }
        double HeroSlotGemCostGrowth { get; }
        // A whole bar of HP comes back in this many hours.
        double HeroRecoverHours { get; }
        // How many heroes of a rarity the player does not own a call can reach (Docs/features/10-heroes.md §6.6).
        int BagOpen(HeroRarity rarity);
        // The first calls across every banner that bring a hero not owned yet.
        int FirstCallsNewHero { get; }
    }
}
