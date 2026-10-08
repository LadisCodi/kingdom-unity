using System.Collections.Generic;

namespace Codigames.Game.Data.Economy
{
    // The currencies the plank may show, in the order it shows them.
    public interface IPlankCurrencies
    {
        IReadOnlyList<IPlankCurrency> OnPlank { get; }
    }
}
