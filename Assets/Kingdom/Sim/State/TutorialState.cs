using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The first-time experience.
    public sealed class TutorialState
    {
        public bool Veteran { get; set; }
        public Dictionary<string, bool> Seen { get; set; }
        public double StartedAt { get; set; }
    }
}
