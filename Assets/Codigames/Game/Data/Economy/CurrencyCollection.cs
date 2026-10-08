using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Currencies", menuName = "Kingdom/Data/Currency Collection")]
    public class CurrencyCollection : DefinitionCollection<ICurrencyDefinition, CurrencyAsset>, IPlankCurrencies
    {
        public override string Title => "Currencies";

        public IReadOnlyList<IPlankCurrency> OnPlank =>
            Entries.OfType<CurrencyAsset>().Where(c => c.Place != PlankPlace.Hidden).ToList<IPlankCurrency>();
    }
}
