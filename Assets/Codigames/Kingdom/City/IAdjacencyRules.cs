using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    public interface IAdjacencyRules
    {
        IReadOnlyList<AdjacencyRule> Rules { get; }
    }
}
