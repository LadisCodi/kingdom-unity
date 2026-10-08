using System.Collections.Generic;
using Codigames.Kingdom.City.State;

namespace Codigames.Kingdom
{
    // Everything a kingdom is that changes: what a save holds.
    public class KingdomState
    {
        public CityState City { get; set; } = new();
        public GroundState Ground { get; set; } = new();

        // Every currency's balance, whoever holds it.
        public Dictionary<string, double> Balances { get; set; } = new();

        // Epoch milliseconds: where the last advance left off.
        public double LastAdvance { get; set; }
    }
}
