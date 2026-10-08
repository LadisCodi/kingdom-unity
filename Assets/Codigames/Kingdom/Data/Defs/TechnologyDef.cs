using System.Collections.Generic;

namespace Codigames.Kingdom.Data
{
    // A technology, with its slot resolved: an unplaced one reports an inert slot and `Placed` false.
    public sealed class TechnologyDef
    {
        public string Id { get; set; }
        // unlock | bonus | mechanic.
        public string Kind { get; set; }
        public IReadOnlyList<TechUnlock> Unlocks { get; set; }
        public string Tome { get; set; }
        public double Era { get; set; }
        public double Row { get; set; }
        public double Col { get; set; }
        public bool Placed { get; set; }
        // Tree edges: all must be complete first.
        public IReadOnlyList<string> Requires { get; set; }
        // Gold, the city's materials, and Knowledge.
        public IReadOnlyDictionary<string, double> Cost { get; set; }
        public IReadOnlyDictionary<string, double> Goods { get; set; }
        public double AnyPrecious { get; set; }
        public IReadOnlyList<TechEffect> Effects { get; set; }
        // On the tree for its shape; does nothing yet.
        public bool Planned { get; set; }
    }
}
