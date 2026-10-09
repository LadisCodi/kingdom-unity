using Codigames.Game.Data.Heroes;
using Codigames.Game.UI.Kit;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Heroes
{
    // A hero as the card every screen shows it as (the web's heroCard): owned — its level or, chosen for a fight, its
    // power; resting — the time left; not found yet — its fragments against the recruit. And whether something can be
    // done with it now, the roster's orb.
    public class HeroCards
    {
        private readonly Kingdom.Heroes.Heroes _heroes;
        private readonly HeroCollection _catalog;
        private readonly ITreasury _treasury;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        public HeroCards(Kingdom.Heroes.Heroes heroes, HeroCollection catalog, ITreasury treasury, UiIcons icons, NumberFormat numbers,
            Localizer localizer)
        {
            _heroes = heroes;
            _catalog = catalog;
            _treasury = treasury;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
        }

        public HeroCardData Card(string id, double now, bool picked = false, bool cta = false, double? power = null)
        {
            var hero = _catalog.Get<HeroAsset>(id);
            var card = new HeroCardData
            {
                Id = id,
                Art = hero.Art,
                Rarity = hero.Rarity,
                UnitType = hero.UnitType,
                StepsPerStar = _heroes.Ladder.Settings.AscensionStepsPerStar,
                Picked = picked,
                Cta = cta,
            };
            if (!_heroes.Owns(id))
            {
                var have = _heroes.Fragments(id);
                var need = _heroes.RecruitCost(id);
                card.Missing = true;
                card.Pill = _numbers.Count(have) + " / " + _numbers.Count(need);
                card.PillIcon = hero.Fragment;
                card.PillReady = have >= need;
                return card;
            }

            card.Ascension = _heroes.Ascension(id);
            card.Rank = _heroes.SkillRank(id);
            var max = _heroes.MaxHp(id);
            var hp = _heroes.Hp(id, now);
            card.Hp = hp < max && max > 0 ? (float)hp / max : null;
            if (_heroes.Exhausted(id, now) && _heroes.RestEndsAt(id, now) is { } ends)
            {
                card.Resting = true;
                card.Pill = _numbers.Countdown(System.Math.Max(0, ends - now) / 1000);
            }
            else if (power.HasValue)
            {
                card.Pill = _numbers.Count(power.Value);
                card.PillIcon = _icons.Get("power");
            }
            else card.Pill = _localizer.Tr("Lv {level}", ("level", _numbers.Count(_heroes.Level(id))));

            return card;
        }

        // Something can be done with this hero now: a level, a star or a rank — or, not found yet, a recruit.
        public bool Ready(string id)
        {
            if (!_heroes.Owns(id)) return _heroes.CanRecruit(id);
            var canLevel = _heroes.Level(id) < _heroes.LevelCap(id) && _treasury.Get(Kingdom.Heroes.Heroes.HERO_XP) >= _heroes.LevelCost(id);
            var canAscend = _heroes.Ascension(id) < _heroes.Ladder.MaxAscension && _heroes.Fragments(id) >= _heroes.AscensionFragmentCost(id);
            return canLevel || canAscend || _heroes.SkillRankRefusal(id) == Kingdom.Heroes.SkillRankBlock.None;
        }
    }
}
