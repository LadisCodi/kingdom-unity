using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.UI.Hud;
using UnityEngine;

namespace Codigames.Game.UI.Menus
{
    // The header: a wooden plank across the top with the coins on the left and, past the rope, the currencies
    // that are never contextual on the right. View only: the HeaderMenuPresenter decides what it shows.
    public class HeaderMenu : Menu
    {
        [SerializeField] private RectTransform _coins;
        [SerializeField] private RectTransform _right;
        [SerializeField] private CurrencySlot _slotPrefab;
        [SerializeField] private KnowledgeTab _knowledge;
        [Header("Slot widths")]
        [SerializeField] private float _coinWidth = 152;
        [SerializeField] private float _rightWidth = 204;
        [SerializeField, Tooltip("A right-hand slot that carries a +.")] private float _soldWidth = 172;

        private readonly List<CurrencySlot> _slots = new();

        // A slot was tapped: its currency's id.
        public event Action<string> CurrencyTapped;

        public IReadOnlyList<CurrencySlot> Slots => _slots;

        public KnowledgeTab Knowledge => _knowledge;

        // Rebuilds the slots: the coins on the left, the rest on the right, each in the order given.
        public void SetSlots(IEnumerable<(string Id, Sprite Icon, bool Sold, bool OnRight)> slots)
        {
            ClearSlots();

            foreach (var (id, icon, sold, onRight) in slots)
            {
                var slot = Instantiate(_slotPrefab, onRight ? _right : _coins);
                WireClicks(slot.gameObject);
                slot.Show(id, icon, sold, !onRight ? _coinWidth : sold ? _soldWidth : _rightWidth);
                slot.Tapped += () => CurrencyTapped?.Invoke(id);
                _slots.Add(slot);
            }
        }

        // Where a currency's icon is on the plank; null when it is not on it.
        public RectTransform SlotIcon(string currency) => _slots.FirstOrDefault(s => s != null && s.CurrencyId == currency)?.Icon;

        // The slot swells a little when something lands in it.
        public void Pulse(string currency) => _slots.FirstOrDefault(s => s != null && s.CurrencyId == currency)?.Pulse();

        protected override void DisposeInternal() => ClearSlots();

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }

            _slots.Clear();
        }
    }
}
