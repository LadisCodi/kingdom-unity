using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // How a technology is shown: its name, its emblem and, for a mechanic only, its written line.
    public interface ITechnologyCard : IIdentifiable
    {
        string DisplayName { get; }

        Sprite Icon { get; }

        // A mechanic's prose; empty for anything whose card is generated from what it does.
        string Description { get; }
    }
}
