using Codigames.Kingdom.Bag;
using UnityEngine;

namespace Codigames.Game.Data.Bag
{
    // Every item, in the Bag's order.
    [CreateAssetMenu(fileName = "Items", menuName = "Kingdom/Data/Item Collection")]
    public class ItemCollection : DefinitionCollection<IItemDefinition, ItemAsset>
    {
        public override string Title => "Items";
    }
}
