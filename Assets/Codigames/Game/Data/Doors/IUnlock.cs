using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.Doors
{
    // The splash that names something just opened (Docs/features/23-tutorials.md §4.6): what opens it, its title,
    // its icon and one paragraph. Collection order is the order two show in.
    public interface IUnlock : IIdentifiable
    {
        UnlockKind Kind { get; }

        // The door (build, research…) or the book (Sagas, Atlas) whose opening shows it.
        string Target { get; }

        string Title { get; }

        string Text { get; }

        Sprite Icon { get; }
    }
}
