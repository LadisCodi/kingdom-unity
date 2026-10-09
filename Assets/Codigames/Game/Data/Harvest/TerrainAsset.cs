using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "Terrain", menuName = "Kingdom/Data/Terrain")]
    public class TerrainAsset : DefinitionAsset
    {
        [SerializeField, Tooltip("What a cell of this ground holds, as a multiple of its stock, per currency; 1 when unlisted."),
         ListDrawerSettings(ShowFoldout = false)]
        private List<Amount> _yields = new();

        public double YieldOf(string currency)
        {
            foreach (var yield in _yields)
            {
                if (yield.Id == currency) return yield.Value;
            }

            return 1;
        }
    }
}
