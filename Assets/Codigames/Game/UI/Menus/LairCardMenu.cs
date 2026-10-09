using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Lairs;
using Codigames.Game.UI.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // A lair's card (Docs/features/18-garrisons-and-raids.md §6), the district card's frame: the name on its plank, the
    // painting of the creature at its worst with its flavour line, the countdown said as the threat it is — or, beaten,
    // that the reward waits — the path of fights, the reward, and Attack or Claim. View only.
    public class LairCardMenu : Menu
    {
        private static readonly Color INK = new(0.231f, 0.141f, 0.071f);
        // Beaten: the reward waits, in the leaf's dark green.
        private static readonly Color LEAF_DARK = new(0.247f, 0.541f, 0.180f);

        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _painting;
        [SerializeField] private TMP_Text _flavour;
        [SerializeField] private Image _clockIcon;
        [SerializeField] private TMP_Text _clockLabel;
        [SerializeField] private TMP_Text _clockValue;
        [SerializeField] private Sprite _hourglass;
        [SerializeField] private Sprite _tick;
        [SerializeField] private SectionHead _progressHead;
        [SerializeField] private LairPath _path;
        [SerializeField] private TMP_Text _pathLabel;
        [SerializeField] private SectionHead _rewardHead;
        [SerializeField] private RectTransform _chips;
        [SerializeField] private LairChip _chipPrefab;
        [SerializeField] private KitButton _action;

        [SerializeField, Tooltip("Its window: where its top edge stands, for the camera.")] private MapCard _card;

        private readonly List<LairChip> _shownChips = new();

        public event Action CloseTapped;
        public event Action ActionTapped;

        public float CardTop() => _card.ViewportTop();

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_action, "lair-attack");
        }

        public void Show(LairCardData data)
        {
            _title.text = data.Title;
            _painting.sprite = data.Painting;
            _flavour.text = data.Flavour;
            _clockIcon.sprite = data.Beaten ? _tick : _hourglass;
            _clockLabel.text = data.ClockLabel;
            _clockValue.text = data.ClockValue;
            _clockValue.color = data.Beaten ? LEAF_DARK : INK;
            _progressHead.Title = data.ProgressHead;
            _rewardHead.Title = data.RewardHead;
            _pathLabel.text = data.PathLabel;
            // The path sizes its stones to the width it has: the layout settled first.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_card.transform);
            _path.Show(data.Fights, data.Won);
            for (var i = 0; i < data.Chips.Count; i++)
            {
                if (i == _shownChips.Count) _shownChips.Add(Instantiate(_chipPrefab, _chips));
                _shownChips[i].gameObject.SetActive(true);
                _shownChips[i].Show(data.Chips[i].Icon, data.Chips[i].Value, data.Chips[i].Tag);
            }

            for (var i = data.Chips.Count; i < _shownChips.Count; i++) _shownChips[i].gameObject.SetActive(false);
            _action.Label = data.Action;
        }

        // A tick rewrites the countdown alone.
        public void SetClock(string value) => _clockValue.text = value;

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _action.onClick.AddListener(OnAction);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _action.onClick.RemoveListener(OnAction);
        }

        private void OnClose() => CloseTapped?.Invoke();

        private void OnAction() => ActionTapped?.Invoke();
    }
}
