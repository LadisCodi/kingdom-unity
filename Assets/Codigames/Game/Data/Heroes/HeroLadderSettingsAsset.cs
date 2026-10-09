using System.Collections.Generic;
using Codigames.Kingdom.Heroes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Heroes
{
    // The hero ladder (Docs/features/10-heroes.md §4) and the party's hero slots and wounds (§2.8, §3).
    [CreateAssetMenu(fileName = "HeroLadder", menuName = "Kingdom/Data/Hero Ladder")]
    public class HeroLadderSettingsAsset : DataSettings, IHeroLadderSettings
    {
        [BoxGroup("Stars"), SerializeField, MinValue(1)] private int _ascensionStars = 5;
        [BoxGroup("Stars"), SerializeField, MinValue(1)] private int _ascensionStepsPerStar = 6;
        [BoxGroup("Stars"), SerializeField] private double _fragmentsPerStepBase = 1;
        [BoxGroup("Stars"), SerializeField] private double _fragmentsPerStepGrowth = 2;
        [BoxGroup("Stars"), SerializeField] private double _ascensionStardustBase = 4;
        [BoxGroup("Stars"), SerializeField] private double _ascensionStardustGrowth = 2;
        [BoxGroup("Stars"), SerializeField, Tooltip("Every ascension point lifts the stats by this share.")] private double _statsPerAscension = 0.02;

        [BoxGroup("Recruit"), SerializeField, SuffixLabel("fragments")] private int _recruitCommon = 15;
        [BoxGroup("Recruit"), SerializeField, SuffixLabel("fragments")] private int _recruitRare = 25;
        [BoxGroup("Recruit"), SerializeField, SuffixLabel("fragments")] private int _recruitLegendary = 40;

        [BoxGroup("Levels"), SerializeField] private double _xpLevelCostBase = 20;
        [BoxGroup("Levels"), SerializeField] private double _xpLevelCostGrowth = 1.0165;
        [BoxGroup("Levels"), SerializeField] private int _heroLevelsPerStar;
        [BoxGroup("Levels"), SerializeField] private int _heroLevelsPerAscension = 10;
        [BoxGroup("Levels"), SerializeField] private int _heroMaxLevel = 310;

        [BoxGroup("Skill ranks"), SerializeField, Tooltip("The level each rank from II unlocks at.")] private List<int> _skillRankLevels = new();
        [BoxGroup("Skill ranks"), SerializeField] private List<double> _skillRankStardust = new();
        [BoxGroup("Skill ranks"), SerializeField] private List<double> _skillRankMaterial = new();
        [BoxGroup("Skill ranks"), SerializeField, Tooltip("What each rank adds, as a share of rank I's value.")] private double _skillRankStep = 0.25;

        [BoxGroup("Party"), SerializeField, MinValue(1)] private int _heroSlots = 3;
        [BoxGroup("Party"), SerializeField, SuffixLabel("Gems")] private double _heroSlotGemCostBase = 2500;
        [BoxGroup("Party"), SerializeField] private double _heroSlotGemCostGrowth = 2;
        [BoxGroup("Party"), SerializeField, SuffixLabel("h a whole bar")] private double _heroRecoverHours = 8;

        public int AscensionStars => _ascensionStars;
        public int AscensionStepsPerStar => _ascensionStepsPerStar;
        public int RecruitFragments(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Common => _recruitCommon,
            HeroRarity.Rare => _recruitRare,
            _ => _recruitLegendary,
        };
        public double FragmentsPerStepBase => _fragmentsPerStepBase;
        public double FragmentsPerStepGrowth => _fragmentsPerStepGrowth;
        public double AscensionStardustBase => _ascensionStardustBase;
        public double AscensionStardustGrowth => _ascensionStardustGrowth;
        public double XpLevelCostBase => _xpLevelCostBase;
        public double XpLevelCostGrowth => _xpLevelCostGrowth;
        public int HeroLevelsPerStar => _heroLevelsPerStar;
        public int HeroLevelsPerAscension => _heroLevelsPerAscension;
        public double StatsPerAscension => _statsPerAscension;
        public int HeroMaxLevel => _heroMaxLevel;
        public IReadOnlyList<int> SkillRankLevels => _skillRankLevels;
        public IReadOnlyList<double> SkillRankStardust => _skillRankStardust;
        public IReadOnlyList<double> SkillRankMaterial => _skillRankMaterial;
        public double SkillRankStep => _skillRankStep;
        public int HeroSlots => _heroSlots;
        public double HeroSlotGemCostBase => _heroSlotGemCostBase;
        public double HeroSlotGemCostGrowth => _heroSlotGemCostGrowth;
        public double HeroRecoverHours => _heroRecoverHours;
    }
}
