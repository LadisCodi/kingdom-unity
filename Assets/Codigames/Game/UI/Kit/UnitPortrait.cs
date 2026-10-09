using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Kit
{
    // A unit's round portrait (the web's k-portrait): a paper disc in a flat outline, the bust clipped by the round,
    // and how many there are on a dark pill at its foot. The bust is placed in its 256-px file's pixels, scaled
    // about its bottom centre (bustFraming.json).
    public class UnitPortrait : MonoBehaviour
    {
        private const float FILE = 256f;
        private const float DROP = 0.08f;

        [SerializeField] private RectTransform _mask;
        [SerializeField] private Image _bust;
        [SerializeField] private GameObject _countPill;
        [SerializeField] private TMP_Text _count;
        [SerializeField, Tooltip("The rank coin over its bottom-right, from rank II.")] private GameObject _rankCoin;
        [SerializeField] private TMP_Text _rankNumeral;

        private Vector2 _shift;
        private float _scale = 1f;

        public void Show(Sprite bust, Vector2 shift, float scale, string count)
        {
            _bust.sprite = bust;
            _shift = shift;
            _scale = scale;
            _countPill.SetActive(!string.IsNullOrEmpty(count));
            _count.text = count;
            Fit();
        }

        // The rank struck on its coin; none below II.
        public void ShowRank(int rank)
        {
            if (_rankCoin == null) return;
            _rankCoin.SetActive(rank >= 2);
            if (rank >= 2) _rankNumeral.text = Kingdom.Army.Troops.Roman(rank);
        }

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (_mask == null || _bust == null) return;
            var side = _mask.rect.width;
            var rect = _bust.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(side, side);
            rect.localScale = new Vector3(_scale, _scale, 1f);
            rect.anchoredPosition = new Vector2(_shift.x * side / FILE, -DROP * side - _shift.y * side / FILE);
        }
    }
}
