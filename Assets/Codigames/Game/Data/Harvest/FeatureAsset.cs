using System.Collections.Generic;
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
        [SerializeField, Range(1, 3), Tooltip("The largest square its painted cells group into: a mountain is one thing.")]
        private int _maxFootprint = 1;

        [BoxGroup("Presentation"), Tooltip("Drawn on the features layer; empty for what is planted, not painted.")]
        [SerializeField] private TileBase _tile;
        [BoxGroup("Presentation"), Tooltip("A block's drawing, by side from 2 × 2."), ShowIf("@_maxFootprint > 1")]
        [SerializeField] private List<TileBase> _blockTiles = new();

        public string Source => _source != null ? _source.Id : null;
        public string RespawnTerrain => _respawnTerrain;
        public int MaxFootprint => _maxFootprint;
        public TileBase Tile => _tile;

        // What a block of this side is drawn with: the cell's own tile for one, else its block drawing.
        public TileBase TileFor(int size)
            => size <= 1 || size - 2 >= _blockTiles.Count || _blockTiles[size - 2] == null ? _tile : _blockTiles[size - 2];
    }
}
