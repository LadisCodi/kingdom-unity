using System.Collections.Generic;
using Codigames.Kingdom.Map;
using UnityEngine;
using UnityEngine.Tilemaps;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // The province as painted in its tilemaps: the terrain layer says which cells exist and their ground, the
    // features layer what stands on them at the start. Read once; the painted map is authored data.
    public class ProvinceMap : MonoBehaviour, IProvinceMap
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private Tilemap _terrain;
        [SerializeField] private Tilemap _features;

        private Dictionary<ModuleVector2Int, string> _terrainByCell;
        private Dictionary<ModuleVector2Int, string> _featureByCell;

        public Grid Grid => _grid;
        public Tilemap Terrain => _terrain;
        public Tilemap Features => _features;

        public IEnumerable<ModuleVector2Int> Cells => TerrainByCell.Keys;

        private Dictionary<ModuleVector2Int, string> TerrainByCell => _terrainByCell ??= Read(_terrain);
        private Dictionary<ModuleVector2Int, string> FeatureByCell => _featureByCell ??= Read(_features);

        public bool Contains(ModuleVector2Int cell) => TerrainByCell.ContainsKey(cell);

        public string TerrainAt(ModuleVector2Int cell) => TerrainByCell.TryGetValue(cell, out var id) ? id : null;

        public string FeatureAt(ModuleVector2Int cell) => FeatureByCell.TryGetValue(cell, out var id) ? id : null;

        // Where a province cell's diamond centre is in the world.
        public Vector3 CellCentre(ModuleVector2Int cell) => _grid.GetCellCenterWorld(ProvinceCoordinates.ToTilemap(cell));

        // The province cell under a world point, whether or not the province holds it.
        public ModuleVector2Int CellAt(Vector3 world) => ProvinceCoordinates.FromTilemap(_grid.WorldToCell(new Vector3(world.x, world.y, 0f)));

        private static Dictionary<ModuleVector2Int, string> Read(Tilemap layer)
        {
            var cells = new Dictionary<ModuleVector2Int, string>();
            if (layer == null) return cells;

            foreach (var position in layer.cellBounds.allPositionsWithin)
            {
                if (layer.GetTile(position) is VariantTile tile) cells[ProvinceCoordinates.FromTilemap(position)] = tile.Id;
            }

            return cells;
        }
    }
}
