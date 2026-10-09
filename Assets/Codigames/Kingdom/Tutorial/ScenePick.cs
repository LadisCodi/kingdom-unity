using System.Collections.Generic;

namespace Codigames.Kingdom.Tutorial
{
    // The director's answer: the scene to start now (or none), and the scenes due but already done, settled unplayed.
    public readonly struct ScenePick
    {
        public ScenePick(ISceneDefinition scene, IReadOnlyList<ISceneDefinition> settled)
        {
            Scene = scene;
            Settled = settled;
        }

        public ISceneDefinition Scene { get; }
        public IReadOnlyList<ISceneDefinition> Settled { get; }
    }
}
