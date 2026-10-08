using Codigames.Kingdom.Core;

namespace Codigames.Kingdom.Data
{
    // A building in ruin where the fog took it.
    public sealed class AbandonedDef
    {
        public string Id { get; set; }
        public string DistrictId { get; set; }
        public Coord Location { get; set; }
        public double Sight { get; set; }
        // Its name on the map, when authored; null = "The old <building>".
        public string Name { get; set; }
    }
}
