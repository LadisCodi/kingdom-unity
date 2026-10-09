using Codigames.Kingdom.Tutorial;
using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    // Every scene, in the order the director considers them.
    [CreateAssetMenu(fileName = "Scenes", menuName = "Kingdom/Data/Scene Collection")]
    public class SceneCollection : DefinitionCollection<ISceneDefinition, StageSceneAsset>
    {
        public override string Title => "Scenes";
    }
}
