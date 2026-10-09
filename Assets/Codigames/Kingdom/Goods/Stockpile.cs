using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Goods.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Goods
{
    // The city's refined goods (Docs/features/17-workshops-and-goods.md §1): a counter per good, never below zero,
    // spent whole or not at all. A price in goods drops its precious terms while precious materials are not in play.
    public class Stockpile
    {
        private readonly GoodsState _state;
        private readonly ICatalog<IGoodDefinition> _goods;
        private readonly IPreciousGate _precious;

        public Stockpile(GoodsState state, ICatalog<IGoodDefinition> goods, IPreciousGate precious = null)
        {
            _state = state;
            _goods = goods;
            _precious = precious ?? new ShutPreciousGate();
        }

        // A good's count changed: the good and its new count.
        public event Action<string, int> Changed;

        public int Get(string good) => _state.Stock.TryGetValue(good, out var n) ? n : 0;

        public void Add(string good, int count)
        {
            var now = Math.Max(0, Get(good) + count);
            _state.Stock[good] = now;
            Changed?.Invoke(good, now);
        }

        // A price as it is charged: without its precious terms while they are not in play, and without zeros.
        public IReadOnlyDictionary<string, double> Priced(IReadOnlyDictionary<string, double> goods)
        {
            if (goods == null || goods.Count == 0) return new Dictionary<string, double>();
            return goods.Where(g => g.Value > 0 && (_precious.Open || !IsPrecious(g.Key))).ToDictionary(g => g.Key, g => g.Value);
        }

        public bool CanAfford(IReadOnlyDictionary<string, double> goods) => Priced(goods).All(g => Get(g.Key) >= g.Value);

        // Pays the whole price or nothing.
        public bool TryPay(IReadOnlyDictionary<string, double> goods)
        {
            if (!CanAfford(goods)) return false;
            foreach (var (good, amount) in Priced(goods)) Add(good, -(int)amount);
            return true;
        }

        public void Refund(IReadOnlyDictionary<string, double> goods)
        {
            foreach (var (good, amount) in Priced(goods)) Add(good, (int)amount);
        }

        private bool IsPrecious(string good) => _goods.TryGet(good, out var definition) && definition.Precious;
    }
}
