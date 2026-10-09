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

        // The harvest sources its crew works; empty for a building with no crew.
        IReadOnlyList<string> HarvestSources { get; }

        // The Harmony it supplies standing: a decoration's.
        double HarmonySupply { get; }

        // The Harmony it demands at each level, a total (entry 0 is what building it asks); empty for none.
        IReadOnlyList<double> HarmonyCostPerLevel { get; }

        // The units a military hall trains; empty for any other building.
        IReadOnlyList<string> Trains { get; }

        // The army a hall allows at each level: a total, not a step.
        IReadOnlyList<int> ArmyCapPerLevel { get; }

        // The wounded an Infirmary keeps, by level.
        IReadOnlyList<int> BedsPerLevel { get; }

        // The good a workshop makes; null for a building that is not one.
        string Produces { get; }

        // How many goods a workshop may hold queued, by level.
        IReadOnlyList<int> QueueLengthPerLevel { get; }

        // The feature it puts on the ground instead of standing (a crop plot plants Crops); null for a building.
        string Plants { get; }

        // How many villagers may work for it.
        IReadOnlyList<int> MaxWorkersPerLevel { get; }

        // How far round its footprint its crew reaches, in rings.
        IReadOnlyList<int> InfluenceRadiusPerLevel { get; }

        // How much faster its crew swings than the ground's own rhythm (1 = as authored).
        IReadOnlyList<double> StrikeSpeedPerLevel { get; }

        // Units each delivery carries on top of what the ground gives.
        IReadOnlyList<double> ExtraUnitsPerDeliveryPerLevel { get; }
    }
}
