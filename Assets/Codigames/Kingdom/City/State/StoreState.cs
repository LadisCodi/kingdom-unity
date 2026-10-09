using System.Collections.Generic;

namespace Codigames.Kingdom.City.State
{
    // What a building has made and not yet handed over: whole units banked, and the moment it started making the
    // next ones.
    public class StoreState
    {
        public Dictionary<string, double> Held { get; set; } = new();

        // Epoch milliseconds; null while it makes nothing (full, or idle).
        public double? AccruingSince { get; set; }
    }
}
