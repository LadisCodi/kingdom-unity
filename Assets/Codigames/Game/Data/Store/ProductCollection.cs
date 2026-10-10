using Codigames.Kingdom.Store;
using UnityEngine;

namespace Codigames.Game.Data.Store
{
    // Every product, in store.json's order: each shelf lists its own in this order.
    [CreateAssetMenu(fileName = "Products", menuName = "Kingdom/Data/Product Collection")]
    public class ProductCollection : DefinitionCollection<IProductDefinition, ProductAsset>
    {
        public override string Title => "Store";
    }
}
