using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.Relics;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Relics;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // A relic's sheet's logic (the web's renderRelicSheet and Game.relicCard): the press for its state — Restore in
    // fragments, then Level up, which takes a whole set and its Stardust; its Shrine and the Shrines it could go to;
    // and its activation, redrawn every second while its window runs.
    public class RelicSheetMenuPresenter : AbstractDataMenuPresenter<RelicSheetMenu, string>, IClosableMenuPresenter, ITickable
    {
        private const string STARDUST = "Stardust";

        private readonly UIManager _ui;
        private readonly Kingdom.Relics.Relics _relics;
        private readonly Shrines _shrines;
        private readonly RelicCards _cards;
        private readonly RelicWords _words;
        private readonly RelicActions _actions;
        private readonly ManaPool _mana;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly PriceTerms _prices;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly AsleepRelics _asleep;

        private double _shownSecond = -1;

        public RelicSheetMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Relics.Relics relics, Shrines shrines, RelicCards cards,
            RelicWords words, RelicActions actions, ManaPool mana, ICatalog<IBuildingDefinition> buildings, PriceTerms prices, UiIcons icons,
            NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds, AsleepRelics asleep) : base(views)
        {
            _asleep = asleep;
            _ui = ui;
            _relics = relics;
            _shrines = shrines;
            _cards = cards;
            _words = words;
            _actions = actions;
            _mana = mana;
            _buildings = buildings;
            _prices = prices;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<RelicSheetMenu>();

        // The window and the Mana count: redrawn once a second.
        public void Tick()
        {
            if (!IsShown || Data == null) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _shownSecond) return;
            _shownSecond = second;
            Refresh();
        }

        protected override void BindInternal(RelicSheetMenu view)
        {
            _asleep.Seen(Data);
            Refresh();
            view.ScrollToTop();
            _relics.Changed += Refresh;
            _shrines.Changed += Refresh;
        }

        protected override void UnbindInternal(RelicSheetMenu view)
        {
            _relics.Changed -= Refresh;
            _shrines.Changed -= Refresh;
        }

        protected override void SubscribeToViewEventsInternal(RelicSheetMenu view)
        {
            view.CloseTapped += RequestClose;
            view.PressTapped += OnPress;
            view.HostTapped += OnHost;
            view.RemoveTapped += OnRemove;
            view.ActivateTapped += OnActivate;
            view.FlaskTapped += OnFlask;
            view.StoreTapped += OnStore;
        }

        protected override void UnsubscribeFromViewEventsInternal(RelicSheetMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.PressTapped -= OnPress;
            view.HostTapped -= OnHost;
            view.RemoveTapped -= OnRemove;
            view.ActivateTapped -= OnActivate;
            view.FlaskTapped -= OnFlask;
            view.StoreTapped -= OnStore;
        }

        private void OnPress()
        {
            if (_relics.IsRestored(Data)) _actions.LevelUp(Data);
            else _actions.Restore(Data);
            Refresh();
        }

        private void OnHost(string shrineId) => _actions.Host(Data, shrineId);
        private void OnRemove() => _actions.Unhost(Data);

        private void OnActivate()
        {
            _actions.Activate(Data);
            Refresh();
        }

        private void OnFlask()
        {
            _actions.UseFlask();
            Refresh();
        }

        // More fragments are the store's.
        private void OnStore()
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _ = _ui.ShowMenu<StoreMenu, string>("supplies");
        }

        private void Refresh()
        {
            if (View == null || Data == null) return;
            var id = Data;
            var relic = _cards.Get(id);
            var now = _clock.NowMs;
            var level = _relics.Level(id);
            var restored = level >= 1;
            var status = _cards.Status(id);
            var hasSet = _relics.DistinctHeld(id) == Kingdom.Relics.Relics.SLOTS;
            var city = relic.Kind == RelicKind.City;
            var setNote = _localizer.Tr("A piece in every slot to level up");

            var sheet = new RelicSheetData
            {
                Title = _localizer.Tr(relic.Name),
                Art = relic.Icon,
                Status = status,
                AwakeRibbon = _localizer.Tr("Awake").ToUpperInvariant(),
                Story = _words.Story(relic, Math.Max(1, level)),
                Stats = _words.Changes(relic, Math.Max(1, level)).Select(c => new StatTileData(_icons.Get(c.Now.Icon), c.Now.Label,
                    restored && c.Changed ? c.Now.Value + " → " + c.To : c.Now.Value)).ToList(),
                Pending = relic.PendingText == null ? null : _localizer.Tr(relic.PendingText),
                LevelHead = restored ? _localizer.Tr("Level {level}", ("level", _numbers.Exact(level))) : _localizer.Tr("Not restored"),
                Slots = _cards.Slots(id),
                SetNote = restored && !hasSet ? setNote : null,
                Press = restored ? RelicPress.LevelUp : _relics.CanRestore(id) ? RelicPress.Restore : RelicPress.None,
                PressLabel = restored ? _localizer.Tr("Level up") : _localizer.Tr("Restore"),
                PressPrice = restored
                    ? _prices.Of(new Dictionary<string, double> { [STARDUST] = _relics.LevelStardust(level) })
                    : Array.Empty<PriceTerm>(),
                // One green action a screen: while an asleep relic's Activate is on the page, Level up steps down to wood.
                PressGreen = status != RelicStatus.Asleep,
                PressBlocked = restored && !hasSet,
                Sources = !hasSet && !_relics.CanRestore(id)
                    ? _localizer.Tr("More fragments are won in battle — in lairs, on the world map and in the depths — bought in the store, or traded with friends")
                    : null,
                HostChoices = Array.Empty<RelicHostChoice>(),
                StoreLabel = _localizer.Tr("Store"),
            };

            if (restored && city) Host(sheet, id);
            if (restored && city && _shrines.HostOf(id) != null) Activation(sheet, id, now);
            View.Show(sheet);
        }

        // Where a restored city relic is hosted, and the Shrines it could go to.
        private void Host(RelicSheetData sheet, string id)
        {
            var host = _shrines.HostOf(id);
            var others = _shrines.All.Where(d => d != host).ToList();
            sheet.ShrineHead = _localizer.Tr("Shrine");
            sheet.HostMuted = host == null;
            sheet.HostLine = host != null ? _localizer.Tr("Hosted in {place}", ("place", Label(host)))
                : others.Count == 0 ? _localizer.Tr("Repair the Shrine in the ruins to host it")
                : _localizer.Tr("Not hosted — host it in a Shrine, then activate it");
            sheet.HostLabel = _localizer.Tr("Host");
            sheet.HostChoices = others.Select(d => new RelicHostChoice
            {
                ShrineId = d.Id,
                Note = _shrines.Hosted(d) is { } held
                    ? _localizer.Tr("{shrine} · holds {relic}", ("shrine", Label(d)), ("relic", _localizer.Tr(_cards.Get(held).Name)))
                    : Label(d),
            }).ToList();
            sheet.CanRemove = host != null;
            sheet.RemoveLabel = _localizer.Tr("Remove");
        }

        // Asleep: the wax, Activate and what a window buys; short of Mana, how soon the pool holds it and a flask.
        // Awake: the window running down.
        private void Activation(RelicSheetData sheet, string id, double now)
        {
            var level = _relics.Level(id);
            var window = _relics.WindowMs(id, level);
            var reach = _localizer.Tr("{n} cells round its Shrine", ("n", _numbers.Exact(Square(_shrines.Radius(id)))));
            sheet.Activation = true;
            sheet.Awake = _shrines.IsAwake(id);
            if (sheet.Awake)
            {
                var left = Math.Max(0, (_shrines.WindowEndsAt(id) ?? now) - now);
                sheet.AwakeFraction = window > 0 ? (float)(left / window) : 0;
                sheet.AwakeLeft = _localizer.Tr("{time} left", ("time", _numbers.Duration(Math.Ceiling(left / 1000))));
                sheet.ActivationNote = _localizer.Tr("Awake over {reach}. Taking it out ends the window.", ("reach", reach));
                return;
            }

            var cost = _shrines.ActivationCost(id);
            sheet.AsleepWax = _localizer.Tr("Asleep");
            sheet.ActivateLabel = _localizer.Tr("Activate");
            sheet.ActivatePrice = _prices.Of(new Dictionary<string, double> { [ManaPool.MANA] = cost });
            sheet.ActivationNote = _localizer.Tr("Awake for {time} over {reach}", ("time", _numbers.Duration(Math.Ceiling(window / 1000))), ("reach", reach));
            if (_mana.Amount >= cost) return;
            var regen = _numbers.Exact(Math.Round(_mana.PerHour));
            var shortBy = cost - _mana.Amount;
            sheet.ManaNote = _mana.PerHour > 0
                ? _localizer.Tr("Mana refills {n} an hour: {cost} in {time}", ("n", regen), ("cost", _numbers.Exact(cost)),
                    ("time", _numbers.Countdown(Math.Ceiling(shortBy / _mana.PerHour * 3600))))
                : _localizer.Tr("Mana refills {n} an hour", ("n", regen));
            if (_actions.SmallestFlask() is { } flask)
            {
                sheet.FlaskLabel = _localizer.Tr("Use");
                sheet.FlaskNote = _localizer.Tr("Mana flask ×{n}", ("n", _numbers.Exact(flask.Count)));
            }
        }

        private static int Square(int radius) => (2 * radius + 1) * (2 * radius + 1);

        // "Shrine #2": its name and its ordinal.
        private string Label(DistrictState district)
        {
            var building = (BuildingAsset)_buildings.Get(district.DefinitionId);
            return _localizer.Tr(building.DisplayName) + " #" + _numbers.Number(district.Ordinal);
        }
    }
}
