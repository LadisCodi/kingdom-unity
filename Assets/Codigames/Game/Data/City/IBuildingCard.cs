using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // How a building is shown on a card: its name, its promise, the build menu's tab it is under and its art.
    public interface IBuildingCard : IIdentifiable
    {
        string DisplayName { get; }

        // What it does, in one line.
        string Promise { get; }

        string BuildTab { get; }

        bool Buildable { get; }

        // The art of the highest tier at or below a level.
        Sprite ArtFor(int level);
    }
}
