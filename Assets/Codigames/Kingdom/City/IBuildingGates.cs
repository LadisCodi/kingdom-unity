using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What holds a building back: how many may stand at each Townhall level, and what reaching each level asks.
    public interface IBuildingGates
    {
        // By Townhall level, from 1; empty = unlimited. Past its end, the last entry holds.
        IReadOnlyList<int> MaxCountPerTownhallLevel { get; }

        // Entry 0 is what reaching level 2 asks; empty = no gate.
        IReadOnlyList<int> RequiredTownhallLevelPerLevel { get; }
        IReadOnlyList<int> RequiredPopulationPerLevel { get; }
    }
}
