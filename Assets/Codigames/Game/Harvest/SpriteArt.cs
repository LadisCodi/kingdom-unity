using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Harvest
{
    // Plain shapes the map draws with: a white pixel, scaled and tinted into a bar.
    [CreateAssetMenu(fileName = "SpriteArt", menuName = "Kingdom/Art/Sprite Art")]
    public class SpriteArt : ScriptableObject
    {
        [SerializeField, Required, PreviewField(32)] private Sprite _pixel;

        public Sprite Pixel => _pixel;
    }
}
