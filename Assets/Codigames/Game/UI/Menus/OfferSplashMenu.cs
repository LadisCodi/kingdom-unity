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
        public OfferAsidePlaque Plaque;
        public string NowTitle;
        public IReadOnlyList<OfferTileData> Now = Array.Empty<OfferTileData>();
        public string Value;
        // More than four rewards with Gems among them: the Gems on a row of their own, the rest in smaller tiles.
        public string Gems;
        public IReadOnlyList<OfferGiftData> Gifts = Array.Empty<OfferGiftData>();
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

    // What stands under the name beside the figure: a hero's rarity on its cloth, or a pack's step on a brass plate.
    public enum OfferAsidePlaque
    {
        None,
        Common,
        Rare,
        Legendary,
        Chain,
    }

    // What an offer opens for good, on a parchment strip under a GIFT tag.
    public sealed class OfferGiftData
    {
        public Sprite Icon;
        public string Title;
        public string Text;
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
        [SerializeField] private RectTransform _gifts;
        [SerializeField] private OfferGiftView _giftPrefab;
        [SerializeField] private GameObject _gemsRow;
        [SerializeField] private TMP_Text _gemsCount;
        [SerializeField] private GameObject _tomorrow;
        [SerializeField] private TMP_Text _tomorrowTitle;
        [SerializeField] private GameObject _lock;
        [SerializeField] private RectTransform _tomorrowTiles;
        [SerializeField] private OfferTileView _tilePrefab;
        [SerializeField] private KitButton _action;
        [SerializeField] private GameObject _wait;
        [SerializeField] private TMP_Text _waitText;
        [SerializeField] private TMP_Text _note;
        [SerializeField] private GameObject _asidePlaque;
        [SerializeField] private Image _asidePlaqueImage;
        [SerializeField] private Sprite _rarityCommon;
        [SerializeField] private Sprite _rarityRare;
        [SerializeField] private Sprite _rarityLegendary;
        [SerializeField] private Sprite _chainPlate;
        [SerializeField] private RectTransform _tabs;
        [SerializeField] private OfferTab _tabPrefab;
        // The light and what stands in it: lowered, and the figure shortened, while the row of offers takes the top.
        [SerializeField] private RectTransform[] _lowered;
        [SerializeField] private RectTransform _figureBox;

        private double? _waitUntil;
        private double? _closesAt;
        private string _waitLabel;
        private string _noteText;
        private Vector2[] _raised;
        private float _figureHeight;
        // offer.css .ofs-screen.has-tabs .ofs-hero: 52 reference px lower, 60% of the column tall rather than 68%.
        private const float TABS_DROP = 52f * 2.8f;
        private const float TABS_HEIGHT = 60f / 68f;
        private const float U = 2.8f;
        private const float SMALL_TILE = 62f * U;
        private static readonly Color CLOTH_INK = new(1f, 0.965f, 0.878f);
        private static readonly Color GOLD_CLOTH_INK = new(0.353f, 0.208f, 0.063f);
        private static readonly Color CHAIN_INK = new(0.29f, 0.173f, 0.047f);

        public event Action CloseTapped;
        public event Action ActionTapped;
        public event Action<int> TabTapped;

        protected override void InitializeInternal()
        {
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            _action.onClick.AddListener(() => ActionTapped?.Invoke());
            _raised = Array.ConvertAll(_lowered, r => r.anchoredPosition);
            _figureHeight = _figureBox.sizeDelta.y;
        }

        // Opened from the map's widget: every offer in a row along the top. Fewer than two, no row.
        public void ShowTabs(IReadOnlyList<OfferTabData> tabs)
        {
            Clear(_tabs);

            var on = tabs.Count > 1;
            _tabs.gameObject.SetActive(on);
            for (var i = 0; i < _lowered.Length; i++) _lowered[i].anchoredPosition = _raised[i] - new Vector2(0f, on ? TABS_DROP : 0f);
            _figureBox.sizeDelta = new Vector2(_figureBox.sizeDelta.x, _figureHeight * (on ? TABS_HEIGHT : 1f));
            if (!on) return;
            for (var i = 0; i < tabs.Count; i++)
            {
                var index = i;
                var tab = Instantiate(_tabPrefab, _tabs);
                tab.Show(tabs[i]);
                tab.Tapped += () => TabTapped?.Invoke(index);
            }
        }

        public void Show(OfferSplashData data)
        {
            _title.text = data.Title;
            _figure.sprite = data.Figure;
            _figure.enabled = data.Figure != null;
            _aside.text = data.Aside;
            _asidePlaque.SetActive(data.Plaque != OfferAsidePlaque.None && !string.IsNullOrEmpty(data.AsideNote));
            Plaque(data.Plaque);
            _asideNote.text = data.AsideNote ?? string.Empty;
            _nowTitle.text = data.NowTitle;
            Fill(_nowTiles, data.Now, data.Gems != null);
            _gemsRow.SetActive(data.Gems != null);
            _gemsCount.text = data.Gems ?? string.Empty;
            _value.gameObject.SetActive(!string.IsNullOrEmpty(data.Value));
            _value.text = data.Value ?? string.Empty;
            _gifts.gameObject.SetActive(data.Gifts.Count > 0);
            Clear(_gifts);
            foreach (var gift in data.Gifts) Instantiate(_giftPrefab, _gifts).Show(gift);
            _tomorrow.SetActive(data.Tomorrow.Count > 0);
            _tomorrowTitle.text = data.TomorrowTitle;
            _lock.SetActive(data.TomorrowLocked);
            Fill(_tomorrowTiles, data.Tomorrow, false);
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

        private void Fill(RectTransform row, IReadOnlyList<OfferTileData> tiles, bool small)
        {
            Clear(row);
            foreach (var tile in tiles)
            {
                var view = Instantiate(_tilePrefab, row);
                view.Show(tile);
                if (small) view.Shrink(SMALL_TILE);
            }

            row.GetComponent<HorizontalLayoutGroup>().spacing = (small ? 4f : 6f) * U;
        }

        // Out of the layout now: a destroyed child lingers to the frame's end.
        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                parent.GetChild(i).gameObject.SetActive(false);
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        // offer.css .ofs-rarity (176 × 52, clear of the curl and the tail) and .ofs-chain (86 wide at least, 26 tall).
        private void Plaque(OfferAsidePlaque plaque)
        {
            if (plaque == OfferAsidePlaque.None) return;
            var chain = plaque == OfferAsidePlaque.Chain;
            _asidePlaqueImage.sprite = plaque switch
            {
                OfferAsidePlaque.Common => _rarityCommon,
                OfferAsidePlaque.Rare => _rarityRare,
                OfferAsidePlaque.Legendary => _rarityLegendary,
                _ => _chainPlate,
            };
            _asideNote.color = plaque switch
            {
                OfferAsidePlaque.Legendary => GOLD_CLOTH_INK,
                OfferAsidePlaque.Chain => CHAIN_INK,
                _ => CLOTH_INK,
            };
            var element = _asidePlaque.GetComponent<LayoutElement>();
            element.minWidth = (chain ? 86f : 176f) * U;
            element.preferredWidth = chain ? -1f : 176f * U;
            element.minHeight = element.preferredHeight = (chain ? 26f : 52f) * U;
            var layout = _asidePlaque.GetComponent<HorizontalLayoutGroup>();
            layout.padding = chain
                ? new RectOffset((int)(12 * U), (int)(12 * U), 0, 0)
                : new RectOffset((int)(36 * U), (int)(30 * U), 0, (int)(4 * U));
        }
    }
}
