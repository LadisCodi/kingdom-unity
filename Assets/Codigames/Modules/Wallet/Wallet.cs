using System;
using System.Collections.Generic;

namespace Codigames.Modules.Wallet
{
    public class Wallet : IWallet
    {
        private readonly Dictionary<string, double> _balances = new();
        private readonly ICurrencyCaps _caps;

        public Wallet(ICurrencyCaps caps = null, IEnumerable<KeyValuePair<string, double>> balances = null)
        {
            _caps = caps;
            if (balances == null) return;

            foreach (var balance in balances) _balances[balance.Key] = balance.Value;
        }

        public event Action<string, double> Changed;

        public IReadOnlyDictionary<string, double> Balances => _balances;

        public double Get(string currency) => _balances.TryGetValue(currency, out var amount) ? amount : 0;

        public bool CanAfford(IReadOnlyDictionary<string, double> cost)
        {
            foreach (var line in cost)
            {
                if (line.Value > 0 && Get(line.Key) < line.Value) return false;
            }

            return true;
        }

        public bool TryPay(IReadOnlyDictionary<string, double> cost)
        {
            if (!CanAfford(cost)) return false;

            foreach (var line in cost)
            {
                if (line.Value > 0) Set(line.Key, Get(line.Key) - line.Value);
            }

            return true;
        }

        public double Add(string currency, double amount)
        {
            if (amount <= 0) return 0;

            var before = Get(currency);
            var cap = _caps?.CapOf(currency);
            // A wallet already over its cap (the cap fell) keeps what it has, and takes nothing more.
            var after = cap.HasValue ? Math.Max(before, Math.Min(cap.Value, before + amount)) : before + amount;
            if (after == before) return 0;

            Set(currency, after);
            return after - before;
        }

        private void Set(string currency, double amount)
        {
            _balances[currency] = amount;
            Changed?.Invoke(currency, amount);
        }
    }
}
