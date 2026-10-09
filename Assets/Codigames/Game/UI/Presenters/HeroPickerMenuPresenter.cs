using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The hero picker's logic (the web's heroPickToggle, heroPickClearSlot, heroPickConfirm): the owned heroes by type,
    // by level or rarity; a tap on one seats it in the first free slot or takes it out of the one it holds; a tap on a
    // filled slot empties it; no free slot, or an exhausted hero, is refused. Select hands the slots back in order; the
    // close leaves without an answer. A popup: the screen it chooses for stays up under it, with its state.
    public class HeroPickerMenuPresenter : AbstractDataMenuPresenter<HeroPickerMenu, HeroPick>, IPopupMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly Kingdom.Heroes.Heroes _heroes;
        private readonly HeroCards _cards;
        private readonly ICombatSettings _combat;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;
        private readonly List<string> _types;

        private string[] _slots = Array.Empty<string>();
        private int _filter;
        private bool _byRarity;

        public HeroPickerMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Heroes.Heroes heroes, HeroCards cards, ICombatSettings combat,
            NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds, IQuickInfoMessageService messages) : base(views)
        {
            _ui = ui;
            _heroes = heroes;
            _cards = cards;
            _combat = combat;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
            _types = heroes.All.Select(h => h.UnitType).Distinct().ToList();
        }

        public void RequestClose() => _ = _ui.HideMenu<HeroPickerMenu>();

        protected override void BindInternal(HeroPickerMenu view)
        {
            _slots = Enumerable.Range(0, Math.Max(1, Data.Slots)).Select(i => i < Data.Selected.Count ? Data.Selected[i] : null).ToArray();
            _filter = 0;
            _byRarity = false;
            Refresh();
            view.ScrollToTop();
        }

        protected override void SubscribeToViewEventsInternal(HeroPickerMenu view)
        {
            view.CloseTapped += RequestClose;
            view.FilterTapped += OnFilter;
            view.SortTapped += OnSort;
            view.CardTapped += OnCard;
            view.SlotTapped += OnSlot;
            view.SelectTapped += OnSelect;
        }

        protected override void UnsubscribeFromViewEventsInternal(HeroPickerMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.FilterTapped -= OnFilter;
            view.SortTapped -= OnSort;
            view.CardTapped -= OnCard;
            view.SlotTapped -= OnSlot;
            view.SelectTapped -= OnSelect;
        }

        private void OnFilter(int index)
        {
            _filter = index;
            Refresh();
            View.ScrollToTop();
        }

        private void OnSort()
        {
            _byRarity = !_byRarity;
            Refresh();
        }

        // Out of its slot if it holds one; else into the first free slot — or refused, when none is free or it is
        // exhausted.
        private void OnCard(string id)
        {
            var at = Array.IndexOf(_slots, id);
            if (at >= 0) _slots[at] = null;
            else if (!_heroes.CanFight(id, _clock.NowMs))
            {
                _sounds.Play(SoundIds.ERROR);
                _messages.Show(new QuickInfoMessageData(_localizer.Tr("{hero} is exhausted — they rest until their HP is full",
                    ("hero", _localizer.Tr(_heroes.Get(id).Name)))));
                return;
            }
            else
            {
                var free = Array.IndexOf(_slots, null);
                if (free < 0)
                {
                    _sounds.Play(SoundIds.ERROR);
                    return;
                }

                _slots[free] = id;
            }

            Refresh();
        }

        private void OnSlot(int index)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null) return;
            _slots[index] = null;
            Refresh();
        }

        private async void OnSelect()
        {
            var chosen = _slots.Where(s => s != null).ToList();
            var select = Data.Select;
            await _ui.HideMenu<HeroPickerMenu>();
            select?.Invoke(chosen);
        }

        private void Refresh()
        {
            if (View == null || Data == null) return;
            var now = _clock.NowMs;
            var list = List().Select(id => _cards.Card(id, now, picked: _slots.Contains(id), power: Power(id))).ToList();
            var slots = _slots.Select(id => id == null
                ? new HeroSlotData { Kind = HeroSlotKind.Empty }
                : new HeroSlotData { Kind = HeroSlotKind.Hero, Card = _cards.Card(id, now, power: Power(id)) }).ToList();
            View.Show(Data.Title ?? _localizer.Tr("Choose heroes"), _filter, _byRarity ? _localizer.Tr("Rarity") : _localizer.Tr("Lv"), list,
                _localizer.Tr("No heroes of that type yet"), _localizer.Tr("Party"),
                _numbers.Count(_slots.Count(s => s != null)) + "/" + _numbers.Count(_slots.Length), slots, _localizer.Tr("Select"));
        }

        // A hero's power in its pill, when choosing for a fight.
        private double? Power(string id) => Data.Fight ? Combat.JsRound(_heroes.Power(id, _combat.HeroPowerPerDmg)) : null;

        // Every owned hero of the type, best first.
        private IEnumerable<string> List()
        {
            var type = _filter == 0 || _filter > _types.Count ? null : _types[_filter - 1];
            var owned = _heroes.Owned.Select(_heroes.Get).Where(h => type == null || h.UnitType == type);
            owned = _byRarity
                ? owned.OrderByDescending(h => h.Rarity).ThenByDescending(h => _heroes.Level(h.Id))
                : owned.OrderByDescending(h => _heroes.Level(h.Id)).ThenByDescending(h => h.Rarity);
            return owned.Select(h => h.Id);
        }
    }
}
