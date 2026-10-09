using System.Collections.Generic;
using Codigames.Game.UI.Data;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Widgets
{
    // A price: one label (the Price role) with its icons inline — over a button, or spaced wide in a buy box (an
    // upgrade's, a technology's). Hidden when there is nothing to pay.
    [RequireComponent(typeof(TMP_Text))]
    public class PriceLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _wide;

        // The smallest a long price is drawn, against its role's size.
        private const float MIN_SCALE = 0.7f;

        private string _line;

        public void Show(IReadOnlyList<PriceTerm> price, string trailing = null)
        {
            gameObject.SetActive(price.Count > 0 || !string.IsNullOrEmpty(trailing));
            _line = PriceLine.Of(price, _wide, trailing);
            Fit();
        }

        // A price longer than its place rather than spilling out of its box: over a button it breaks between its terms,
        // as the web's wraps; spaced wide in a buy box it is drawn smaller, all of it.
        private void Fit()
        {
            if (_line == null) return;
            if (!_wide)
            {
                _text.textWrappingMode = TMPro.TextWrappingModes.Normal;
                _text.text = _line;
                return;
            }

            var room = ((RectTransform)transform).rect.width;
            var wanted = _text.GetPreferredValues(_line).x;
            if (room <= 0 || wanted <= room)
            {
                _text.text = _line;
                return;
            }

            var scale = Mathf.Max(MIN_SCALE, Mathf.Floor(room / wanted * 100) / 100f);
            _text.text = "<size=" + Mathf.RoundToInt(scale * 100) + "%>" + _line;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Fit();
        }
    }
}
