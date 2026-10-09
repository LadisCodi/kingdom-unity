using System.Collections.Generic;

namespace Codigames.Kingdom.Lairs.State
{
    // Every lair found so far, by id, and the device's offset from UTC: raids land in the player's own day.
    public class LairsState
    {
        public Dictionary<string, LairState> Lairs { get; set; } = new();
        public int UtcOffsetMinutes { get; set; }
    }

    public class LairState
    {
        // Epoch milliseconds: when it was found and its warning began.
        public double ArmedAt { get; set; }
        public double? NextRaidAt { get; set; }
        // What its raids took and it carries, by currency: what clearing it gives back.
        public Dictionary<string, double> Hoard { get; set; } = new();
        // Fights won on its path.
        public int Won { get; set; }
        // Its last fight won: no more raids, its hoard waiting to be claimed.
        public bool Defeated { get; set; }
        // Claimed: gone for good.
        public bool Cleared { get; set; }
        // What the party that beat it carries to the claim: the Lore and Seasoned spoils, as shares.
        public double SpoilsLore { get; set; }
        public double SpoilsSeasoned { get; set; }
    }
}
