using System.Collections.Generic;

namespace Codigames.Game.Data.Research
{
    public interface ITechnologyCards
    {
        // Null when there is no such technology.
        ITechnologyCard Card(string id);

        IReadOnlyList<ITechnologyCard> Cards { get; }
    }
}
