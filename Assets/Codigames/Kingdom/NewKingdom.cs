using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Map;
using Codigames.Modules.Core;

namespace Codigames.Kingdom
{
    // A kingdom on its first second: the new city, every currency at its start.
    public static class NewKingdom
    {
        public static KingdomState Create(IProvinceMap map, IConstructionSettings settings, ICatalog<ICurrencyDefinition> currencies, double now)
        {
            var (city, ground) = NewCity.Create(map, settings);
            var state = new KingdomState { City = city, Ground = ground, LastAdvance = now };

            foreach (var currency in currencies.Items)
            {
                if (currency.Start > 0) state.Balances[currency.Id] = currency.Start;
            }

            return state;
        }
    }
}
