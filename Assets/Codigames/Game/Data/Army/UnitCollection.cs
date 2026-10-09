using Codigames.Kingdom.Army;
using UnityEngine;

namespace Codigames.Game.Data.Army
{
    // Every unit, in the order the halls show them.
    [CreateAssetMenu(fileName = "Units", menuName = "Kingdom/Data/Unit Collection")]
    public class UnitCollection : DefinitionCollection<IUnitDefinition, UnitAsset>
    {
        public override string Title => "Units";
    }
}
