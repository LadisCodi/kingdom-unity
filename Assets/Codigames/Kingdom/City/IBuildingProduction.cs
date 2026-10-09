using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What a building makes and keeps, by level (entry 0 is level 1); an empty list = none of it.
    public interface IBuildingProduction
    {
        // Gold it makes of its own a minute, with nobody in it (the Townhall).
        IReadOnlyList<double> GoldPerMinutePerLevel { get; }

        // Units its store holds, all currencies together.
        IReadOnlyList<double> StorageCapacityPerLevel { get; }

        // Villagers it houses.
        IReadOnlyList<int> PopulationCapacityPerLevel { get; }

        // How much more its residents pay, as a fraction of the base rent: a total at each level.
        IReadOnlyList<double> TaxBonusPerLevel { get; }
    }
}
