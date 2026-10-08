using System.Collections.Generic;
using Kingdom.Sim.Core;

namespace Kingdom.Sim.State
{
    // A building standing (or being built) in the city.
    public sealed class District
    {
        public string UniqueId { get; set; }
        public string DefinitionId { get; set; }
        // Stamped when placed; prices it for ever.
        public double Ordinal { get; set; }
        public double Level { get; set; }
        public double AssignedWorkers { get; set; }
        // Anchor: the top-left cell.
        public Coord Location { get; set; }
        // UnderConstruction | Built.
        public string State { get; set; }
        public double VisualVariant { get; set; }
        // What waits uncollected in its store.
        public Dictionary<string, double> Stored { get; set; }
        public double? RentAnchor { get; set; }
        // The city relic its Shrine holds.
        public string Hosts { get; set; }
    }
}
