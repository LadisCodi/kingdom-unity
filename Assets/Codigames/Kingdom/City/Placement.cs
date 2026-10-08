using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Map;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.City
{
    // Where a building may go: anywhere on the province's dry ground with nothing standing on it, while the
    // Townhall's level allows one more. Layout is never policed beyond that.
    public class Placement
    {
        private const string WATER = "Water";

        private readonly IProvinceMap _map;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;
        private readonly CityState _city;
        private readonly GroundState _ground;

        public Placement(IProvinceMap map, ICatalog<IBuildingDefinition> buildings, IConstructionSettings settings,
            CityState city, GroundState ground)
        {
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

            foreach (var cell in GridMath.Rect(anchor, building.Width, building.Height))
            {
                if (!_map.Contains(cell)) return PlacementProblem.OutsideProvince;
                if (_ground.Features.ContainsKey(cell)) return PlacementProblem.Occupied;

                var standing = CityQueries.At(_city, _buildings, cell);
                if (standing != null && standing.Id != movingId) return PlacementProblem.Occupied;

                if (_map.TerrainAt(cell) == WATER) return PlacementProblem.NeedsLand;
            }

            if (movingId == null)
            {
                var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
                if (cap.HasValue && CityQueries.Count(_city, definitionId) >= cap.Value) return PlacementProblem.CountLimit;
            }

            return PlacementProblem.None;
        }
    }
}
