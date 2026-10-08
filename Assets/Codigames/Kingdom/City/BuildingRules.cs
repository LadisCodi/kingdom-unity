using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What a legal building is. Checked by the data window as it is edited, before a build, and by a test.
    public static class BuildingRules
    {
        public const int MAX_LEVEL = 20;

        public static IEnumerable<string> Problems(IBuildingDefinition building)
        {
            if (string.IsNullOrEmpty(building.Id)) yield return "It has no id.";
            if (building.MaxLevel < 1 || building.MaxLevel > MAX_LEVEL) yield return $"Its max level must be 1 to {MAX_LEVEL}.";
            if (building.Width < 1 || building.Height < 1) yield return "Its footprint must be at least one cell.";

            foreach (var problem in CostProblems(building)) yield return problem;
            foreach (var problem in DurationProblems(building.Duration)) yield return problem;
            foreach (var problem in GateProblems(building)) yield return problem;
        }

        // Rules across the collection: the Townhall's ladder bounds every count cap.
        public static IEnumerable<string> Problems(IEnumerable<IBuildingDefinition> buildings, IConstructionSettings settings)
        {
            var townhall = settings.Townhall;
            if (townhall == null)
            {
                yield return "No building is the Townhall.";
                yield break;
            }

            foreach (var building in buildings)
            {
                if (building.Gates.MaxCountPerTownhallLevel.Count > townhall.MaxLevel)
                    yield return $"{building.Id}: its count caps run past the Townhall's {townhall.MaxLevel} levels.";
            }
        }

        private static IEnumerable<string> CostProblems(IBuildingDefinition building)
        {
            var cost = building.Cost;
            if (cost.PerLevel.Count != building.MaxLevel)
                yield return $"It has {cost.PerLevel.Count} level prices for {building.MaxLevel} levels.";
            if (cost.InstanceLinearGrowth < 0) yield return "Its linear instance growth cannot be negative.";
            if (cost.InstanceExponentialGrowth < 1) yield return "Its exponential instance growth must be at least 1.";

            for (var level = 0; level < cost.PerLevel.Count; level++)
            {
                foreach (var amount in cost.PerLevel[level].Currencies)
                {
                    if (amount.Value < 0) yield return $"Level {level + 1} costs a negative amount of {amount.Key}.";
                }

                foreach (var amount in cost.PerLevel[level].Goods)
                {
                    if (amount.Value < 0) yield return $"Level {level + 1} costs a negative amount of {amount.Key}.";
                }
            }
        }

        private static IEnumerable<string> DurationProblems(IBuildingDuration duration)
        {
            if (duration.BuildSeconds < 0 || duration.UpgradeSeconds < 0 || duration.LateUpgradeSeconds < 0)
                yield return "A duration cannot be negative.";
        }

        private static IEnumerable<string> GateProblems(IBuildingDefinition building)
        {
            var gates = building.Gates;
            if (gates.RequiredTownhallLevelPerLevel.Count > building.MaxLevel - 1)
                yield return "It has more Townhall gates than levels to reach.";
            if (gates.RequiredPopulationPerLevel.Count > building.MaxLevel - 1)
                yield return "It has more population gates than levels to reach.";
        }
    }
}
