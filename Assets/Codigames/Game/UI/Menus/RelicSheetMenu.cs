using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Relics;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // A relic's sheet (the web's relicSheet, mockup M67's lower panel), over the Bag it was opened from: the relic,
    // lit and ribboned awake, dimmed under the Zs asleep; what it does as a sentence with its numbers; its numbers as
    // tiles, now → next level; its level, six slots and the one press for its state; its Shrine and the Shrines it could
    // go to; its activation; and the way to more fragments. View only: the RelicSheetMenuPresenter fills it.
    public class RelicSheetMenu : Menu
    {
        private static readonly Color ASLEEP = new(0.82f, 0.8f, 0.76f, 1);
        private static readonly Color INK = new Color32(0x3b, 0x24, 0x12, 0xff);
        private static readonly Color MUTED = new Color32(0x7a, 0x5c, 0x3e, 0xff);

        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private Image _art;
        [SerializeField] private GameObject _glow;
        [SerializeField] private GameObject _zs;
        [SerializeField] private GameObject _ribbon;
        [SerializeField] private TMP_Text _ribbonText;
        [SerializeField] private TMP_Text _story;
        [SerializeField] private StatBand _stats;
        [SerializeField] private TMP_Text _pending;

        [SerializeField] private TMP_Text _levelHead;
        [SerializeField] private RelicSlotsView _slots;
        [SerializeField] private TMP_Text _setNote;
        [SerializeField] private CostButton _press;
        [SerializeField] private TMP_Text _sources;

        [SerializeField] private SectionHead _shrineHead;
        [SerializeField] private GameObject _host;
        [SerializeField] private TMP_Text _hostLine;
        [SerializeField] private RectTransform _hostChoices;
        [SerializeField] private CostButton _hostChoicePrefab;
        [SerializeField] private KitButton _remove;

        [SerializeField] private GameObject _activation;
        [SerializeField] private GameObject _asleepRow;
        [SerializeField] private TMP_Text _wax;
        [SerializeField] private CostButton _activate;
        [SerializeField] private GameObject _awakeRow;
        [SerializeField] private ProgressBar _awakeBar;
        [SerializeField] private Sprite _awakeFill;
        [SerializeField] private TMP_Text _activationNote;
        [SerializeField] private TMP_Text _manaNote;
        [SerializeField] private CostButton _flask;

        [SerializeField] private KitButton _store;

        private readonly List<CostButton> _choices = new();
        private readonly List<string> _choiceIds = new();

        public event Action CloseTapped;
        public event Action PressTapped;
        public event Action<string> HostTapped;
        public event Action RemoveTapped;
        public event Action ActivateTapped;
        public event Action FlaskTapped;
        public event Action StoreTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_activate.Button, "relic-activate");
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;

        public void Show(RelicSheetData sheet)
        {
            _title.text = sheet.Title;
            _art.sprite = sheet.Art;
            _art.color = sheet.Status == RelicStatus.Asleep ? ASLEEP : Color.white;
            _glow.SetActive(sheet.Status == RelicStatus.Awake);
            _zs.SetActive(sheet.Status == RelicStatus.Asleep);
            _ribbon.SetActive(sheet.Status == RelicStatus.Awake);
            _ribbonText.text = sheet.AwakeRibbon;
            _story.text = sheet.Story;
            _stats.Show(sheet.Stats);
            Line(_pending, sheet.Pending);

            _levelHead.text = sheet.LevelHead;
            _slots.Show(sheet.Slots, true);
            Line(_setNote, sheet.SetNote);
            _press.gameObject.SetActive(sheet.Press != RelicPress.None);
            if (sheet.Press != RelicPress.None)
            {
                _press.Button.Label = sheet.PressLabel;
                _press.Button.Material = sheet.PressGreen ? ButtonMaterial.Green : ButtonMaterial.Wood;
                // Without a whole set the price stays and the button greys; the note above says why.
                _press.Show(sheet.PressPrice, !sheet.PressBlocked);
            }

            Line(_sources, sheet.Sources);
            ShowHost(sheet);
            ShowActivation(sheet);
            _store.Label = sheet.StoreLabel;
        }

        private void ShowHost(RelicSheetData sheet)
        {
            var shown = sheet.ShrineHead != null;
            _shrineHead.gameObject.SetActive(shown);
            _host.SetActive(shown);
            if (!shown) return;
            _shrineHead.Title = sheet.ShrineHead;
            _hostLine.text = "<sprite name=\"Shrine\">" + sheet.HostLine;
            _hostLine.color = sheet.HostMuted ? MUTED : INK;
            _choiceIds.Clear();
            for (var i = 0; i < sheet.HostChoices.Count; i++)
            {
                if (i == _choices.Count)
                {
                    var view = Instantiate(_hostChoicePrefab, _hostChoices);
                    var index = i;
                    view.Button.onClick.AddListener(() => HostTapped?.Invoke(_choiceIds[index]));
                    _choices.Add(view);
                }

                _choiceIds.Add(sheet.HostChoices[i].ShrineId);
                _choices[i].gameObject.SetActive(true);
                _choices[i].Button.Label = sheet.HostLabel;
                _choices[i].ShowNote(sheet.HostChoices[i].Note, true);
            }

            for (var i = sheet.HostChoices.Count; i < _choices.Count; i++) _choices[i].gameObject.SetActive(false);
            _remove.gameObject.SetActive(sheet.CanRemove);
            _remove.Label = sheet.RemoveLabel;
        }

        private void ShowActivation(RelicSheetData sheet)
        {
            _activation.SetActive(sheet.Activation);
            if (!sheet.Activation) return;
            _asleepRow.SetActive(!sheet.Awake);
            _awakeRow.SetActive(sheet.Awake);
            if (sheet.Awake) _awakeBar.Set(sheet.AwakeFraction, sheet.AwakeLeft, _awakeFill);
            else
            {
                _wax.text = sheet.AsleepWax;
                _activate.Button.Label = sheet.ActivateLabel;
                _activate.Show(sheet.ActivatePrice, true);
            }

            _activationNote.text = sheet.ActivationNote;
            Line(_manaNote, sheet.ManaNote);
            _flask.gameObject.SetActive(sheet.FlaskNote != null);
            if (sheet.FlaskNote != null)
            {
                _flask.Button.Label = sheet.FlaskLabel;
                _flask.ShowNote(sheet.FlaskNote, true);
            }
        }

        private static void Line(TMP_Text text, string value)
        {
            text.gameObject.SetActive(!string.IsNullOrEmpty(value));
            text.text = value ?? string.Empty;
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _press.Button.onClick.AddListener(OnPress);
            _remove.onClick.AddListener(OnRemove);
            _activate.Button.onClick.AddListener(OnActivate);
            _flask.Button.onClick.AddListener(OnFlask);
            _store.onClick.AddListener(OnStore);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _press.Button.onClick.RemoveListener(OnPress);
            _remove.onClick.RemoveListener(OnRemove);
            _activate.Button.onClick.RemoveListener(OnActivate);
            _flask.Button.onClick.RemoveListener(OnFlask);
            _store.onClick.RemoveListener(OnStore);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnPress() => PressTapped?.Invoke();
        private void OnRemove() => RemoveTapped?.Invoke();
        private void OnActivate() => ActivateTapped?.Invoke();
        private void OnFlask() => FlaskTapped?.Invoke();
        private void OnStore() => StoreTapped?.Invoke();
    }
}
