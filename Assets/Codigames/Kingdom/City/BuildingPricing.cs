using System;
using System.Collections.Generic;
using Codigames.Kingdom.Economy;

namespace Codigames.Kingdom.City
{
    // What a level of a building costs: the authored price of that level for the first instance, its
    // currencies multiplied by how many came before (M(N) = linear × (N − 1) + growth^(N − 1)) and rounded
    // to three significant figures; its goods as authored.
    public static class BuildingPricing
    {
        public static double InstanceMultiplier(IBuildingCost cost, int ordinal)
        {
            var n = Math.Max(1, ordinal) - 1;
            return cost.InstanceLinearGrowth * n + Math.Pow(cost.InstanceExponentialGrowth, n);
        }

        // Level 1 is the build.
        public static IReadOnlyDictionary<string, double> Currencies(IBuildingDefinition building, int ordinal, int level)
        {
            var authored = PriceOf(building, level).Currencies;
            var multiplier = InstanceMultiplier(building.Cost, ordinal);
            var price = new Dictionary<string, double>();

            foreach (var line in authored)
            {
                if (line.Value > 0) price[line.Key] = Prices.RoundPrice(line.Value * multiplier);
            }

            return price;
        }

        public static IReadOnlyDictionary<string, double> Goods(IBuildingDefinition building, int level) => PriceOf(building, level).Goods;

        private static ILevelCost PriceOf(IBuildingDefinition building, int level)
        {
            var levels = building.Cost.PerLevel;
            return levels[Math.Min(Math.Max(level, 1), levels.Count) - 1];
        }
    }
}
