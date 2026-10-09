using UnityEngine;

namespace Codigames.Game.Data.Tutorial
{
    [CreateAssetMenu(fileName = "Speakers", menuName = "Kingdom/Data/Speaker Collection")]
    public class SpeakerCollection : DefinitionCollection<ISpeaker, SpeakerAsset>
    {
        public override string Title => "Speakers";
    }
}
