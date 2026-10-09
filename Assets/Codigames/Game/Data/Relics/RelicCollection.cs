using Codigames.Kingdom.Relics;
using UnityEngine;

namespace Codigames.Game.Data.Relics
{
    // Every relic, in the Bag's order: the city relics first.
    [CreateAssetMenu(fileName = "Relics", menuName = "Kingdom/Data/Relic Collection")]
    public class RelicCollection : DefinitionCollection<IRelicDefinition, RelicAsset>
    {
        public override string Title => "Relics";
    }
}
