using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Sites
{
    // The abandoned buildings: found in the fog, repaired where they stand. A repair is a build at level 1 —
    // the next ordinal's price, a free builder, room under the count cap — with two differences: no technology
    // is asked, and the ground is not the player's to choose. Its wait is its own when the building has one.
    // A ruin missing a piece asks for it too, and the repair spends it.
    public class Ruins
    {
        private readonly SitesState _state;
        private readonly SiteGround _ground;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly Construction _construction;
        private readonly IRevealedGround _revealed;
        private readonly IRepairItems _items;

        public Ruins(SitesState state, SiteGround ground, ICatalog<IBuildingDefinition> buildings, Construction construction,
            IRevealedGround revealed, IRepairItems items = null)
        {
            _state = state;
            _ground = ground;
            _buildings = buildings;
            _construction = construction;
            _revealed = revealed;
            _items = items;
        }

        // A ruin's repair started: the site, and the district it now is.
        public event Action<IAbandonedSite> Repaired;

        public IEnumerable<IAbandonedSite> Standing => _ground.StandingRuins;

        public IAbandonedSite At(Vector2Int cell) => _ground.RuinAt(cell);

        public IAbandonedSite Get(string id) => Standing.FirstOrDefault(s => s.Id == id);

        // What repairing it costs: the level-1 price of the next of its kind.
        public IReadOnlyDictionary<string, double> Price(IAbandonedSite site) => _construction.Offer(site.District).Price;

        public string MissingItem(IAbandonedSite site)
        {
            var item = _buildings.Get(site.District).Repair?.Item;
            return string.IsNullOrEmpty(item) || (_items?.Held(item) ?? 0) > 0 ? null : item;
        }

        public RepairRefusal Refusal(string id)
        {
            var site = Get(id);
            if (site == null) return RepairRefusal.NotFound;
            if (_ground.Cells(site).Any(c => !_revealed.IsRevealed(c))) return RepairRefusal.NotRevealed;

            var refusal = _construction.RepairRefusal(site.District);
            if (refusal == ConstructionRefusal.NoFreeBuilder) return RepairRefusal.NoFreeBuilder;
            if (refusal == ConstructionRefusal.AtCap) return RepairRefusal.AtCap;
            if (MissingItem(site) != null) return RepairRefusal.MissingItem;
            return refusal == ConstructionRefusal.CannotAfford ? RepairRefusal.CannotAfford : RepairRefusal.None;
        }

        public RepairRefusal Repair(string id, double now)
        {
            var refusal = Refusal(id);
            if (refusal != RepairRefusal.None) return refusal;

            var site = Get(id);
            var building = _buildings.Get(site.District);

            // The ground is the ruin's until the repair starts.
            _state.Repaired.Add(site.Id);
            if (_construction.Repair(site.District, site.Anchor, building.Repair?.Seconds ?? 0, now) != ConstructionRefusal.None)
            {
                _state.Repaired.Remove(site.Id);
                return RepairRefusal.CannotAfford;
            }

            if (!string.IsNullOrEmpty(building.Repair?.Item)) _items?.Take(building.Repair.Item);
            Repaired?.Invoke(site);
            return RepairRefusal.None;
        }
    }
}
