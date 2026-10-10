using System;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // THE MANA SHEET (the web's manaSheet): the pool — how full, when it will be, what it draws from the land, that
    // every tap is paid from it — and the refill, a whole pool on top, at two tills: today's rung of Gems, or a short
    // video while one is offered. Each says why when it cannot. View only: the ManaMenuPresenter fills it.
    public class ManaMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _gauge;
        [SerializeField] private TMP_Text _regen;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private TMP_Text _prize;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private TMP_Text _gemsLeft;
        [SerializeField] private TMP_Text _gemsNote;
        [SerializeField] private CostButton _gems;
        [SerializeField] private TMP_Text _adLeft;
        [SerializeField] private TMP_Text _adNote;
        [SerializeField] private KitButton _ad;
        [SerializeField] private TMP_Text _adWhy;
        [SerializeField, Tooltip("Grown or shrunk to fit what it says.")] private RectTransform _window;
        [SerializeField] private RectTransform _content;

        private float? _chrome;

        public event Action CloseTapped;
        public event Action GemsTapped;
        public event Action AdTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            _gems.Button.onClick.AddListener(() => GemsTapped?.Invoke());
            _ad.onClick.AddListener(() => AdTapped?.Invoke());
        }

        public void Show(ManaSheetData data)
        {
            _title.text = data.Title;
            _name.text = data.Name;
            _status.text = data.Status;
            _fill.fillAmount = Mathf.Clamp01(data.Fill);
            _gauge.text = data.Gauge;
            _regen.text = data.Regen;
            _note.text = data.Note;
            _prize.text = data.Prize;
            _amount.text = data.Amount;
            _gemsLeft.text = data.GemsLeft;
            _gemsNote.text = data.GemsNote;
            _gems.Button.Label = data.GemsLabel;
            if (string.IsNullOrEmpty(data.GemsWhy)) _gems.Show(data.GemsPrice, data.GemsEnabled);
            else _gems.ShowGate(data.GemsWhy);
            _adLeft.text = data.AdLeft;
            _adNote.text = data.AdNote;
            _ad.Label = data.AdLabel;
            _ad.gameObject.SetActive(string.IsNullOrEmpty(data.AdWhy));
            _adWhy.gameObject.SetActive(!string.IsNullOrEmpty(data.AdWhy));
            _adWhy.text = data.AdWhy ?? string.Empty;
            Fit();
        }

        protected override void PreShowInternal() => Fit();

        private void Fit()
        {
            if (_window == null || _content == null || !isActiveAndEnabled) return;
            _chrome ??= _window.rect.height - ((RectTransform)_content.parent).rect.height;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _window.sizeDelta = new Vector2(_window.sizeDelta.x, LayoutUtility.GetPreferredHeight(_content) + _chrome.Value);
        }
    }

    public sealed class ManaSheetData
    {
        public string Title;
        public string Name;
        public string Status;
        public float Fill;
        public string Gauge;
        public string Regen;
        public string Note;
        public string Prize;
        public string Amount;
        public string GemsLeft;
        public string GemsNote;
        public string GemsLabel;
        public System.Collections.Generic.IReadOnlyList<Data.PriceTerm> GemsPrice = Array.Empty<Data.PriceTerm>();
        public bool GemsEnabled;
        public string GemsWhy;
        public string AdLeft;
        public string AdNote;
        public string AdLabel;
        public string AdWhy;
    }
}
