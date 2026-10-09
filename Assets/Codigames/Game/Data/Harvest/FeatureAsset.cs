using Codigames.Kingdom.Harvest;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "Feature", menuName = "Kingdom/Data/Feature")]
    public class FeatureAsset : DefinitionAsset, IFeatureDefinition
    {
        [SerializeField, Tooltip("The harvest source it is; empty when nothing can be taken from it.")] private HarvestSourceAsset _source;
        [SerializeField, Required] private string _respawnTerrain = "Grassland";

        [BoxGroup("Presentation"), Tooltip("Drawn on the features layer; empty for what is planted, not painted.")]
        [SerializeField] private TileBase _tile;

        public string Source => _source != null ? _source.Id : null;
        public string RespawnTerrain => _respawnTerrain;
        public TileBase Tile => _tile;
    }
}
