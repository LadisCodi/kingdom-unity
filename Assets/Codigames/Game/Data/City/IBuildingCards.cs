using System.Collections.Generic;

namespace Codigames.Game.Data.City
{
    // Every building's card, in the order the build menu lists them.
    public interface IBuildingCards
    {
        IReadOnlyList<IBuildingCard> Cards { get; }
    }
}
