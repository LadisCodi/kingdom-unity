using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.Heroes;
using Codigames.Game.Heroes;
using Codigames.Game.UI.Bag;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Reveal;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;

namespace Codigames.Game.UI.Presenters
{
    // The reveal's words and cards (the web's gachaScreen prizeCard and mountGachaScreen): each prize as the card the
    // screen deals — its back's ink by rarity, a hero's art, a currency's or an item's picture, a fragments bar and its
    // readings — what the room says for each new hero, and the feast's music while it is open. Collect closes it.
    public class RevealScreenPresenter : AbstractDataMenuPresenter<RevealScreen, Kingdom.Heroes.Reveal>
    {
        private static readonly Color CLEAR = new(1, 1, 1, 0);

        private readonly UIManager _ui;
        private readonly HeroCollection _heroes;
        private readonly ItemCollection _items;
        private readonly ItemProse _prose;
        private readonly HeroWords _words;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly MusicDirector _music;
        private readonly Codigames.Game.Data.Relics.RelicCollection _relics;

        public RevealScreenPresenter(IMenuViewFactory views, UIManager ui, HeroCollection heroes, ItemCollection items, ItemProse prose, HeroWords words,
            UiIcons icons, NumberFormat numbers, Localizer localizer, MusicDirector music, Codigames.Game.Data.Relics.RelicCollection relics = null) : base(views)
        {
            _relics = relics;
            _ui = ui;
            _heroes = heroes;
            _items = items;
            _prose = prose;
            _words = words;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _music = music;
        }

        protected override void BindInternal(RevealScreen view)
        {
            view.Count = n => _numbers.Count(n);
            view.SetWords(_localizer.Tr("Tap to reveal"), _localizer.Tr("Tap to continue"), _localizer.Tr("Skip"), _localizer.Tr("Collect"),
                _localizer.Tr("Rewards"));
            _music.Set(MusicMoment.Feast, true);
        }

        // The sequence starts once the screen is up: a coroutine needs it active.
        protected override System.Threading.Tasks.Task PostShowInternal(RevealScreen view)
        {
            view.Play(Screen(Data));
            return System.Threading.Tasks.Task.CompletedTask;
        }

        protected override void UnbindInternal(RevealScreen view)
        {
            view.Stop();
            _music.Set(MusicMoment.Feast, false);
        }

        protected override void SubscribeToViewEventsInternal(RevealScreen view) => view.CollectTapped += OnCollect;

        protected override void UnsubscribeFromViewEventsInternal(RevealScreen view) => view.CollectTapped -= OnCollect;

        private void OnCollect() => _ = _ui.HideMenu<RevealScreen>();

        private RevealScreenData Screen(Kingdom.Heroes.Reveal reveal)
        {
            var cards = reveal.Prizes.Select(Card).ToList();
            var heroes = new List<RevealHeroLine>();
            for (var i = 0; i < reveal.Prizes.Count; i++)
            {
                if (!cards[i].NewHero) continue;
                var hero = _heroes.Get<HeroAsset>(reveal.Prizes[i].Id);
                heroes.Add(new RevealHeroLine
                {
                    Card = i,
                    Kicker = hero.Rarity == HeroRarity.Legendary ? _localizer.Tr("A legend answers") : _localizer.Tr("A new hero answers"),
                    Name = _localizer.Tr(hero.Name),
                    Title = string.IsNullOrEmpty(hero.Title) ? string.Empty : _localizer.Tr(hero.Title),
                    Rarity = _words.Rarity(hero.Rarity),
                    Tier = hero.Rarity,
                });
            }

            return new RevealScreenData
            {
                Chest = reveal.Chest,
                Caption = reveal.Caption ?? (reveal.Calls == 1
                    ? _localizer.Tr("One call")
                    : _localizer.Tr("{n} calls", ("n", _numbers.Count(reveal.Calls)))),
                Cards = cards,
                Heroes = heroes,
            };
        }

        private RevealCardData Card(Prize prize)
        {
            switch (prize.Kind)
            {
                case PrizeKind.Currency:
                    return new RevealCardData
                    {
                        Kind = prize.Kind, Icon = _icons.Get(prize.Id), Name = Currency(prize.Id), Count = Times(prize.Amount), Wash = CLEAR,
                    };
                case PrizeKind.Item:
                {
                    var item = _items.Get<ItemAsset>(prize.Id);
                    return new RevealCardData
                    {
                        Kind = prize.Kind, Icon = item.Icon, Name = _prose.Name(item), Count = Times(prize.Amount), Wash = CLEAR,
                    };
                }
                case PrizeKind.RelicFragment:
                {
                    var relic = _relics?.Get<Codigames.Game.Data.Relics.RelicAsset>(prize.Id);
                    var piece = relic != null ? relic.Fragment(prize.Slot) : null;
                    return new RevealCardData
                    {
                        Kind = prize.Kind, Icon = piece, Count = Times(prize.Amount), Wash = CLEAR,
                        Name = relic == null ? prize.Id : prize.Slot == Kingdom.Relics.Relics.KEYSTONE
                            ? _localizer.Tr("{relic} keystone", ("relic", _localizer.Tr(relic.Name)))
                            : _localizer.Tr(relic.Name),
                    };
                }
                case PrizeKind.Supplies:
                    return new RevealCardData
                    {
                        Kind = prize.Kind,
                        Name = prize.Family == SupplyFamily.Speedup ? _localizer.Tr("Speed-ups") : _localizer.Tr("Chests"),
                        Supplies = prize.Items.Select(i => (_items.Get<ItemAsset>(i.Item).Icon, Times(i.Amount))).ToList(),
                        Wash = CLEAR,
                    };
                case PrizeKind.Bag:
                {
                    var rarest = prize.Rows.Select(r => _heroes.Get(r.HeroId).Rarity).DefaultIfEmpty(HeroRarity.Common).Max();
                    return new RevealCardData
                    {
                        Kind = prize.Kind,
                        Name = _localizer.Tr("Fragments"),
                        Rarity = rarest,
                        Ink = Ink(rarest),
                        Glow = Glow(rarest),
                        Rows = prize.Rows.Select(Row).ToList(),
                        Wash = CLEAR,
                    };
                }
                case PrizeKind.Hero:
                {
                    var hero = _heroes.Get<HeroAsset>(prize.Id);
                    return new RevealCardData
                    {
                        Kind = prize.Kind, Art = hero.Art, Rarity = hero.Rarity, Name = Name(hero), NewHero = true, Charged = true, Ink = Ink(hero.Rarity),
                        Glow = Glow(hero.Rarity), Wash = CLEAR,
                    };
                }
                default:
                {
                    var hero = _heroes.Get<HeroAsset>(prize.Id);
                    var recruited = prize.Progress?.Recruited == true;
                    return new RevealCardData
                    {
                        Kind = prize.Kind,
                        Art = hero.Art,
                        Rarity = hero.Rarity,
                        Missing = prize.Progress?.TowardRecruit == true,
                        Wash = Wash(hero.Rarity),
                        Name = Name(hero),
                        Count = Times(prize.Amount),
                        CountIcon = hero.Fragment,
                        Bar = Bar(prize.Progress),
                        NewHero = recruited,
                        Charged = recruited,
                        Ink = Ink(hero.Rarity),
                        Glow = Glow(hero.Rarity),
                    };
                }
            }
        }

        private RevealRowData Row(BagRow row)
        {
            var hero = _heroes.Get<HeroAsset>(row.HeroId);
            return new RevealRowData
            {
                Art = hero.Art,
                Missing = row.Progress?.TowardRecruit == true,
                Wash = Wash(hero.Rarity),
                Name = Name(hero),
                Count = "+" + _numbers.Count(row.Amount),
                CountIcon = hero.Fragment,
                Bar = Bar(row.Progress),
            };
        }

        // The bar's two readings: "8 / 10", or what filling it did.
        private RevealBar Bar(FragmentProgress p)
        {
            if (p == null || p.Goal <= 0) return null;
            string Text(int n) => p.Recruited && n >= p.Goal
                ? _localizer.Tr("Recruited!")
                : _numbers.Count(p.TowardRecruit ? System.Math.Min(n, p.Goal) : n) + " / " + _numbers.Count(p.Goal);
            return new RevealBar
            {
                From = (float)p.From / p.Goal,
                To = (float)System.Math.Min(p.To, p.Goal) / p.Goal,
                FromText = Text(p.From),
                ToText = Text(p.To),
                Recruited = p.Recruited,
                Gold = p.TowardRecruit,
            };
        }

        // A hero's name on a card: "The" is dropped, as the web's cards drop it.
        private string Name(HeroAsset hero)
        {
            var name = _localizer.Tr(hero.Name);
            return name.StartsWith("The ") ? name.Substring(4) : name;
        }

        private string Times(int n) => "×" + _numbers.Count(n);

        private string Currency(string id) => id switch
        {
            "Gold" => _localizer.Tr("Gold"),
            "Food" => _localizer.Tr("Food"),
            "Wood" => _localizer.Tr("Wood"),
            "Stone" => _localizer.Tr("Stone"),
            "Mana" => _localizer.Tr("Mana"),
            "Knowledge" => _localizer.Tr("Knowledge"),
            "Stardust" => _localizer.Tr("Stardust"),
            "HeroXp" => _localizer.Tr("Hero xp"),
            _ => _localizer.Tr("Gems"),
        };

        private static int Ink(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Legendary => 3,
            HeroRarity.Rare => 2,
            _ => 1,
        };

        private static Color Glow(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Legendary => new Color32(242, 178, 51, 255),
            HeroRarity.Rare => new Color32(169, 139, 230, 255),
            _ => new Color32(127, 195, 230, 255),
        };

        private static Color Wash(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Legendary => new Color32(242, 178, 51, 102),
            HeroRarity.Rare => new Color32(138, 107, 196, 82),
            _ => new Color32(95, 143, 168, 82),
        };
    }
}
