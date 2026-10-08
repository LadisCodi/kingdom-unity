using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Hud
{
    // One currency on the header's plank: its icon over the slot's left end, its amount, and a + when the
    // store sells it. View only: the header's presenter fills it.
    public class CurrencySlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private GameObject _plus;
        [SerializeField] private LayoutElement _layout;
        [SerializeField, Tooltip("The amount's right margin without and with the +.")]
        private Vector2 _amountRight = new(21, 53);

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

        public void SetAmount(string amount) => _amount.text = amount;

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnTapped() => Tapped?.Invoke();
    }
}
