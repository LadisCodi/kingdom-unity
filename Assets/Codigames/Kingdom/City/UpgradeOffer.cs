using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What a district's next level would be: its price, its wait, and what stands in the way.
    public sealed class UpgradeOffer
    {
        public UpgradeOffer(int targetLevel, IReadOnlyDictionary<string, double> price, double seconds,
            ConstructionRefusal refusal, int requiredTownhallLevel, int requiredPopulation = 0, string requiredTech = null)
        {
            RequiredTech = requiredTech;
            RequiredPopulation = requiredPopulation;
            TargetLevel = targetLevel;
            Price = price;
            Seconds = seconds;
            Refusal = refusal;
            RequiredTownhallLevel = requiredTownhallLevel;
        }

        public int TargetLevel { get; }

        // Empty at the highest level.
        public IReadOnlyDictionary<string, double> Price { get; }

        public double Seconds { get; }
        public ConstructionRefusal Refusal { get; }

        // The Townhall level the next level asks for; 0 when it asks for none.
        public int RequiredTownhallLevel { get; }

        // The villagers the next level asks for; 0 when it asks for none.
        public int RequiredPopulation { get; }

        // The technology the next level asks for, while it is not researched; null otherwise.
        public string RequiredTech { get; }
    }
}
