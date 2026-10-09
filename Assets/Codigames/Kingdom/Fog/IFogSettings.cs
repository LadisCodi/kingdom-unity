using System.Collections.Generic;

namespace Codigames.Kingdom.Fog
{
    public interface IFogSettings
    {
        // A cell's Gold by its ring from the Townhall, entry 0 being ring 1; past the last, FallbackGrowth a ring.
        IReadOnlyList<double> CostPerRing { get; }
        double FallbackGrowth { get; }

        // Taps that clear a cell; each charges its share of the price.
        int TapsToReveal { get; }

        double MinCost { get; }

        // The map gets dearer as it is revealed: CountGrowth once per CountStep cells revealed.
        int CountStep { get; }
        double CountGrowth { get; }

        // How many rings from the Townhall a cell may be paid for, by its level from 1.
        IReadOnlyList<int> ReachPerTownhallLevel { get; }
    }
}
