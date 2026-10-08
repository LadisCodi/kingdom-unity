using Codigames.Kingdom.City;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // Every building, in the build menu's order.
    [CreateAssetMenu(fileName = "Buildings", menuName = "Kingdom/Data/Building Collection")]
    public class BuildingCollection : DefinitionCollection<IBuildingDefinition, BuildingAsset>
    {
        public override string Title => "Buildings";
    }
}
