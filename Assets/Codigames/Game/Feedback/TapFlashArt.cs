using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Feedback
{
    // What a tap's white flash is drawn with: the sprite added onto itself, and for a fog cell a diamond in the
    // cloud's sunlit tone (the web's PALETTE.fogFlash).
    [CreateAssetMenu(fileName = "TapFlashArt", menuName = "Kingdom/Art/Tap Flash Art")]
    public class TapFlashArt : ScriptableObject
    {
        [SerializeField, Required, Tooltip("Kingdom/Sprite Flash: what a building or a feature flashes with.")] private Material _flash;
        [SerializeField, Required, PreviewField(32), Tooltip("One cell's diamond, a fog cell's flash.")] private Sprite _cell;
        [SerializeField] private Color _fogFlash = new(1f, 0.973f, 0.941f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("How white a fog cell goes at the flash's peak.")] private float _fogAlpha = 0.75f;

        public Material Flash => _flash;
        public Sprite Cell => _cell;
        public Color FogFlash => _fogFlash;
        public float FogAlpha => _fogAlpha;
    }
}
