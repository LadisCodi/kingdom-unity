using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // An analytics event the sim raised, waiting for the tick to send it.
    public sealed class SimTrack
    {
        public string Name { get; set; }
        public Dictionary<string, object> Props { get; set; }
        // The sim's time when it happened.
        public double At { get; set; }
        // Made by the replay of an absence.
        public bool Offline { get; set; }
    }
}
