using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Goods;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Data
{
    // A price as a line reads it: its coins, then its refined goods, each marked when the kingdom is short of it.
    public class PriceTerms
    {
        private readonly ITreasury _treasury;
        private readonly Stockpile _stockpile;
        private readonly NumberFormat _numbers;

        public PriceTerms(ITreasury treasury, Stockpile stockpile, NumberFormat numbers)
        {
            _treasury = treasury;
            _stockpile = stockpile;
            _numbers = numbers;
        }

        public IReadOnlyList<PriceTerm> Of(IReadOnlyDictionary<string, double> currencies, IReadOnlyDictionary<string, double> goods = null)
        {
            var terms = currencies.Where(p => p.Value > 0)
                .Select(p => new PriceTerm(p.Key, _numbers.Exact(p.Value), _treasury.Get(p.Key) < p.Value)).ToList();
            if (goods != null)
                terms.AddRange(goods.Where(g => g.Value > 0).Select(g => new PriceTerm(g.Key, _numbers.Exact(g.Value), _stockpile.Get(g.Key) < g.Value)));
            return terms;
        }
    }
}
