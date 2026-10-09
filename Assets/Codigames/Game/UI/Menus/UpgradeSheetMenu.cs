using System;
using System.Collections.Generic;
using Codigames.Game.UI.Buildings;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The upgrade sheet (the web's upgradeSheet, mockup M35), centred over the map, in the order a player asks:
    // what it becomes — the building now and next, each with its level on an enamel plaque; what it gains — a row per
    // figure the level moves; what it asks — every requirement ticked or crossed, then the price, the wait and the
    // button, and under it why it is off. View only: the UpgradeSheetMenuPresenter fills it.
    public class UpgradeSheetMenu : Menu
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField] private BuildingPortrait _from;
        [SerializeField] private TMP_Text _fromLevel;
        [SerializeField] private BuildingPortrait _to;
        [SerializeField] private TMP_Text _toLevel;
        [SerializeField] private SectionHead _gainsHead;
        [SerializeField] private RectTransform _gains;
        [SerializeField] private UpgradeRow _gainPrefab;
        [SerializeField] private SectionHead _gatesHead;
        [SerializeField] private RectTransform _gates;
        [SerializeField] private GateRow _gatePrefab;
        [SerializeField] private PriceLabel _price;
        [SerializeField] private KitButton _upgrade;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private RectTransform _window;

        private readonly List<UpgradeRow> _gainRows = new();
        private readonly List<GateRow> _gateRows = new();

        public event Action CloseTapped;
        public event Action UpgradeTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_upgrade, "upgrade-go");
        }

        public void Show(UpgradeSheetData sheet)
        {
            _title.text = sheet.Title;
            _from.Show(sheet.From);
            _fromLevel.text = sheet.FromLevel;
            _to.Show(sheet.To);
            _toLevel.text = sheet.ToLevel;

            _gainsHead.gameObject.SetActive(sheet.Gains.Count > 0);
            _gains.gameObject.SetActive(sheet.Gains.Count > 0);
            for (var i = 0; i < sheet.Gains.Count; i++)
            {
                if (i == _gainRows.Count) _gainRows.Add(Instantiate(_gainPrefab, _gains));
                var gain = sheet.Gains[i];
                _gainRows[i].gameObject.SetActive(true);
                _gainRows[i].Show(gain.Icon, gain.Label, gain.Value, gain.Delta, gain.Worse);
            }
            for (var i = sheet.Gains.Count; i < _gainRows.Count; i++) _gainRows[i].gameObject.SetActive(false);

            _gatesHead.gameObject.SetActive(sheet.Gates.Count > 0);
            _gates.gameObject.SetActive(sheet.Gates.Count > 0);
            for (var i = 0; i < sheet.Gates.Count; i++)
            {
                if (i == _gateRows.Count) _gateRows.Add(Instantiate(_gatePrefab, _gates));
                var gate = sheet.Gates[i];
                _gateRows[i].gameObject.SetActive(true);
                _gateRows[i].Show(gate.Icon, gate.Label, gate.Met);
            }
            for (var i = sheet.Gates.Count; i < _gateRows.Count; i++) _gateRows[i].gameObject.SetActive(false);

            _price.Show(sheet.Price, sheet.Time);
            _upgrade.Label = sheet.Button;
            _upgrade.interactable = sheet.CanUpgrade;
            _note.gameObject.SetActive(!string.IsNullOrEmpty(sheet.Note));
            _note.text = sheet.Note;
            LayoutRebuilder.MarkLayoutForRebuild(_window);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _upgrade.onClick.AddListener(OnUpgrade);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _upgrade.onClick.RemoveListener(OnUpgrade);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnUpgrade() => UpgradeTapped?.Invoke();
    }
}
