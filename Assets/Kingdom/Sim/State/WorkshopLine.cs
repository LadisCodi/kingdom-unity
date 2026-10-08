using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // A workshop's queue, and the moment its work is measured from.
    public sealed class WorkshopLine
    {
        public List<WorkshopItem> Items { get; set; }
        public double Anchor { get; set; }
    }
}
