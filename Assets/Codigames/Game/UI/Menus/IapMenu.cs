using System;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The purchase confirmation (the web's iapSheet): the product, what it hands over, the price against what is left
    // of the month's budget, why it cannot be bought when it cannot, Not now beside Buy, and the line that no real money
    // moves. View only: the IapMenuPresenter fills it.
    public class IapMenu : Menu
    {
        private static readonly Color OK = new Color32(0x4f, 0x8a, 0x2e, 0xff);
        private static readonly Color SHORT = new Color32(0xc0, 0x39, 0x2b, 0xff);

        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _sub;
        [SerializeField] private TMP_Text _lines;
        [SerializeField] private TMP_Text[] _rowLabels = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _rowValues = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text _note;
        [SerializeField] private TMP_Text _footer;
        [SerializeField] private KitButton _cancel;
        [SerializeField] private KitButton _buy;
        [SerializeField, Tooltip("Grown or shrunk to fit what it says.")] private RectTransform _window;
        [SerializeField] private RectTransform _content;

        // The window's height round its content: its head, frame and padding.
        private float? _chrome;

        public event Action CloseTapped;
        public event Action BuyTapped;

        protected override void InitializeInternal() => CoachTarget.Tag(_close, "close");

        // `after` true: the last row is what is left after (green); false: what is short (red).
        public void Show(string title, Sprite art, string name, string sub, string lines, (string Label, string Value)[] rows, bool after,
            string note, string footer, string cancel, string buy, bool affordable)
        {
            _title.text = title;
            _art.sprite = art;
            _art.enabled = art != null;
            _name.text = name;
            _sub.text = sub;
            _lines.gameObject.SetActive(!string.IsNullOrEmpty(lines));
            _lines.text = lines ?? string.Empty;
            for (var i = 0; i < _rowLabels.Length; i++)
            {
                var shown = i < rows.Length;
                _rowLabels[i].transform.parent.gameObject.SetActive(shown);
                if (!shown) continue;
                _rowLabels[i].text = rows[i].Label;
                _rowValues[i].text = rows[i].Value;
                _rowValues[i].color = i == rows.Length - 1 ? (after ? OK : SHORT) : _rowLabels[i].color;
            }

            _note.gameObject.SetActive(!string.IsNullOrEmpty(note));
            _note.text = string.IsNullOrEmpty(note) ? string.Empty : "<sprite name=\"padlock\"> " + note;
            _footer.text = footer;
            _cancel.Label = cancel;
            _buy.Label = buy;
            _buy.interactable = affordable;
            Fit();
        }

        // Inactive, a layout measures nothing: fitted again once on screen, before the window unrolls.
        protected override void PreShowInternal() => Fit();

        private void Fit()
        {
            if (_window == null || _content == null) return;
            _chrome ??= _window.rect.height - ((RectTransform)_content.parent).rect.height;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _window.sizeDelta = new Vector2(_window.sizeDelta.x, LayoutUtility.GetPreferredHeight(_content) + _chrome.Value);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _cancel.onClick.AddListener(OnClose);
            _buy.onClick.AddListener(OnBuy);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _cancel.onClick.RemoveListener(OnClose);
            _buy.onClick.RemoveListener(OnBuy);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnBuy() => BuyTapped?.Invoke();
    }
}
