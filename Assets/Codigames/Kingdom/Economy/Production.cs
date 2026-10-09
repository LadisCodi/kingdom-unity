using System.Linq;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;

namespace Codigames.Kingdom.Economy
{
    // Gold from rent and the buildings' own making, plus what the crews bring home; any other coin, the crews alone.
    public class Production : IProduction
    {
        private const string GOLD = "Gold";

        private readonly CityState _city;
        private readonly Stores _stores;
        private readonly Workforce _crews;

        public Production(CityState city, Stores stores, Workforce crews)
        {
            _city = city;
            _stores = stores;
            _crews = crews;
        }

        public double MakesPerSecond(string currency)
        {
            var gathered = _crews.GatherPerSecond(currency);
            return currency == GOLD ? _city.Districts.Sum(_stores.GoldPerMinute) / 60 + gathered : gathered;
        }
    }
}
