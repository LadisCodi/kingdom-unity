using TMPro;
using UnityEngine;

namespace Codigames.Game.City
{
    // A wood-and-glass bar on the map (the web's trough bar): the tube's dark inside, the fill uncovered from the
    // left, the glass over both, and the time inside. Each piece is three-sliced with its ends kept in
    // proportion to the bar's height, as the web draws it.
    public class ProgressBarView : MonoBehaviour
    {
        // The tube and the fill sit inside the glass by these fractions of the bar's height.
        private const float INSET_X = 0.0625f;
        private const float INSET_Y = 0.078f;
        private const float FILL_OVERHANG = 0.03f;
        private const float WORLD_POINTS_PER_UNIT = 10f;

        [SerializeField] private SpriteRenderer _base;
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private SpriteRenderer _border;
        [SerializeField] private TMP_Text _label;

        private float _width = 0.8f;
        private float _height = 0.2f;
        private float _fraction;

        // The bar's size in world units.
        public void SetSize(float width, float height)
        {
            _width = width;
            _height = height;
            Layout();
        }

        public void SetFraction(float fraction)
        {
            _fraction = Mathf.Clamp01(fraction);
            Layout();
        }

        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        private void Layout()
        {
            var tubeWidth = _width - _height * INSET_X * 2f;
            var tubeHeight = _height - _height * INSET_Y * 2f;
            Slice(_base, tubeWidth, tubeHeight, 0f);
            Slice(_border, _width, _height, 0f);

            // The web uncovers the fill to the fraction; here it is drawn that long, never shorter than its two ends.
            var fillFull = tubeWidth + _height * FILL_OVERHANG * 2f;
            var ends = 2f * EndWidth(_fill, tubeHeight);
            var fillWidth = Mathf.Max(ends, fillFull * _fraction);
            _fill.enabled = _fraction > 0f;
            Slice(_fill, fillWidth, tubeHeight, (fillWidth - fillFull) / 2f);

            if (_label != null)
            {
                _label.rectTransform.sizeDelta = new Vector2(_width, _height);
                // The time is 0.6 of the bar's height; a world text's size is about ten points to a world unit.
                _label.fontSize = _height * 0.6f * WORLD_POINTS_PER_UNIT;
            }
        }

        // Draws a sliced sprite at a world size with its ends scaled by the height: the renderer works at the
        // sprite's own height and the transform scales it.
        private static void Slice(SpriteRenderer renderer, float width, float height, float x)
        {
            if (renderer.sprite == null) return;

            var native = renderer.sprite.rect.height / renderer.sprite.pixelsPerUnit;
            var scale = height / native;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.transform.localPosition = new Vector3(x, 0f, 0f);
            renderer.size = new Vector2(width / scale, native);
        }

        private static float EndWidth(SpriteRenderer renderer, float height)
            => renderer.sprite == null ? 0f : renderer.sprite.border.x * height / renderer.sprite.rect.height;
    }
}
