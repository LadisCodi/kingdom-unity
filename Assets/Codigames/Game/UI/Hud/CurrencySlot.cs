using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Codigames.Game.UI.Hud
{
    // One currency on the header's plank: its icon over the slot's left end, its amount, a + when the store
    // sells it, and a fill under both when it is a pool (Mana). View only: the header's presenter fills it.
    public class CurrencySlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private GameObject _plus;
        [SerializeField] private LayoutElement _layout;
        [SerializeField, Tooltip("The amount's right margin without and with the +.")]
        private Vector2 _amountRight = new(21, 53);
        [SerializeField, Tooltip("A pool's fill under the icon and the amount: its mask, cut to the fraction.")]
        private RectTransform _gauge;
        [SerializeField] private RectTransform _gaugeFill;
        [SerializeField, Tooltip("The amount's size, and a smaller one for a line that takes its turn (a countdown).")]
        private Vector2 _amountSizes = new(30, 22);

        private DG.Tweening.Tween _pulse;

        public event Action Tapped;

        public string CurrencyId { get; private set; }

        public void Show(string currencyId, Sprite icon, bool sold, float width)
        {
            CurrencyId = currencyId;
            _icon.sprite = icon;
            _plus.SetActive(sold);
            _layout.preferredWidth = width;

            var amount = _amount.rectTransform;
            amount.offsetMax = new Vector2(-(sold ? _amountRight.y : _amountRight.x), amount.offsetMax.y);
        }

        public RectTransform Icon => _icon.rectTransform;

        public void Pulse()
        {
            _pulse?.Complete();
            _pulse = _icon.rectTransform.DOPunchScale(Vector3.one * 0.28f, 0.22f, 1, 0f);
        }

        public void SetAmount(string amount, bool small = false)
        {
            _amount.text = amount;
            _amount.fontSize = small ? _amountSizes.y : _amountSizes.x;
        }

        // A pool's fill, 0 to 1; null for a currency that is not a pool.
        public void SetGauge(float? fraction)
        {
            _gauge.gameObject.SetActive(fraction.HasValue);
            if (!fraction.HasValue) return;

            _gauge.anchorMax = new Vector2(Mathf.Clamp01(fraction.Value), 1f);
            // The fill keeps the whole slot's length inside its inset; the mask shows the fraction of it.
            var full = ((RectTransform)_gauge.parent).rect.width - _gauge.offsetMin.x * 2f;
            _gaugeFill.sizeDelta = new Vector2(full, _gaugeFill.sizeDelta.y);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
