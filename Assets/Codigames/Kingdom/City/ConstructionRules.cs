using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What legal construction settings are.
    public static class ConstructionRules
    {
        public static IEnumerable<string> Problems(IConstructionSettings settings)
        {
            if (settings.Townhall == null) yield return "No building is the Townhall.";
            if (settings.LateUpgradeFromLevel < 2) yield return "The late curve cannot start before level 2.";
            if (settings.StartBuilders < 1) yield return "A city starts with at least one builder.";
            if (settings.MaxBuilders < settings.StartBuilders) yield return "The builder ceiling is below the builders at the start.";
            if (settings.BuilderGemCostBase < 0 || settings.BuilderGemCostGrowth < 1) yield return "A builder's price cannot fall.";
        }
    }
}
