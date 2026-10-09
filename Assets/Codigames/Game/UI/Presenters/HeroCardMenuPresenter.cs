using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Heroes;
using Codigames.Game.Heroes;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Audio;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // One hero's card (the web's heroesSheet detail and its Game.do* handlers): its stats, its ascension, its skill and
    // the next rank, its boon, and its level — or its fragments and the recruit, not found yet. Each press is the rule's
    // own call; a refusal says why. Its close goes back to the roster.
    public class HeroCardMenuPresenter : AbstractDataMenuPresenter<HeroCardMenu, string>, IClosableMenuPresenter, IPurseMenu
    {
        private static readonly string[] PURSE = { Kingdom.Heroes.Heroes.HERO_XP, Kingdom.Heroes.Heroes.STARDUST };

        private readonly UIManager _ui;
        private readonly Kingdom.Heroes.Heroes _heroes;
        private readonly HeroCollection _catalog;
        private readonly HeroWords _words;
        private readonly ITreasury _treasury;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;

        public HeroCardMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Heroes.Heroes heroes, HeroCollection catalog, HeroWords words,
            ITreasury treasury, UiIcons icons, NumberFormat numbers, Localizer localizer, ISoundService sounds, IQuickInfoMessageService messages)
            : base(views)
        {
            _ui = ui;
            _heroes = heroes;
            _catalog = catalog;
            _words = words;
            _treasury = treasury;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _sounds = sounds;
            _messages = messages;
        }

        public IReadOnlyList<string> Purse => PURSE;

        public async void RequestClose()
        {
            await _ui.HideMenu<HeroCardMenu>();
            _ = _ui.ShowMenu<HeroesMenu>();
        }

        protected override void BindInternal(HeroCardMenu view)
        {
            Refresh();
            _heroes.Changed += Refresh;
            _treasury.Changed += OnTreasury;
        }

        protected override void UnbindInternal(HeroCardMenu view)
        {
            _heroes.Changed -= Refresh;
            _treasury.Changed -= OnTreasury;
        }

        protected override void SubscribeToViewEventsInternal(HeroCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.AscendTapped += OnAscend;
            view.SkillTapped += OnSkill;
            view.ReadTapped += OnRead;
        }

        protected override void UnsubscribeFromViewEventsInternal(HeroCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.AscendTapped -= OnAscend;
            view.SkillTapped -= OnSkill;
            view.ReadTapped -= OnRead;
        }

        private void OnTreasury(string currency, double amount) => Refresh();

        private void OnAscend()
        {
            var result = _heroes.Ascend(Data);
            if (result == HeroAscendResult.Ascended) _sounds.Play(SoundIds.UPGRADE_BOUGHT);
            else if (result == HeroAscendResult.NotEnoughFragments) Say("Not enough Fragments yet");
            else if (result == HeroAscendResult.NotEnoughStardust) Say("Not enough Stardust");
        }

        private void OnSkill()
        {
            var result = _heroes.BuySkillRank(Data);
            if (result == SkillRankBlock.None) _sounds.Play(SoundIds.UPGRADE_BOUGHT);
            else if (result == SkillRankBlock.NotEnoughStardust) Say("Not enough Stardust");
            else if (result == SkillRankBlock.NotEnoughMaterial) Say("Not enough precious material yet");
            else if (result == SkillRankBlock.LevelTooLow) Say("Reach the level first");
        }

        // The level's button, or — not found yet — the recruit.
        private void OnRead()
        {
            if (!_heroes.Owns(Data))
            {
                if (!_heroes.CanRecruit(Data)) _ = _ui.ShowMenu<StoreMenu, string>(StoreMenuPresenter.HEROES);
                else if (_heroes.Recruit(Data) == HeroRecruitResult.Recruited) _sounds.Play(SoundIds.CHAIN_FINISHED);
                return;
            }

            var result = _heroes.LevelUp(Data);
            if (result == HeroLevelResult.Levelled) _sounds.Play(SoundIds.UPGRADE_BOUGHT);
            else if (result == HeroLevelResult.NotEnoughXp) Say("Not enough Hero XP");
            else if (result == HeroLevelResult.AscensionCapped) Say("Their ascension holds them back");
        }

        private void Say(string english)
        {
            _sounds.Play(SoundIds.ERROR);
            _messages.Show(new QuickInfoMessageData(_localizer.Tr(english)));
        }

        private void Refresh()
        {
            if (View == null || string.IsNullOrEmpty(Data)) return;
            View.Show(Sheet(Data));
        }

        private HeroSheetData Sheet(string id)
        {
            var hero = _catalog.Get<HeroAsset>(id);
            var owned = _heroes.Owns(id);
            var ladder = _heroes.Ladder;
            var body = _heroes.Body(id);
            var sheet = new HeroSheetData
            {
                Name = _localizer.Tr(hero.Name),
                Rarity = hero.Rarity,
                RarityLabel = _words.Rarity(hero.Rarity),
                UnitType = hero.UnitType,
                TypeLabel = _words.UnitType(hero.UnitType),
                Art = hero.Art,
                Missing = !owned,
                Ascension = _heroes.Ascension(id),
                StepsPerStar = ladder.Settings.AscensionStepsPerStar,
                Stats = new[]
                {
                    new HeroStat("atk", _localizer.Tr("stat::Attack"), _numbers.Count(Kingdom.Battles.Combat.JsRound(body.Atk))),
                    new HeroStat("dmg", _localizer.Tr("Damage"), _numbers.Count(Kingdom.Battles.Combat.JsRound(body.Dmg))),
                    new HeroStat("def", _localizer.Tr("Defence"), _numbers.Count(Kingdom.Battles.Combat.JsRound(body.Def))),
                    new HeroStat("hp", _localizer.Tr("Health"), _numbers.Count(Kingdom.Battles.Combat.JsRound(body.Hp))),
                },
                SkillName = _words.SkillName(hero.Skill),
                SkillRank = owned ? _heroes.SkillRank(id) : 1,
                SkillTop = ladder.MaxSkillRank,
                Boon = _words.Boon(hero),
            };
            sheet.SkillSays = _words.Skill(hero, sheet.SkillRank);

            if (!owned)
            {
                var have = _heroes.Fragments(id);
                var need = _heroes.RecruitCost(id);
                sheet.Read = Reading(_localizer.Tr("Fragments"), have, need);
                sheet.ReadShare = need > 0 ? (float)have / need : 0;
                // Whichever of the two doors to them is open: Recruit once the price has piled up, the banner until then.
                sheet.ReadBuy = have >= need
                    ? new HeroBuy { Price = new[] { new PriceTerm("fragment", _numbers.Count(need), false) }, Label = _localizer.Tr("Recruit") }
                    : new HeroBuy { Label = "<sprite name=\"star\"> " + _localizer.Tr("Call for aid"), Material = ButtonMaterial.Gem };

                return sheet;
            }

            Ascension(id, sheet);
            Skill(id, hero, sheet);
            Level(id, sheet);
            return sheet;
        }

        // Both prices over the button that spends them: the Stardust toll and the fragments beside the hero.
        private void Ascension(string id, HeroSheetData sheet)
        {
            if (sheet.Ascension >= _heroes.Ladder.MaxAscension) return;
            var toll = _heroes.AscensionStardustCost(id);
            var need = _heroes.AscensionFragmentCost(id);
            var have = _heroes.Fragments(id);
            var shortDust = _treasury.Get(Kingdom.Heroes.Heroes.STARDUST) < toll;
            sheet.Ascend = new HeroBuy
            {
                Price = new[]
                {
                    new PriceTerm(Kingdom.Heroes.Heroes.STARDUST, _numbers.Count(toll), shortDust),
                    new PriceTerm("fragment", _numbers.Count(have) + " / " + _numbers.Count(need), have < need),
                },
                Label = _localizer.Tr("Ascend"),
                Enabled = have >= need && !shortDust,
            };
        }

        // What the next rank waits on — a level to reach, an ascension first — or its price and Upgrade.
        private void Skill(string id, HeroAsset hero, HeroSheetData sheet)
        {
            if (sheet.SkillRank >= sheet.SkillTop) return;
            var unlock = _heroes.NextSkillRankLevel(id);
            if (unlock is { } level && _heroes.Level(id) < level)
            {
                sheet.SkillNote = "<sprite name=\"padlock\"> " + (_heroes.LevelCap(id) < level
                    ? _localizer.Tr("Ascend, then reach level {level}", ("level", _numbers.Count(level)))
                    : _localizer.Tr("Reach level {level}", ("level", _numbers.Count(level))));
                return;
            }

            if (_heroes.SkillRankPrice(id) is not { } price) return;
            var block = _heroes.SkillRankRefusal(id);
            var terms = new List<PriceTerm>
            {
                new(Kingdom.Heroes.Heroes.STARDUST, _numbers.Count(price.Stardust), _treasury.Get(Kingdom.Heroes.Heroes.STARDUST) < price.Stardust),
            };
            terms.AddRange(price.Goods.Select(g => new PriceTerm(g.Key, _numbers.Count(g.Value), block == SkillRankBlock.NotEnoughMaterial)));
            sheet.SkillBuy = new HeroBuy
            {
                Price = terms,
                Label = _localizer.Tr("Upgrade"),
                Enabled = block is SkillRankBlock.None,
            };
        }

        // The level over its bar; at the ascension's ceiling, the stars to reach instead of a button.
        private void Level(string id, HeroSheetData sheet)
        {
            var level = _heroes.Level(id);
            var cap = _heroes.LevelCap(id);
            sheet.Read = Reading(_localizer.Tr("Level"), level, cap);
            sheet.ReadShare = cap > 0 ? (float)level / cap : 0;
            if (level >= _heroes.Ladder.Settings.HeroMaxLevel)
            {
                sheet.ReadNote = _localizer.Tr("At the ceiling");
                return;
            }

            if (level >= cap)
            {
                sheet.ReadNote = _localizer.Tr("Ascend to");
                sheet.CapStars = sheet.Ascension + 1;
                return;
            }

            var cost = _heroes.LevelCost(id);
            var shortXp = _treasury.Get(Kingdom.Heroes.Heroes.HERO_XP) < cost;
            sheet.ReadBuy = new HeroBuy
            {
                Price = new[] { new PriceTerm(Kingdom.Heroes.Heroes.HERO_XP, _numbers.Count(cost), shortXp) },
                Label = _localizer.Tr("Level Up"),
                Enabled = !shortXp,
            };
        }

        // "Level 7 of 30": the number bold and larger, wherever the language puts it.
        private string Reading(string label, int have, int of)
        {
            var line = _localizer.Tr("{label} {have} of {of}", ("label", label), ("have", "\u0001"), ("of", _numbers.Count(of)));
            return line.Replace("\u0001", "<size=140%><b><color=#3B2412>" + _numbers.Count(have) + "</color></b></size>");
        }
    }
}
