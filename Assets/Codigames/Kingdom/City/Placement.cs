using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Map;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.City
{
    // Where a building may go: anywhere on the kingdom's revealed dry ground with nothing standing on it, while
    // the Townhall's level allows one more. Layout is never policed beyond that.
    public class Placement
    {
        private const string WATER = "Water";

        private readonly IProvinceMap _map;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;
        private readonly CityState _city;
        private readonly GroundState _ground;
        private readonly IRevealedGround _revealed;
        private readonly ISiteGround _sites;

        public Placement(IProvinceMap map, ICatalog<IBuildingDefinition> buildings, IConstructionSettings settings,
            CityState city, GroundState ground, IRevealedGround revealed, ISiteGround sites = null)
        {
            _sites = sites;
            _revealed = revealed;
            _map = map;
            _buildings = buildings;
            _settings = settings;
            _city = city;
            _ground = ground;
        }

        // For a move, the moving district neither blocks itself nor counts against the cap.
        public PlacementProblem Check(string definitionId, Vector2Int anchor, string movingId = null)
        {
            var building = _buildings.Get(definitionId);
            var ground = GroundProblem(building, anchor, movingId);
            if (ground != PlacementProblem.None || movingId != null) return ground;

            var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
            return cap.HasValue && CityQueries.Count(_city, definitionId) >= cap.Value
                ? PlacementProblem.CountLimit
                : PlacementProblem.None;
        }

        // The legal anchor nearest the Townhall, where placing a building starts; null when none is left. Ties
        // go to the lower y, then the lower x, so the pick never depends on the map's order.
        public Vector2Int? Nearest(string definitionId)
        {
            var building = _buildings.Get(definitionId);
            Vector2Int? best = null;
            var bestRings = int.MaxValue;

            foreach (var cell in _map.Cells)
            {
                if (GroundProblem(building, cell, null) != PlacementProblem.None) continue;

                var rings = CityQueries.DistanceFromTownhall(_city, _buildings, _settings, cell);
                if (rings < bestRings || (rings == bestRings && Before(cell, best.Value)))
                {
                    best = cell;
                    bestRings = rings;
                }
            }

            return best;
        }

        // Where a lifted tree or crop plot may be put down: any revealed dry cell a building could stand on; the cell it
        // leaves counts as bare.
        public PlacementProblem FeatureProblem(Vector2Int to, Vector2Int leaving)
        {
            if (!_map.Contains(to)) return PlacementProblem.OutsideProvince;
            if (!_revealed.IsRevealed(to)) return PlacementProblem.InFog;
            if ((_ground.Features.ContainsKey(to) && to != leaving) || (_sites?.Holds(to) ?? false)) return PlacementProblem.Occupied;
            if (CityQueries.At(_city, _buildings, to) != null) return PlacementProblem.Occupied;
            return _map.TerrainAt(to) == WATER ? PlacementProblem.NeedsLand : PlacementProblem.None;
        }

        // What the ground says: the footprint on the province, on dry land, with nothing on it.
        private PlacementProblem GroundProblem(IBuildingDefinition building, Vector2Int anchor, string movingId)
        {
            foreach (var cell in GridMath.Rect(anchor, building.Width, building.Height))
            {
                if (!_map.Contains(cell)) return PlacementProblem.OutsideProvince;
                if (!_revealed.IsRevealed(cell)) return PlacementProblem.InFog;
                if (_ground.Features.ContainsKey(cell) || (_sites?.Holds(cell) ?? false)) return PlacementProblem.Occupied;

                var standing = CityQueries.At(_city, _buildings, cell);
                if (standing != null && standing.Id != movingId) return PlacementProblem.Occupied;

                if (_map.TerrainAt(cell) == WATER) return PlacementProblem.NeedsLand;
            }

            return PlacementProblem.None;
        }

        private static bool Before(Vector2Int a, Vector2Int b) => a.Y != b.Y ? a.Y < b.Y : a.X < b.X;
    }
}
