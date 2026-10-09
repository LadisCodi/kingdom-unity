using Codigames.Kingdom.Heroes;
using UnityEngine;

namespace Codigames.Game.Data.Heroes
{
    // Every banner, in the screen's order: the standard one first.
    [CreateAssetMenu(fileName = "Banners", menuName = "Kingdom/Data/Banner Collection")]
    public class BannerCollection : DefinitionCollection<IBannerDefinition, BannerAsset>
    {
        public override string Title => "Banners";
    }
}
