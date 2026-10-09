using Codigames.Kingdom.Harvest;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "HarvestSources", menuName = "Kingdom/Data/Harvest Source Collection")]
    public class HarvestSourceCollection : DefinitionCollection<IHarvestSource, HarvestSourceAsset>
    {
        public override string Title => "Harvest sources";
    }
}
