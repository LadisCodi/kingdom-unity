using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // A landmark's card: its name, the site on a tile with whether it is yours, what claiming it gives — a bigger
    // Mana pool and the map lit round it — and Claim with its price, or, once claimed, what it holds now. View only.
    public class LandmarkCardMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Button _scrim;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _art;
        [SerializeField] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private TMP_Text _head;
        [SerializeField] private TMP_Text _mana;
        [SerializeField] private TMP_Text _uncovered;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private GameObject _claimHost;
        [SerializeField] private Button _claim;
        [SerializeField] private TMP_Text _price;

        public event Action CloseTapped;
        public event Action ClaimTapped;

        public void Show(string title, Sprite art, string status, string head, string mana, string uncovered, string note, bool claimable,
            string price, bool affordable)
        {
            _title.text = title;
            _art.sprite = art;
            if (art != null) _artFit.aspectRatio = art.rect.width / art.rect.height;
            _status.text = status;
            _head.text = head;
            _mana.text = mana;
            _uncovered.text = uncovered;
            _note.text = note;
            _claimHost.SetActive(claimable);
            _price.text = price;
            _price.color = affordable ? Color.white : new Color32(0xff, 0xb0, 0xa0, 0xff);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _scrim.onClick.AddListener(OnClose);
            _claim.onClick.AddListener(OnClaim);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _scrim.onClick.RemoveListener(OnClose);
            _claim.onClick.RemoveListener(OnClaim);
        }

        private void OnClose() => CloseTapped?.Invoke();

        private void OnClaim() => ClaimTapped?.Invoke();
    }
}
