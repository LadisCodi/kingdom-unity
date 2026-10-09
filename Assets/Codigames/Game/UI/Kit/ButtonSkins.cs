using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // Every button material's art, and how a label and a carved glyph read on it, at rest and off.
    [CreateAssetMenu(fileName = "Button Skins", menuName = "Kingdom/UI/Button Skins")]
    public class ButtonSkins : ScriptableObject
    {
        [SerializeField] private List<ButtonSkin> _skins = new();
        [SerializeField] private Material _offLabel;
        [SerializeField] private Color _labelColor = new Color32(0xff, 0xf6, 0xe0, 0xff);
        [SerializeField] private Color _offLabelColor = new Color32(0xe3, 0xe6, 0xec, 0xff);
        [SerializeField] private Color _woodGlyph = new Color32(40, 20, 8, 158);
        [SerializeField] private Color _paintGlyph = new Color32(10, 6, 30, 128);
        [SerializeField] private Color _offGlyph = new Color32(40, 46, 60, 140);

        public Material OffLabel => _offLabel;
        public Color LabelColor => _labelColor;
        public Color OffLabelColor => _offLabelColor;
        public Color OffGlyph => _offGlyph;

        public ButtonSkin Of(ButtonMaterial material)
        {
            foreach (var skin in _skins)
                if (skin.Material == material) return skin;
            return _skins.Count > 0 ? _skins[0] : null;
        }

        // A glyph is carved into the face: a darker tone of wood, or of the paint or stone.
        public Color GlyphOn(ButtonMaterial material) => material == ButtonMaterial.Wood ? _woodGlyph : _paintGlyph;

        public void Set(IEnumerable<ButtonSkin> skins, Material offLabel)
        {
            _skins = new List<ButtonSkin>(skins);
            _offLabel = offLabel;
        }
    }
}
