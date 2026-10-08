using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    // How a currency is shown on the header's plank.
    public interface IPlankCurrency : IIdentifiable
    {
        Sprite Icon { get; }

        PlankPlace Place { get; }

        // The store sells it: its slot carries a + that opens the store.
        bool Sold { get; }
    }
}
