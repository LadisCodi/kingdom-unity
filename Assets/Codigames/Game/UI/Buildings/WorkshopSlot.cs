using System;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // A place in a workshop's queue (the web's dc-ws-slot): a dim plate while empty; the good and its ✕ while waiting;
    // the good, its bar and its ✕, on brighter parchment, while worked.
    public class WorkshopSlot : MonoBehaviour
    {
        [SerializeField] private Image _plate;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _icon;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private KitButton _cancel;
        [SerializeField] private LayoutElement _element;
        [SerializeField] private Color _shade = new Color32(0xe2, 0xcc, 0xa0, 0xff);
        [SerializeField] private Color _working = new Color32(0xf4, 0xe4, 0xc1, 0xff);
        [SerializeField] private float _width = 145;
        [SerializeField] private float _workingWidth = 252;

        public event Action Cancelled;

        public void Show(WorkshopSlotData slot)
        {
            _group.alpha = slot.Filled ? 1 : 0.45f;
            _plate.color = slot.Working ? _working : _shade;
            _icon.gameObject.SetActive(slot.Filled);
            _icon.sprite = slot.Icon;
            _cancel.gameObject.SetActive(slot.Filled);
            _bar.gameObject.SetActive(slot.Working);
            if (slot.Working) _bar.Set(slot.Progress, string.Empty);
            _element.minWidth = _element.preferredWidth = slot.Working ? _workingWidth : slot.Filled ? _width : _width * 0.53f;
        }

        private void OnEnable() => _cancel.onClick.AddListener(OnCancel);
        private void OnDisable() => _cancel.onClick.RemoveListener(OnCancel);
        private void OnCancel() => Cancelled?.Invoke();
    }
}
