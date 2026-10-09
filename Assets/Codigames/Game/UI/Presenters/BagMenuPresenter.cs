using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Bag;
using Codigames.Game.UI.Bag;
using Codigames.Game.UI.Hud;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The Bag's logic (the web's bagSheet and Game.bagScreen): the open tab's items, the picked one's popover with
    // what one is worth now, the quantity and the coin a choice chest pays in, and the boosts running. Using an item
    // flies what it paid into the header; a speed-up goes to a timer, through the picker.
    public class BagMenuPresenter : AbstractMenuPresenter<BagMenu>, IClosableMenuPresenter, ITickable
    {
        private static readonly BagTab[] TABS = { BagTab.Resources, BagTab.SpeedUps, BagTab.Boosts, BagTab.Relics, BagTab.Other };
        private static readonly BoostKind[] BOOSTS = { BoostKind.Rent, BoostKind.Harvest, BoostKind.Mana };

        private readonly UIManager _ui;
        private readonly Kingdom.Bag.Bag _bag;
        private readonly ItemCollection _items;
        private readonly Speedups _speedups;
        private readonly Boosts _boosts;
        private readonly ItemProse _prose;
        private readonly ItemTiles _tiles;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;

        private BagTab _tab = BagTab.Resources;
        private string _picked;
        private int _quantity = 1;
        private string _coin = "Gold";
        private double _shownSecond = -1;

        // What the last item used paid out, as the Bag announced it: what flies into the header.
        private IReadOnlyDictionary<string, double> _lastPaid;

        public BagMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Bag.Bag bag, ItemCollection items, Speedups speedups,
            Boosts boosts, ItemProse prose, ItemTiles tiles, UiIcons icons, NumberFormat numbers, Localizer localizer, IClock clock,
            ISoundService sounds, RewardFlight flight, RewardFragments fragments) : base(views)
        {
            _ui = ui;
            _bag = bag;
            _items = items;
            _speedups = speedups;
            _boosts = boosts;
            _prose = prose;
            _tiles = tiles;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
        }

        public void RequestClose() => _ = _ui.HideMenu<BagMenu>();

        // The ribbons count down: redrawn once a second while the Boosts tab shows any.
        public void Tick()
        {
            if (!IsShown || _tab != BagTab.Boosts) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _shownSecond) return;
            _shownSecond = second;
            Refresh();
        }

        protected override void BindInternal(BagMenu view)
        {
            _bag.MarkOpened();
            Refresh();
            _bag.Granted += OnGranted;
            _bag.Used += OnUsed;
        }

        protected override void UnbindInternal(BagMenu view)
        {
            _bag.Granted -= OnGranted;
            _bag.Used -= OnUsed;
        }

        private void OnUsed(string id, int count, IReadOnlyDictionary<string, double> paid) => _lastPaid = paid;

        protected override void SubscribeToViewEventsInternal(BagMenu view)
        {
            view.CloseTapped += RequestClose;
            view.TabTapped += OnTab;
            view.TileTapped += OnTile;
            view.Popover.QuantityChanged += OnQuantity;
            view.Popover.CoinPicked += OnCoin;
            view.Popover.ActionTapped += OnAction;
        }

        protected override void UnsubscribeFromViewEventsInternal(BagMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.TabTapped -= OnTab;
            view.TileTapped -= OnTile;
            view.Popover.QuantityChanged -= OnQuantity;
            view.Popover.CoinPicked -= OnCoin;
            view.Popover.ActionTapped -= OnAction;
        }

        private void OnGranted(string id, int count) => Refresh();

        private void OnTab(int index)
        {
            if (TABS[index] == _tab) return;
            _tab = TABS[index];
            _picked = null;
            Refresh();
            View.ScrollToTop();
        }

        // A tap opens the tile's popover; a second tap closes it.
        private void OnTile(string id)
        {
            _picked = _picked == id ? null : id;
            _quantity = 1;
            _bag.MarkSeen(id);
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnCoin(string coin)
        {
            _coin = coin;
            Refresh();
        }

        private void OnQuantity(int quantity)
        {
            _quantity = quantity;
            var item = _items.Get<ItemAsset>(_picked);
            View.Popover.ShowQuantity(quantity, Total(item, quantity), UseLabel(item, quantity), _numbers.Exact(quantity));
        }

        private void OnAction()
        {
            if (_picked == null) return;
            var item = _items.Get<ItemAsset>(_picked);
            if (item.Kind == ItemKind.Speedup)
            {
                if (_speedups.FirstJobFor(item.Id, _clock.NowMs) is { } job) _ = _ui.ShowMenu<SpeedupMenu, SpeedJob>(job);
                return;
            }

            var from = View.Popover.ActionScreenPoint();
            _lastPaid = null;
            var result = _bag.Use(item.Id, Math.Min(_quantity, _bag.Count(item.Id)), _clock.NowMs, item.Kind == ItemKind.Choice ? _coin : null);
            if (result != UseItemResult.Used)
            {
                _sounds.Play(SoundIds.ERROR);
                return;
            }

            if (_lastPaid != null && _lastPaid.Count > 0) _flight.Fly(_lastPaid, from, (c, a) => _fragments.For(c, a, false));
            else _sounds.Play(SoundIds.BUTTON_PRESS);
            if (_bag.Count(item.Id) == 0) _picked = null;
            _quantity = 1;
            Refresh();
        }

        private void Refresh()
        {
            var held = _bag.In(_tab).OfType<ItemAsset>().ToList();
            if (_picked != null && held.All(i => i.Id != _picked)) _picked = null;
            var picked = held.FindIndex(i => i.Id == _picked);

            View.Show(new BagScreenData
            {
                Tabs = TABS.Select(t => new BagTabData
                {
                    Label = TabLabel(t),
                    Open = t == _tab,
                    Empty = !_bag.In(t).Any(),
                    Fresh = _bag.IsFresh(t),
                }).ToList(),
                Items = held.Select(i => _tiles.Tile(i, _bag.Count(i.Id), _bag.IsFresh(i.Id), i.Id == _picked)).ToList(),
                Empty = EmptyLine(_tab),
                Ribbons = _tab == BagTab.Boosts ? Ribbons() : Array.Empty<BoostRibbonData>(),
                Picked = picked,
                Popover = picked < 0 ? null : Popover(held[picked], picked % 4),
            });
        }

        private BagPopoverData Popover(ItemAsset item, int column)
        {
            var worth = _bag.ChestValue(item.Id, item.Kind == ItemKind.Choice ? _coin : null);
            var count = _bag.Count(item.Id);
            var quantity = Math.Max(1, Math.Min(_quantity, count));
            var pop = new BagPopoverData
            {
                Name = _prose.Name(item),
                Line = _prose.Line(item, worth),
                Column = column,
                Max = 0,
                Quantity = quantity,
                QuantityText = _numbers.Exact(quantity),
            };

            switch (item.Kind)
            {
                case ItemKind.Key:
                    // A key is spent on its banner's call, in the store: the store is still to come.
                    pop.Action = BagAction.None;
                    return pop;
                case ItemKind.Part:
                    pop.Action = BagAction.None;
                    return pop;
                case ItemKind.Speedup:
                    var job = _speedups.FirstJobFor(item.Id, _clock.NowMs);
                    if (job == null) pop.Note = _localizer.Tr("Nothing of this kind is running");
                    pop.Action = job == null ? BagAction.None : BagAction.SpeedUp;
                    pop.ActionLabel = _localizer.Tr("Speed up a timer");
                    return pop;
            }

            if (item.Kind == ItemKind.Boost && item.Boost.HasValue && _boosts.Running(item.Boost.Value) != null)
                pop.Note = _localizer.Tr("Extends the running one");
            if (item.Kind == ItemKind.Choice)
                pop.Choice = Kingdom.Bag.Bag.CHEST_COINS.Select(c => new ChoicePlateData
                {
                    Coin = c,
                    Icon = _icons.Get(c),
                    Amount = _numbers.Exact(_bag.ChestValue(item.Id, c).TryGetValue(c, out var v) ? v : 0),
                    Picked = c == _coin,
                }).ToList();

            pop.Max = count;
            pop.Total = Total(item, quantity);
            pop.Action = BagAction.Use;
            pop.ActionLabel = UseLabel(item, quantity);
            return pop;
        }

        // "×3 → 1800 [coin]": a chest's pay for the quantity on the slider.
        private string Total(ItemAsset item, int quantity)
        {
            var worth = _bag.ChestValue(item.Id, item.Kind == ItemKind.Choice ? _coin : null);
            if (worth.Count == 0) return string.Empty;
            var coin = worth.First();
            return "×" + _numbers.Exact(quantity) + " → " + _numbers.Exact(coin.Value * quantity) + " <sprite name=\"" + coin.Key + "\">";
        }

        private string UseLabel(ItemAsset item, int quantity)
        {
            var choice = item.Kind == ItemKind.Choice;
            return quantity > 1
                ? choice ? _localizer.Tr("Open ×{n}", ("n", _numbers.Exact(quantity))) : _localizer.Tr("Use ×{n}", ("n", _numbers.Exact(quantity)))
                : choice ? _localizer.Tr("Open") : _localizer.Tr("Use");
        }

        private IReadOnlyList<BoostRibbonData> Ribbons()
        {
            var now = _clock.NowMs;
            return BOOSTS.Select(k => (Kind: k, Running: _boosts.Running(k)))
                .Where(b => b.Running != null && b.Running.EndsAt > now)
                .Select(b => new BoostRibbonData
                {
                    Icon = _icons.Get(b.Kind == BoostKind.Rent ? "boostRent" : b.Kind == BoostKind.Harvest ? "boostHarvest" : "boostMana"),
                    What = _prose.BoostWhat(b.Kind, b.Running.Multiplier),
                    Left = _numbers.Duration(Math.Ceiling((b.Running.EndsAt - now) / 1000)),
                }).ToList();
        }

        private string TabLabel(BagTab tab) => tab switch
        {
            BagTab.Resources => _localizer.Tr("Resources"),
            BagTab.SpeedUps => _localizer.Tr("tab::Speed ups"),
            BagTab.Boosts => _localizer.Tr("tab::Boosts"),
            BagTab.Relics => _localizer.Tr("Relics"),
            _ => _localizer.Tr("Other"),
        };

        private string EmptyLine(BagTab tab) => tab switch
        {
            BagTab.Resources => _localizer.Tr("Chests turn up in the fog and in quests"),
            BagTab.SpeedUps => _localizer.Tr("Speed-ups turn up in lairs and quests"),
            BagTab.Boosts => _localizer.Tr("Boosts turn up in quests, lairs and the Survey"),
            BagTab.Relics => _localizer.Tr("Relic fragments turn up in lairs and in the fog"),
            _ => _localizer.Tr("Keys and flasks turn up as rewards"),
        };
    }
}
