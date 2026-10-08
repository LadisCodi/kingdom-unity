using System;
using System.Collections.Generic;
using Codigames.Modules.Core;
using Codigames.Modules.Wallet;

namespace Codigames.Kingdom.Economy
{
    // The kingdom's three purses — the city's, the kingdom's and the player's — each a wallet, and each
    // currency routed to the purse its definition names.
    public class Treasury : ITreasury
    {
        private readonly ICatalog<ICurrencyDefinition> _currencies;
        private readonly Dictionary<CurrencyScope, Wallet> _wallets = new();

        // A new kingdom: every currency at its start.
        public Treasury(ICatalog<ICurrencyDefinition> currencies, ICurrencyCaps caps = null)
            : this(currencies, StartBalances(currencies), caps)
        {
        }

        // A saved kingdom: its balances.
        public Treasury(ICatalog<ICurrencyDefinition> currencies, IReadOnlyDictionary<string, double> balances, ICurrencyCaps caps = null)
        {
            _currencies = currencies;

            foreach (CurrencyScope scope in Enum.GetValues(typeof(CurrencyScope)))
            {
                var wallet = new Wallet(caps);
                wallet.Changed += (currency, amount) => Changed?.Invoke(currency, amount);
                _wallets[scope] = wallet;
            }

            foreach (var balance in balances) WalletOf(balance.Key).Add(balance.Key, balance.Value);
        }

        public event Action<string, double> Changed;

        public IReadOnlyDictionary<string, double> Balances
        {
            get
            {
                var all = new Dictionary<string, double>();
                foreach (var wallet in _wallets.Values)
                {
                    foreach (var balance in wallet.Balances) all[balance.Key] = balance.Value;
                }

                return all;
            }
        }

        public double Get(string currency) => WalletOf(currency).Get(currency);

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
                if (line.Value > 0) WalletOf(line.Key).TryPay(new Dictionary<string, double> { [line.Key] = line.Value });
            }

            return true;
        }

        public double Add(string currency, double amount) => WalletOf(currency).Add(currency, amount);

        private Wallet WalletOf(string currency)
            => _wallets[_currencies.TryGet(currency, out var definition) ? definition.Scope : CurrencyScope.City];

        private static Dictionary<string, double> StartBalances(ICatalog<ICurrencyDefinition> currencies)
        {
            var balances = new Dictionary<string, double>();
            foreach (var currency in currencies.Items)
            {
                if (currency.Start > 0) balances[currency.Id] = currency.Start;
            }

            return balances;
        }
    }
}
