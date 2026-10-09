using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Widgets
{
    // The kit's glass bar: a trough, a fill that runs to the fraction (blue while it fills, green once done),
    // a rim over both, and the numbers inside. Its slices keep their proportions at any height.
    [ExecuteAlways]
    public class ProgressBar : MonoBehaviour
    {
        // The source pixels of each piece's round ends, and the share of the bar's height they are drawn at.
        private const float BASE_END = 54;
        private const float BASE_SHARE = 0.42f;
        private const float BORDER_END = 64;
        private const float BORDER_SHARE = 0.5f;
        private const float LABEL_SHARE = 0.6f;

        [SerializeField] private Image _base;
        [SerializeField] private RectTransform _fillArea;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _border;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Sprite _blue;
        [SerializeField] private Sprite _green;

        private float _fraction;

        public void Set(float fraction, string label, bool done = false)
        {
            _fraction = Mathf.Clamp01(fraction);
            _fill.sprite = done ? _green : _blue;
            _label.text = label;
            Fit();
        }

        // A bar in a fill of its own (a fragments bar's gold toward a recruit).
        public void Set(float fraction, string label, Sprite fill)
        {
            _fraction = Mathf.Clamp01(fraction);
            _fill.sprite = fill != null ? fill : _blue;
            _label.text = label;
            Fit();
        }

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            var height = ((RectTransform)transform).rect.height;
            if (height <= 0 || _base == null) return;

            _base.pixelsPerUnitMultiplier = BASE_END / (BASE_SHARE * height);
            _fill.pixelsPerUnitMultiplier = BASE_END / (BASE_SHARE * height);
            _border.pixelsPerUnitMultiplier = BORDER_END / (BORDER_SHARE * height);
            _label.fontSize = LABEL_SHARE * height;

            // Never narrower than its own two ends: below that it shows as their sliver.
            var fill = (RectTransform)_fill.transform;
            var ends = 2 * BASE_SHARE * height;
            var width = _fillArea.rect.width;
            fill.gameObject.SetActive(_fraction > 0);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.pivot = new Vector2(0, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(Mathf.Max(ends * Mathf.Min(1, _fraction * 4), _fraction * width), 0);
        }
    }
}
