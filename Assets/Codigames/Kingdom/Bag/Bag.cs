using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Bag
{
    // THE BAG (Docs/proposals/inventory.md): everything the player owns that is neither a currency nor a building,
    // held until it is used.
    // 1. An item is not a currency: nothing is priced in items, and an item leaves only by being used.
    // 2. A chest is a duration of the city's own production, read when it is opened, floored for a coin the city
    //    barely makes yet; what it pays lands in the purse, past any store.
    // 3. A use is a command at `now`: nothing about it is scheduled, so an absence never replays one.
    public class Bag : IItemHoldings
    {
        public const string MANA = "Mana";
        private const double SECONDS_PER_HOUR = 3600;

        // What a chest, or a choice chest, may pay.
        public static readonly IReadOnlyList<string> CHEST_COINS = new[] { "Gold", "Food", "Wood", "Stone" };

        private readonly BagState _state;
        private readonly ICatalog<IItemDefinition> _items;
        private readonly ITreasury _treasury;
        private readonly IProduction _production;
        private readonly IBagSettings _settings;
        private readonly Boosts _boosts;
        private readonly ManaPool _mana;

        public Bag(BagState state, ICatalog<IItemDefinition> items, ITreasury treasury, IProduction production, IBagSettings settings,
            Boosts boosts, ManaPool mana)
        {
            _state = state;
            _items = items;
            _treasury = treasury;
            _production = production;
            _settings = settings;
            _boosts = boosts;
            _mana = mana;
        }

        // Items came in: their id, and how many.
        public event Action<string, int> Granted;

        // Items were used: their id, how many, and what they paid by currency (empty for a boost).
        public event Action<string, int, IReadOnlyDictionary<string, double>> Used;

        public int Count(string id) => _state.Held.TryGetValue(id, out var n) ? n : 0;

        // Every item held, in the catalog's order: the Bag's.
        public IEnumerable<IItemDefinition> Held => _items.Items.Where(i => Count(i.Id) > 0);

        public bool IsFresh(string id) => _state.Fresh.Contains(id);

        // Items come in since the Bag was last opened.
        public int Badge => _state.Badge;

        public void Grant(string itemId, int count)
        {
            if (!_items.Contains(itemId) || count <= 0) return;
            _state.Held[itemId] = Count(itemId) + count;
            _state.Fresh.Add(itemId);
            _state.Badge += count;
            Granted?.Invoke(itemId, count);
        }

        // What one of this chest pays now, by currency: its seconds of what the city makes of its coin, floored. A
        // choice chest pays the coin picked. Nothing for an item that is not a chest.
        public IReadOnlyDictionary<string, double> ChestValue(string id, string choice = null)
        {
            var paid = new Dictionary<string, double>();
            if (!_items.TryGet(id, out var item)) return paid;
            var coin = item.Kind == ItemKind.Chest ? item.Coin : item.Kind == ItemKind.Choice ? choice : null;
            if (coin == null || !CHEST_COINS.Contains(coin)) return paid;

            var made = _production.MakesPerSecond(coin) * item.Seconds;
            var floor = _settings.ChestFloorPerHour * item.Seconds / SECONDS_PER_HOUR;
            paid[coin] = Prices.RoundPrice(Math.Max(floor, made));
            return paid;
        }

        // Uses `count` of an item at `now`: a chest pays `count` times what one pays; a boost starts or extends; a flask
        // fills the pool, what goes over its cap lost; a tome's Knowledge lands over the bar's cap.
        public UseItemResult Use(string id, int count, double now, string choice = null)
        {
            if (!_items.TryGet(id, out var item)) return UseItemResult.UnknownItem;
            if (item.Kind is ItemKind.Speedup or ItemKind.Key or ItemKind.Part) return UseItemResult.UsedElsewhere;
            if (item.Kind == ItemKind.Choice && (choice == null || !CHEST_COINS.Contains(choice))) return UseItemResult.NeedsACoin;
            if (count < 1 || Count(id) < count) return UseItemResult.NotHeld;

            var paid = new Dictionary<string, double>();
            switch (item.Kind)
            {
                case ItemKind.Chest:
                case ItemKind.Choice:
                    foreach (var (coin, amount) in ChestValue(id, choice)) paid[coin] = _treasury.Add(coin, amount * count);
                    break;
                case ItemKind.Boost when item.Boost.HasValue:
                    _boosts.Start(item.Boost.Value, 1 + item.Value / 100, item.Seconds * count, now);
                    break;
                case ItemKind.Flask:
                    _mana.ApplyDue(now);
                    var room = Math.Max(0, _mana.Cap - _mana.Amount);
                    var mana = Math.Min(room, Math.Floor(_mana.Cap * item.Value * count / 100));
                    if (mana > 0) paid[MANA] = _treasury.Add(MANA, mana);
                    break;
                case ItemKind.Tome:
                    paid[KnowledgeBar.KNOWLEDGE] = _treasury.Add(KnowledgeBar.KNOWLEDGE, item.Value * count);
                    break;
            }

            Take(id, count);
            Used?.Invoke(id, count, paid);
            return UseItemResult.Used;
        }

        // Takes `count` of an item out for what spends it (a speed-up on a timer, a part on a repair); false, and nothing
        // taken, when fewer are held.
        public bool Take(string id, int count)
        {
            var held = Count(id);
            if (count < 1 || held < count) return false;
            if (held > count) _state.Held[id] = held - count;
            else
            {
                _state.Held.Remove(id);
                _state.Fresh.Remove(id);
            }

            return true;
        }

        // Its tile was tapped: no longer new.
        public void MarkSeen(string id) => _state.Fresh.Remove(id);

        // The tab an item is kept under: chests with resources, speed-ups and boosts each their own, the rest Other.
        public static BagTab TabOf(IItemDefinition item) => item.Kind switch
        {
            ItemKind.Chest or ItemKind.Choice => BagTab.Resources,
            ItemKind.Speedup => BagTab.SpeedUps,
            ItemKind.Boost => BagTab.Boosts,
            _ => BagTab.Other,
        };

        // What a tab holds, in the catalog's order.
        public IReadOnlyList<IItemDefinition> In(BagTab tab) => Held.Where(i => TabOf(i) == tab).ToList();

        // Whether a tab holds anything not looked at yet.
        public bool IsFresh(BagTab tab) => Held.Any(i => TabOf(i) == tab && IsFresh(i.Id));

        // The Bag was opened: the nav's orb clears.
        public void MarkOpened() => _state.Badge = 0;
    }
}
