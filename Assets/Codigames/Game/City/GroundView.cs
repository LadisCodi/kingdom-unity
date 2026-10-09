using Codigames.Game.Data.Harvest;
using Codigames.Game.Map;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Map;
using Codigames.Modules.Clock;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.City
{
    // Keeps the features layer in step with the ground: a feature gone from the ground (under the Townhall,
    // emptied for good) is gone from the map, one that comes back is drawn again, a cell emptied and growing
    // back is drawn dim, and nothing under the cloud bank is drawn. A block (a 3 × 3 mountain) is drawn once, on its
    // bottom cell, so its feet stand on the footprint's bottom corner.
    public class GroundView : MonoBehaviour
    {
        [SerializeField] private Color _exhausted = new(0.55f, 0.5f, 0.45f, 1f);
        [SerializeField, Tooltip("What stands on ground seen but not yet the kingdom's: drained of colour under the veil.")]
        private Color _discovered = new(0.8f, 0.8f, 0.88f, 1f);

        private GroundState _ground;
        private ProvinceMap _map;
        private Harvesting _harvesting;
        private FeatureCollection _features;
        private IClock _clock;
        private FogOfWar _fog;
        private Footprints _footprints;

        [Inject]
        public void Construct(GroundState ground, ProvinceMap map, Harvesting harvesting, FeatureCollection features, IClock clock,
            FogOfWar fog, Footprints footprints)
        {
            _footprints = footprints;
            _fog = fog;
            _ground = ground;
            _map = map;
            _harvesting = harvesting;
            _features = features;
            _clock = clock;
        }

        private void Start()
        {
            Refresh();
            _harvesting.DepotChanged += OnDepotChanged;
            _harvesting.FeatureRemoved += OnFeatureRemoved;
            _harvesting.FeatureAppeared += OnFeatureAppeared;
            _fog.Changed += OnFogChanged;
        }

        private void OnDestroy()
        {
            if (_harvesting == null) return;

            _harvesting.DepotChanged -= OnDepotChanged;
            _harvesting.FeatureRemoved -= OnFeatureRemoved;
            _harvesting.FeatureAppeared -= OnFeatureAppeared;
            _fog.Changed -= OnFogChanged;
        }

        public void Refresh()
        {
            var layer = _map.Features;
            var wanted = new System.Collections.Generic.Dictionary<Vector3Int, (ModuleVector2Int Cell, TileBase Tile)>();
            foreach (var feature in _ground.Features)
            {
                if (Hidden(feature.Key)) continue;
                wanted[ProvinceCoordinates.ToTilemap(DrawnAt(feature.Key))] = (feature.Key, TileOf(feature.Key, feature.Value));
            }

            foreach (var position in layer.cellBounds.allPositionsWithin)
            {
                if (layer.HasTile(position) && !wanted.ContainsKey(position)) layer.SetTile(position, null);
            }

            foreach (var drawn in wanted)
            {
                if (layer.GetTile(drawn.Key) != drawn.Value.Tile) layer.SetTile(drawn.Key, drawn.Value.Tile);
                Tint(drawn.Value.Cell);
            }
        }

        private void OnDepotChanged(ModuleVector2Int cell) => Tint(cell);

        private void OnFogChanged(System.Collections.Generic.IReadOnlyCollection<ModuleVector2Int> cells) => Refresh();

        // Under the cloud bank nothing on the ground is drawn.
        private bool Hidden(ModuleVector2Int cell) => _fog.VisibilityAt(cell) == Visibility.Undiscovered;

        private void OnFeatureRemoved(ModuleVector2Int cell) => _map.Features.SetTile(ProvinceCoordinates.ToTilemap(cell), null);

        private void OnFeatureAppeared(ModuleVector2Int cell, string featureId)
        {
            if (!Hidden(cell)) Draw(cell, featureId);
        }

        private void Draw(ModuleVector2Int cell, string featureId)
        {
            var tile = TileOf(cell, featureId);
            if (tile != null) _map.Features.SetTile(ProvinceCoordinates.ToTilemap(DrawnAt(cell)), tile);
        }

        private TileBase TileOf(ModuleVector2Int cell, string featureId)
            => _features.TryGet(featureId, out var definition) && definition is FeatureAsset feature ? feature.TileFor(_footprints.SizeOf(cell)) : null;

        // A block's bottom cell, else the cell itself.
        private ModuleVector2Int DrawnAt(ModuleVector2Int cell)
        {
            var anchor = _footprints.AnchorOf(cell);
            var size = _footprints.SizeOf(cell);
            return size <= 1 ? cell : new ModuleVector2Int(anchor.X + size - 1, anchor.Y + size - 1);
        }

        private void Tint(ModuleVector2Int cell)
        {
            var position = ProvinceCoordinates.ToTilemap(DrawnAt(cell));
            _map.Features.SetTileFlags(position, TileFlags.None);
            var colour = _harvesting.IsExhausted(cell, _clock.NowMs) ? _exhausted : Color.white;
            if (_fog.VisibilityAt(cell) == Visibility.Discovered) colour *= _discovered;
            _map.Features.SetColor(position, colour);
        }
    }
}
