using Codigames.Kingdom.Harvest;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "Features", menuName = "Kingdom/Data/Feature Collection")]
    public class FeatureCollection : DefinitionCollection<IFeatureDefinition, FeatureAsset>
    {
        public override string Title => "Features";
    }
}
