namespace Codigames.Kingdom.City
{
    // One rule: a building (or a group token, AnyDecoration) standing beside another moves a stat of the first.
    // Directional: `District` receives. A negative time is a faster one.
    public sealed class AdjacencyRule
    {
        public AdjacencyRule(string district, string neighbor, AdjacencyStat stat, double magnitude)
        {
            District = district;
            Neighbor = neighbor;
            Stat = stat;
            Magnitude = magnitude;
        }

        public string District { get; }
        public string Neighbor { get; }
        public AdjacencyStat Stat { get; }
        public double Magnitude { get; }
    }
}
