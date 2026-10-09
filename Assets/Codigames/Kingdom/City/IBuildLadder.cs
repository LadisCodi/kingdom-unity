using System.Collections.Generic;

namespace Codigames.Kingdom.City
{
    // Port: a kind of building priced off a ladder of its own rather than its curve (the Shrines: the ruin first, then
    // one for materials, then Gems). Null price: its usual one.
    public interface IBuildLadder
    {
        // What stops one more of it beyond the usual rules; None when nothing does.
        ConstructionRefusal Refusal(IBuildingDefinition building);

        IReadOnlyDictionary<string, double> Price(IBuildingDefinition building);

        // One was placed at the ladder's price.
        void Built(IBuildingDefinition building);
    }
}
