using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Tutorial
{
    // A scene: what starts it, where it plays, what settles it unplayed, and its lines in order.
    public interface ISceneDefinition : IIdentifiable
    {
        Condition Trigger { get; }

        // It may start over an open sheet.
        bool Anywhere { get; }

        // An introduction: it waits out the breath between scenes and lets others go first. False for the First
        // Morning's beats, which run strictly in order.
        bool Skippable { get; }

        SceneWhere Where { get; }

        // True when the player has already done what it teaches; unset for never.
        Condition DoneWhen { get; }

        IReadOnlyList<ISceneLine> Lines { get; }
    }
}
