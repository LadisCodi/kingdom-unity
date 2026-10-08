using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Currencies", menuName = "Kingdom/Data/Currency Collection")]
    public class CurrencyCollection : DefinitionCollection<ICurrencyDefinition, CurrencyAsset>, IPlankCurrencies, ICurrencyIcons
    {
        public override string Title => "Currencies";

        public IReadOnlyList<IPlankCurrency> OnPlank =>
            Entries.OfType<CurrencyAsset>().Where(c => c.Place != PlankPlace.Hidden).ToList<IPlankCurrency>();

        public Sprite IconOf(string currency) => TryGet(currency, out var definition) ? ((CurrencyAsset)definition).Icon : null;
    }
}
