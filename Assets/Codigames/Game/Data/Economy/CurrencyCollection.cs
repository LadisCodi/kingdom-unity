using Codigames.Kingdom.Economy;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Currencies", menuName = "Kingdom/Data/Currency Collection")]
    public class CurrencyCollection : DefinitionCollection<ICurrencyDefinition, CurrencyAsset>
    {
        public override string Title => "Currencies";
    }
}
