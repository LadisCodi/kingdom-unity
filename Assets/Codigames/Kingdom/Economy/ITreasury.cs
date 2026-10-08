using System;
using System.Collections.Generic;

namespace Codigames.Kingdom.Economy
{
    // Every currency the kingdom holds, whoever holds it: the city, the kingdom or the player.
    public interface ITreasury
    {
        // A currency's amount changed: its id and its new amount.
        event Action<string, double> Changed;

        double Get(string currency);

        // Every balance, by currency.
        IReadOnlyDictionary<string, double> Balances { get; }

        bool CanAfford(IReadOnlyDictionary<string, double> cost);

        // Pays the whole cost or nothing, across holders.
        bool TryPay(IReadOnlyDictionary<string, double> cost);

        // Returns what was actually added (a cap may hold some back).
        double Add(string currency, double amount);
    }
}
