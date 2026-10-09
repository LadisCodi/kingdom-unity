using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Sites.State;

namespace Codigames.Kingdom.Sites
{
    // The pool grows with every landmark claimed, and the Watchtower repaired and standing is a landmark's worth.
    public class SiteManaSources : IManaSources
    {
        private const string WATCHTOWER = "Watchtower";

        private readonly SitesState _sites;
        private readonly CityState _city;
        private readonly IManaSettings _settings;

        public SiteManaSources(SitesState sites, CityState city, IManaSettings settings)
        {
            _sites = sites;
            _city = city;
            _settings = settings;
        }

        public double ExtraCap
            => (_sites.Claimed.Count + (_city.Districts.Any(d => d.DefinitionId == WATCHTOWER && d.Built) ? 1 : 0)) * _settings.LandmarkCap;

        public double ExtraPerHour => 0;
    }
}
