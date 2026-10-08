using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // Landmarks claimed.
    public sealed class LandmarksState
    {
        public Dictionary<string, bool> Claimed { get; set; }
    }
}
