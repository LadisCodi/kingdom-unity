using System;
using TMPro;
using Codigames.Game.UI.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Bag
{
    // One item on the Bag's grid (the web's bag-tile): a square of dark wood tinted by its rarity, its picture, the
    // size over a borrowed picture, the count at its foot, a speed-up's badge, the sparkle of something new, and the
    // gold ring while its popover is open.
    public class BagTile : MonoBehaviour
    {
        // The rarities' tints, 1 to 5 (the web's --tier).
        private static readonly Color32[] TIERS =
        {
            new(0xef, 0xe0, 0xbd, 0xff), new(0x8f, 0xd0, 0x6a, 0xff), new(0x6f, 0xb6, 0xe0, 0xff),
            new(0xb3, 0x8a, 0xe6, 0xff), new(0xf4, 0xc2, 0x4a, 0xff),
        };

        [SerializeField] private Button _button;
        [SerializeField] private Image _wood;
        [SerializeField] private Image _art;
        [SerializeField] private RectTransform _artRect;
        [SerializeField] private TMP_Text _size;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GameObject _badge;
        [SerializeField] private Image _badgeIcon;
        [SerializeField] private GameObject _fresh;
        [SerializeField] private GameObject _picked;

        public event Action<string> Tapped;

        public string Id { get; private set; }

        public void Show(BagTileData tile)
        {
            CoachTarget.Tag(this, "bag-item:" + tile.Id);
            Id = tile.Id;
            _wood.color = TIERS[Mathf.Clamp(tile.Tier, 1, 5) - 1];
            _art.sprite = tile.Icon;
            // A picture of its own fills more of the tile: it carries its size in its art.
            var share = string.IsNullOrEmpty(tile.Size) ? 0.82f : 0.59f;
            _artRect.anchorMin = new Vector2(0.5f - share / 2, 0.5f - share / 2);
            _artRect.anchorMax = new Vector2(0.5f + share / 2, 0.5f + share / 2);
            _size.gameObject.SetActive(!string.IsNullOrEmpty(tile.Size));
            _size.text = tile.Size;
            _count.text = tile.Count;
            _badge.SetActive(tile.Badge != null);
            _badgeIcon.sprite = tile.Badge;
            _fresh.SetActive(tile.Fresh);
            _picked.SetActive(tile.Picked);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);
        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);
        private void OnTapped() => Tapped?.Invoke(Id);
    }
}
