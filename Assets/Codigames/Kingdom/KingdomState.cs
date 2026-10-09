using System.Collections.Generic;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Magic.State;

namespace Codigames.Kingdom
{
    // Everything a kingdom is that changes: what a save holds.
    public class KingdomState
    {
        public CityState City { get; set; } = new();
        public GroundState Ground { get; set; } = new();

        public HarvestState Harvest { get; set; } = new();

        public ManaState Mana { get; set; } = new();

        public FogState Fog { get; set; } = new();

        // This kingdom's own randomness: every draw hashes it with the parts that name the event.
        public uint Seed { get; set; }

        // Every currency's balance, whoever holds it.
        public Dictionary<string, double> Balances { get; set; } = new();

        // Epoch milliseconds: where the last advance left off.
        public double LastAdvance { get; set; }
    }
}
