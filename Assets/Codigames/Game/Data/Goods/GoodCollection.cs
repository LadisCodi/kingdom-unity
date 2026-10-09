using Codigames.Kingdom.Goods;
using UnityEngine;

namespace Codigames.Game.Data.Goods
{
    // Every refined good and precious material.
    [CreateAssetMenu(fileName = "Goods", menuName = "Kingdom/Data/Good Collection")]
    public class GoodCollection : DefinitionCollection<IGoodDefinition, GoodAsset>
    {
        public override string Title => "Goods";
    }
}
