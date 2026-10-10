using System;
using System.Collections.Generic;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Store;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // What an offer's splash shows.
    public sealed class OfferSplashData
    {
        public string Title;
        public Sprite Figure;
        // A hero's name and rarity, or the pack's own words and its step in a chain.
        public string Aside;
        public string AsideNote;
        public string NowTitle;
        public IReadOnlyList<OfferTileData> Now = Array.Empty<OfferTileData>();
        public string Value;
        public IReadOnlyList<string> Gifts = Array.Empty<string>();
        public string TomorrowTitle;
        public IReadOnlyList<OfferTileData> Tomorrow = Array.Empty<OfferTileData>();
        public bool TomorrowLocked;
        // The action: a price, Claim, or nothing (waiting: Wait counts down).
        public string Action;
        public double? WaitUntil;
        public string WaitLabel;
        public double? ClosesAt;
        public string Note;
    }

    // AN OFFER'S SPLASH (the web's offerSplash, Docs/features/14-monetization.md §2.6): the whole screen for one
    // offer — its figure in a burst of light, its name on a ribbon, its hero's name and rarity or its own words, what
    // lands now, what lands tomorrow, the slots it opens for good, and its price — or, bought, Claim when tomorrow has
    // come, or the time until it does. View only: the OfferSplashMenuPresenter fills it.
    public class OfferSplashMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Image _burst;
        [SerializeField] private Image _figure;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _aside;
        [SerializeField] private TMP_Text _asideNote;
        [SerializeField] private TMP_Text _nowTitle;
        [SerializeField] private RectTransform _nowTiles;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private TMP_Text _gifts;
        [SerializeField] private GameObject _tomorrow;
        [SerializeField] private TMP_Text _tomorrowTitle;
        [SerializeField] private GameObject _lock;
        [SerializeField] private RectTransform _tomorrowTiles;
        [SerializeField] private OfferTileView _tilePrefab;
        [SerializeField] private KitButton _action;
        [SerializeField] private GameObject _wait;
        [SerializeField] private TMP_Text _waitText;
        [SerializeField] private TMP_Text _note;

        private double? _waitUntil;
        private double? _closesAt;
        private string _waitLabel;
        private string _noteText;

        public event Action CloseTapped;
        public event Action ActionTapped;

        protected override void InitializeInternal()
        {
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            _action.onClick.AddListener(() => ActionTapped?.Invoke());
        }

        public void Show(OfferSplashData data)
        {
            _title.text = data.Title;
            _figure.sprite = data.Figure;
            _figure.enabled = data.Figure != null;
            _aside.text = data.Aside;
            _asideNote.gameObject.SetActive(!string.IsNullOrEmpty(data.AsideNote));
            _asideNote.text = data.AsideNote ?? string.Empty;
            _nowTitle.text = data.NowTitle;
            Fill(_nowTiles, data.Now);
            _value.gameObject.SetActive(!string.IsNullOrEmpty(data.Value));
            _value.text = data.Value ?? string.Empty;
            _gifts.gameObject.SetActive(data.Gifts.Count > 0);
            _gifts.text = string.Join("\n", data.Gifts);
            _tomorrow.SetActive(data.Tomorrow.Count > 0);
            _tomorrowTitle.text = data.TomorrowTitle;
            _lock.SetActive(data.TomorrowLocked);
            Fill(_tomorrowTiles, data.Tomorrow);
            _action.gameObject.SetActive(!string.IsNullOrEmpty(data.Action));
            _action.Label = data.Action ?? string.Empty;
            _wait.SetActive(data.WaitUntil != null);
            _waitUntil = data.WaitUntil;
            _waitLabel = data.WaitLabel;
            _closesAt = data.ClosesAt;
            _noteText = data.Note;
            _note.gameObject.SetActive(_closesAt != null || !string.IsNullOrEmpty(_noteText));
        }

        // The countdowns, written in place.
        public void Tick(double now, Func<double, string> format)
        {
            if (_waitUntil != null) _waitText.text = "<sprite name=\"hourglass\"> " + _waitLabel + " " + format(Remaining(_waitUntil.Value, now));
            var parts = new List<string>();
            if (_closesAt != null) parts.Add("<sprite name=\"hourglass\"> " + format(Remaining(_closesAt.Value, now)));
            if (!string.IsNullOrEmpty(_noteText)) parts.Add(_noteText);
            _note.text = string.Join("   ", parts);
        }

        private static double Remaining(double until, double now) => Math.Max(0, Math.Ceiling((until - now) / 1000));

        private void Fill(RectTransform row, IReadOnlyList<OfferTileData> tiles)
        {
            for (var i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
            foreach (var tile in tiles) Instantiate(_tilePrefab, row).Show(tile);
        }
    }
}
