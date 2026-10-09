using Codigames.Kingdom.Harvest;
using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "Terrains", menuName = "Kingdom/Data/Terrain Collection")]
    public class TerrainCollection : DefinitionCollection<IIdentifiable, TerrainAsset>, ITerrainYields
    {
        public override string Title => "Terrains";

        public double YieldOf(string terrain, string currency)
            => terrain != null && TryGet(terrain, out var definition) ? ((TerrainAsset)definition).YieldOf(currency) : 1;
    }
}
