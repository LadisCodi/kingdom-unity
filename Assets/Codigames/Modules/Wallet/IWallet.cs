using System;
using System.Collections.Generic;

namespace Codigames.Modules.Wallet
{
    // Amounts of currencies, by id.
    public interface IWallet
    {
        // A currency's amount changed: its id and its new amount.
        event Action<string, double> Changed;

        IReadOnlyDictionary<string, double> Balances { get; }

        double Get(string currency);

        bool CanAfford(IReadOnlyDictionary<string, double> cost);

        // Pays the whole cost or nothing.
        bool TryPay(IReadOnlyDictionary<string, double> cost);

        // Adds up to the cap; returns what was actually added.
        double Add(string currency, double amount);
    }
}
