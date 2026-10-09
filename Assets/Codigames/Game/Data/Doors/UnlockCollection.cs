using UnityEngine;

namespace Codigames.Game.Data.Doors
{
    // Every unlock splash, in the order two show in.
    [CreateAssetMenu(fileName = "Unlocks", menuName = "Kingdom/Data/Unlock Collection")]
    public class UnlockCollection : DefinitionCollection<IUnlock, UnlockAsset>
    {
        public override string Title => "Unlocks";
    }
}
