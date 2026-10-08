using System.Collections.Generic;

namespace Kingdom.Sim.Data
{
    // A relic's passive: the numbers it moves, its value at level 1 and per level.
    public sealed class RelicPassive
    {
        public IReadOnlyList<RelicStat> Stats { get; set; }
        public double Base { get; set; }
        public double PerLevel { get; set; }
    }
}
