using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // A workshop's panel on its card (Docs/features/17-workshops-and-goods.md §8). View only: the card's presenter fills
    // it and answers its buttons.
    public class WorkshopPanel : MonoBehaviour
    {
        [SerializeField] private Image _goodIcon;
        [SerializeField] private TMP_Text _goodName;
        [SerializeField] private TMP_Text _recipe;
        [SerializeField] private Image _heldIcon;
        [SerializeField] private TMP_Text _held;
        [SerializeField] private TMP_Text _heldLabel;
        [SerializeField] private TMP_Text _crew;
        [SerializeField] private RectTransform _slots;
        [SerializeField] private WorkshopSlot _slotPrefab;
        [SerializeField] private GameObject _nextRow;
        [SerializeField] private TMP_Text _next;
        [SerializeField] private CostButton _finish;
        [SerializeField] private KitButton _speedUp;
        [SerializeField] private TMP_Text _makeNote;
        [SerializeField] private CostButton _make;
        [SerializeField] private Color _ink = new Color32(0x3b, 0x24, 0x12, 0xff);
        [SerializeField] private Color _clay = new Color32(0xd4, 0x55, 0x3e, 0xff);

        private readonly List<WorkshopSlot> _slotViews = new();

        public event Action MakeTapped;
        public event Action<int> CancelTapped;
        public event Action FinishTapped;
        public event Action SpeedUpTapped;

        public void Show(WorkshopPanelData panel)
        {
            _goodIcon.sprite = panel.GoodIcon;
            _heldIcon.sprite = panel.GoodIcon;
            _goodName.text = panel.GoodName;
            _recipe.text = panel.Recipe;
            _held.text = panel.Held;
            _heldLabel.text = panel.HeldLabel;
            _crew.text = panel.CrewLine;
            _crew.color = panel.CrewWarning ? _clay : _ink;

            for (var i = 0; i < panel.Slots.Count; i++)
            {
                if (i == _slotViews.Count)
                {
                    var slot = Instantiate(_slotPrefab, _slots);
                    var index = i;
                    slot.Cancelled += () => CancelTapped?.Invoke(index);
                    _slotViews.Add(slot);
                }

                _slotViews[i].gameObject.SetActive(true);
                _slotViews[i].Show(panel.Slots[i]);
            }

            for (var i = panel.Slots.Count; i < _slotViews.Count; i++) _slotViews[i].gameObject.SetActive(false);

            _nextRow.SetActive(panel.Next != null);
            _next.text = panel.Next;
            _speedUp.gameObject.SetActive(panel.Next != null && panel.SpeedUp);
            _speedUp.Label = panel.SpeedUpLabel;
            _finish.gameObject.SetActive(panel.Next != null && !panel.SpeedUp);
            if (panel.Next != null && !panel.SpeedUp) _finish.Show(panel.Finish, panel.CanFinish);
            _finish.Button.Label = panel.FinishLabel;

            _makeNote.text = panel.MakeNote;
            _make.Button.Label = panel.MakeLabel;
            _make.Show(panel.MakePrice, panel.CanMake);
        }

        private void OnEnable()
        {
            _make.Button.onClick.AddListener(OnMake);
            _finish.Button.onClick.AddListener(OnFinish);
            _speedUp.onClick.AddListener(OnSpeedUp);
        }

        private void OnDisable()
        {
            _make.Button.onClick.RemoveListener(OnMake);
            _finish.Button.onClick.RemoveListener(OnFinish);
            _speedUp.onClick.RemoveListener(OnSpeedUp);
        }

        private void OnMake() => MakeTapped?.Invoke();
        private void OnFinish() => FinishTapped?.Invoke();
        private void OnSpeedUp() => SpeedUpTapped?.Invoke();
    }
}
