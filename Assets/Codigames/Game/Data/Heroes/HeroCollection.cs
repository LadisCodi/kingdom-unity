using Codigames.Kingdom.Heroes;
using UnityEngine;

namespace Codigames.Game.Data.Heroes
{
    // Every hero, in the roster's order.
    [CreateAssetMenu(fileName = "Heroes", menuName = "Kingdom/Data/Hero Collection")]
    public class HeroCollection : DefinitionCollection<IHeroDefinition, HeroAsset>
    {
        public override string Title => "Heroes";
    }
}
