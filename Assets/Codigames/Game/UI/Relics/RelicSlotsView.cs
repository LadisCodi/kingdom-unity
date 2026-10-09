using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Relics
{
    // A relic's six slots in a row (the web's rl-slots): each fragment's own piece where it is held, with the count
    // past one; its silhouette in chalk inside a dashed outline where it is missing, so the gap says WHICH piece; the
    // keystone last, larger, gold-rimmed.
    public class RelicSlotsView : MonoBehaviour
    {
        private static readonly Color CHALK = new(0.35f, 0.35f, 0.35f, 0.28f);
        private static readonly Color HELD_RIM = new(0.36f, 0.23f, 0.12f, 0.6f);
        private static readonly Color MISSING_RIM = new(0.36f, 0.23f, 0.12f, 0.45f);
        private static readonly Color HELD_FILL = new(1, 1, 1, 0.35f);
        private static readonly Color GOLD = new Color32(0xc8, 0x8a, 0x1c, 0xff);

        [SerializeField] private List<Image> _fills = new();
        [SerializeField] private List<Image> _rims = new();
        [SerializeField] private List<Image> _dashes = new();
        [SerializeField] private List<Image> _arts = new();
        [SerializeField] private List<TMP_Text> _counts = new();

        public void Show(IReadOnlyList<RelicSlotData> slots, bool counts)
        {
            for (var i = 0; i < _arts.Count && i < slots.Count; i++)
            {
                var slot = slots[i];
                var held = slot.Count > 0;
                _arts[i].sprite = slot.Art;
                _arts[i].color = held ? Color.white : CHALK;
                _fills[i].enabled = held;
                _fills[i].color = HELD_FILL;
                _rims[i].enabled = held;
                _rims[i].color = slot.Keystone ? GOLD : HELD_RIM;
                _dashes[i].enabled = !held;
                _dashes[i].color = slot.Keystone ? GOLD : MISSING_RIM;
                if (i < _counts.Count)
                {
                    _counts[i].gameObject.SetActive(counts && slot.Count > 1);
                    _counts[i].text = slot.CountText;
                }
            }
        }
    }
}
