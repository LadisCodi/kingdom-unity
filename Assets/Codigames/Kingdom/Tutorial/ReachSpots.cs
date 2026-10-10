using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.Tutorial
{
    // WHERE A WORKER BUILDING WOULD WORK THE MOST (the web's reachSpot): of the anchors it could stand on — clear now, or
    // once the fog over them is paid — the one whose reach holds the most of what its crew works, then a clear one, then
    // the nearest the Townhall. Measured with the player's own building of the kind, or one as it would stand at level 1.
    public class ReachSpots
    {
        private const string WATER = "Water";

        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IProvinceMap _map;
        private readonly Placement _placement;
        private readonly Workforce _crews;
        private readonly FogOfWar _fog;
        private readonly GroundState _ground;
        private readonly SitesState _sites;
        private readonly IProvinceSites _province;
        private readonly IConstructionSettings _settings;

        public ReachSpots(CityState city, ICatalog<IBuildingDefinition> buildings, IProvinceMap map, Placement placement, Workforce crews,
            FogOfWar fog, GroundState ground, SitesState sites, IProvinceSites province, IConstructionSettings settings)
        {
            _city = city;
            _buildings = buildings;
            _map = map;
            _placement = placement;
            _crews = crews;
            _fog = fog;
            _ground = ground;
            _sites = sites;
            _province = province;
            _settings = settings;
        }

        public readonly struct Spot
        {
            public Spot(Vector2Int cell, int works, bool clear)
            {
                Cell = cell;
                Works = works;
                Clear = clear;
            }

            public Vector2Int Cell { get; }
            public int Works { get; }
            public bool Clear { get; }
        }

        // The best spot; clear ground only when asked. Null when nowhere would do.
        public Spot? Best(string definitionId, bool clearOnly)
        {
            if (!_buildings.TryGet(definitionId, out var building)) return null;
            var sample = Sample(definitionId);
            Spot? best = null;
            var bestDistance = int.MaxValue;
            foreach (var cell in _map.Cells)
            {
                var clear = _placement.Check(definitionId, cell, sample.Id == string.Empty ? null : sample.Id) == PlacementProblem.None;
                if (!clear && (clearOnly || !StandsOnceCleared(building, cell, sample.Id))) continue;
                var works = WorksAt(sample, cell);
                var distance = CityQueries.DistanceFromTownhall(_city, _buildings, _settings, cell);
                if (best == null || works > best.Value.Works || (works == best.Value.Works && clear && !best.Value.Clear)
                    || (works == best.Value.Works && clear == best.Value.Clear && distance < bestDistance))
                {
                    best = new Spot(cell, works, clear);
                    bestDistance = distance;
                }
            }

            return best;
        }

        // How many cells its crew would work standing at `anchor`.
        public int WorksAt(string definitionId, Vector2Int anchor, string movingId)
        {
            var sample = movingId != null ? _city.Districts.FirstOrDefault(d => d.Id == movingId) ?? Sample(definitionId) : Sample(definitionId);
            return WorksAt(sample, anchor);
        }

        private int WorksAt(DistrictState sample, Vector2Int anchor)
            => _crews.Workable(new DistrictState
            {
                Id = sample.Id, DefinitionId = sample.DefinitionId, Level = sample.Level, Anchor = anchor, Built = true,
            }).Count;

        private DistrictState Sample(string definitionId)
            => _city.Districts.FirstOrDefault(d => d.DefinitionId == definitionId)
               ?? new DistrictState { Id = string.Empty, DefinitionId = definitionId, Level = 1, Built = true };

        // Could the footprint stand on `anchor` once its fog is paid? Every cell clear already, or buyable and bare.
        private bool StandsOnceCleared(IBuildingDefinition building, Vector2Int anchor, string movingId)
            => GridMath.Rect(anchor, building.Width, building.Height).All(c =>
            {
                if (!_map.Contains(c) || _map.TerrainAt(c) == WATER) return false;
                if (_fog.IsRevealed(c))
                {
                    var standing = CityQueries.At(_city, _buildings, c);
                    return !_ground.Features.ContainsKey(c) && (standing == null || standing.Id == movingId);
                }

                return _fog.VisibilityAt(c) == Visibility.Discovered && _fog.IsPayable(c) && _fog.TerrainTech(c) == null
                       && !_ground.Features.ContainsKey(c) && CityQueries.At(_city, _buildings, c) == null
                       && !_province.Abandoned.Any(a => a.Anchor == c && !_sites.Repaired.Contains(a.Id));
            });
    }
}
