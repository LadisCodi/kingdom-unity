using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Relics.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Relics
{
    // THE SHRINE LADDER (the web's shrineBuild): the ruined Shrine in the province is repaired first; then a few are
    // built for their materials (their usual price, dear by design); every one after asks Gems, one price each, until
    // the ladder ends.
    public class ShrineLadder : IBuildLadder
    {
        private const string GEMS = "Gems";

        private readonly RelicsState _state;
        private readonly IRelicSettings _settings;
        private readonly SitesState _sites;
        private readonly IProvinceSites _province;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly City.State.CityState _city;

        public ShrineLadder(RelicsState state, IRelicSettings settings, SitesState sites, IProvinceSites province,
            ICatalog<IBuildingDefinition> buildings, City.State.CityState city)
        {
            _state = state;
            _settings = settings;
            _sites = sites;
            _province = province;
            _buildings = buildings;
            _city = city;
        }

        private IEnumerable<IAbandonedSite> Ruins(IBuildingDefinition building)
            => _province.Abandoned.Where(a => a.District == building.Id);

        // How many were built for materials so far: every Shrine standing, less the ruins and the ones bought.
        private int ForMaterials(IBuildingDefinition building)
            => CityQueries.Count(_city, building.Id) - Ruins(building).Count(r => _sites.Repaired.Contains(r.Id)) - _state.PremiumShrines;

        // The Gems the next one costs; null while it is built for materials, or when the ladder has ended.
        public int? Gems(IBuildingDefinition building)
        {
            if (!building.HostsRelic || ForMaterials(building) < _settings.ShrineMaterialBuilds) return null;
            var prices = _settings.ShrinePremiumGems;
            return _state.PremiumShrines < prices.Count ? prices[_state.PremiumShrines] : null;
        }

        public ConstructionRefusal Refusal(IBuildingDefinition building)
        {
            if (!building.HostsRelic) return ConstructionRefusal.None;
            if (Ruins(building).Any(r => !_sites.Repaired.Contains(r.Id))) return ConstructionRefusal.RuinFirst;
            if (ForMaterials(building) >= _settings.ShrineMaterialBuilds && Gems(building) == null) return ConstructionRefusal.AtCap;
            return ConstructionRefusal.None;
        }

        public IReadOnlyDictionary<string, double> Price(IBuildingDefinition building)
            => Gems(building) is { } gems ? new Dictionary<string, double> { [GEMS] = gems } : null;

        public void Built(IBuildingDefinition building)
        {
            if (Gems(building) != null) _state.PremiumShrines += 1;
        }
    }
}
