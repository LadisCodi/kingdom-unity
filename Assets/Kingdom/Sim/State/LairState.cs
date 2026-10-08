using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // A lair found: its clock, its hoard, how far it is beaten.
    public sealed class LairState
    {
        public double ArmedAt { get; set; }
        public double? NextRaidAt { get; set; }
        public Dictionary<string, double> Hoard { get; set; }
        public double? Won { get; set; }
        public bool Defeated { get; set; }
        public bool Cleared { get; set; }
        public LairSpoils Spoils { get; set; }
    }
}
