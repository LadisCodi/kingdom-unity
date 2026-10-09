using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // What building one more of a kind would be, read before a plot is picked: the number it would carry, what
    // it costs, how long it takes on the plot nearest the Townhall, how many stand of how many may, and what
    // stands in the way.
    public sealed class BuildOffer
    {
        public BuildOffer(string definitionId, int ordinal, IReadOnlyDictionary<string, double> price, double seconds,
            int count, int? cap, ConstructionRefusal refusal, string requiredTech = null)
        {
            RequiredTech = requiredTech;
            DefinitionId = definitionId;
            Ordinal = ordinal;
            Price = price;
            Seconds = seconds;
            Count = count;
            Cap = cap;
            Refusal = refusal;
        }

        public string DefinitionId { get; }
        public int Ordinal { get; }
        public IReadOnlyDictionary<string, double> Price { get; }
        public double Seconds { get; }
        public int Count { get; }

        // How many may stand at the Townhall's level; null = unlimited.
        public int? Cap { get; }

        public ConstructionRefusal Refusal { get; }

        // The technology that opens it, while it is not researched; null otherwise.
        public string RequiredTech { get; }

        // Several may stand, so each is called by its number (Housing #3).
        public bool Numbered => !Cap.HasValue || Cap.Value > 1;
    }
}
