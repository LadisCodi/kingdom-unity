using System;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Economy;

namespace Codigames.Kingdom.Heroes
{
    // A hero's body at a level and an ascension, its level cap, and what each step of the ladder costs
    // (Docs/features/10-heroes.md §4, the web's heroLadder.ts and skills.ts): pure arithmetic on the settings.
    public class HeroLadder
    {
        private readonly IHeroLadderSettings _settings;

        public HeroLadder(IHeroLadderSettings settings)
        {
            _settings = settings;
        }

        public IHeroLadderSettings Settings => _settings;

        public int MaxAscension => _settings.AscensionStars * _settings.AscensionStepsPerStar;

        public int FullStars(int ascension) => Math.Min(ascension, MaxAscension) / _settings.AscensionStepsPerStar;

        // Hero XP the next level costs from `level`.
        public double XpLevelCost(int level) => Prices.RoundPrice(_settings.XpLevelCostBase * Math.Pow(_settings.XpLevelCostGrowth, level));

        // Fragments the next ascension costs: the same for every point of a star, dearer each star.
        public double AscensionFragmentCost(int ascension)
            => Prices.RoundPrice(_settings.FragmentsPerStepBase * Math.Pow(_settings.FragmentsPerStepGrowth, FullStars(ascension)));

        public double AscensionStardustCost(int ascension)
            => Prices.RoundPrice(_settings.AscensionStardustBase * Math.Pow(_settings.AscensionStardustGrowth, FullStars(ascension)));

        // The highest level an ascension allows: ten a point, a star's worth more when it is full.
        public int LevelCap(int ascension)
        {
            var a = Math.Min(ascension, MaxAscension);
            return _settings.HeroMaxLevel - (MaxAscension - a) * _settings.HeroLevelsPerAscension
                   - (_settings.AscensionStars - FullStars(a)) * _settings.HeroLevelsPerStar;
        }

        // Its stat block at a level: each level adds its steps, each ascension point lifts the lot.
        public (double Atk, double Dmg, double Def, double Hp) Body(IHeroDefinition hero, int level, int ascension)
        {
            var step = level - 1;
            var mult = 1 + _settings.StatsPerAscension * Math.Min(ascension, MaxAscension);
            return ((hero.Atk + hero.AtkPerLevel * step) * mult, (hero.Dmg + hero.DmgPerLevel * step) * mult,
                (hero.Def + hero.DefPerLevel * step) * mult, (hero.Hp + hero.HpPerLevel * step) * mult);
        }

        public int MaxSkillRank => 1 + _settings.SkillRankLevels.Count;

        // A skill's value at a rank: each rank adds a step of rank 1's.
        public double RankValue(IHeroDefinition hero, int rank) => hero.SkillValue * (1 + _settings.SkillRankStep * (Math.Max(1, rank) - 1));

        // The skill resolved for the board: a share as per-mille, a Daze's delay and every timed skill's clock in ticks,
        // a Bulwark's flat as it is.
        public SlotSkill SlotSkill(IHeroDefinition hero, int rank, int tickMs)
        {
            var v = RankValue(hero, rank);
            int Ticks(double seconds) => Math.Max(1, Combat.JsRound(seconds * 1000 / tickMs));
            var amount = hero.Skill == "Bulwark" ? Combat.JsRound(v) : hero.Skill == "Daze" ? Ticks(v) : Combat.JsRound(v * 10);
            return new SlotSkill(hero.Skill, amount, Skills.IsTimed(hero.Skill) ? Ticks(hero.SkillEvery) : 0);
        }
    }
}
