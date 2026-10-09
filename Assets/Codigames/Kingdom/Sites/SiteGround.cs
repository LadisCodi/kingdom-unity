using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.Sites
{
    // The ground the province's sites stand on: an abandoned building takes up its cells until its repair starts.
    public class SiteGround : ISiteGround
    {
        private readonly SitesState _state;
        private readonly IProvinceSites _sites;
        private readonly ICatalog<IBuildingDefinition> _buildings;

        public SiteGround(SitesState state, IProvinceSites sites, ICatalog<IBuildingDefinition> buildings)
        {
            _state = state;
            _sites = sites;
            _buildings = buildings;
        }

        public IEnumerable<IAbandonedSite> StandingRuins => _sites.Abandoned.Where(s => !_state.Repaired.Contains(s.Id));

        // The ruin standing on a cell; null when none does.
        public IAbandonedSite RuinAt(Vector2Int cell) => StandingRuins.FirstOrDefault(s => Cells(s).Contains(cell));

        public bool Holds(Vector2Int cell) => RuinAt(cell) != null;

        public IEnumerable<Vector2Int> Cells(IAbandonedSite site)
        {
            var building = _buildings.Get(site.District);
            return GridMath.Rect(site.Anchor, building.Width, building.Height);
        }

        // Every ruin's footprint, as a block when it is square: what clears from the fog as one.
        public IEnumerable<(Vector2Int Anchor, int Size)> Blocks()
            => _sites.Abandoned.Select(s => (s.Anchor, Building: _buildings.Get(s.District)))
                .Where(s => s.Building.Width == s.Building.Height && s.Building.Width > 1)
                .Select(s => (s.Anchor, s.Building.Width));
    }
}
