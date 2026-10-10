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

        [BoxGroup("Presentation")]
        [SerializeField] private string _displayName;
        [BoxGroup("Presentation"), Tooltip("Drawn on the features layer; empty for what is planted, not painted.")]
        [SerializeField] private TileBase _tile;
        [BoxGroup("Presentation"), Tooltip("A block's drawing, by side from 2 × 2."), ShowIf("@_maxFootprint > 1")]
        [SerializeField] private List<TileBase> _blockTiles = new();
        [BoxGroup("Presentation"), Tooltip("Drawn while emptied and growing back; empty to draw it dimmed.")]
        [SerializeField] private TileBase _exhaustedTile;
        [BoxGroup("Presentation"), Tooltip("An emptied block's drawing, by side from 2 × 2."), ShowIf("@_maxFootprint > 1")]
        [SerializeField] private List<TileBase> _exhaustedBlockTiles = new();
        [BoxGroup("Presentation"), Tooltip("Planted and coming up: each stage for an equal share of the wait; empty to draw it emptied.")]
        [SerializeField] private List<TileBase> _growingTiles = new();
        [SerializeField, Tooltip("Whole or growing, it sways in the wind; emptied, it stands still.")] private bool _sways;

        public string Source => _source != null ? _source.Id : null;
        public string RespawnTerrain => _respawnTerrain;
        public int MaxFootprint => _maxFootprint;
        public string DisplayName => _displayName;
        public TileBase Tile => _tile;
        public bool Sways => _sways;

        // What a block of this side is drawn with: the cell's own tile for one, else its block drawing.
        public TileBase TileFor(int size) => Pick(_tile, _blockTiles, size);

        // Emptied: its own drawing when it has one, by side; null when it is drawn dimmed instead.
        public TileBase ExhaustedTileFor(int size) => _exhaustedTile == null ? null : Pick(_exhaustedTile, _exhaustedBlockTiles, size);

        // Planted and `progress` (0…1) of the way grown: the stage reached, or null with no stages drawn.
        public TileBase GrowingTile(double progress)
        {
            if (_growingTiles.Count == 0) return null;
            var stage = System.Math.Min(_growingTiles.Count - 1, (int)System.Math.Floor(progress * _growingTiles.Count));
            return _growingTiles[System.Math.Max(0, stage)];
        }

        private static TileBase Pick(TileBase single, List<TileBase> blocks, int size)
            => size <= 1 || size - 2 >= blocks.Count || blocks[size - 2] == null ? single : blocks[size - 2];
    }
}
