using System.Collections.Generic;
using Codigames.Game.UI.Widgets;
using Codigames.Kingdom.Heroes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Reveal
{
    // A prize as a card (the web's .gr-card): a back and a face, flipped by its inner. The back is an aged sheet with an
    // ink drawing as ornate as the card is rare; the face a torn page in ink — or, a whole hero, the roster's own card.
    // Fragments carry their bar under the card, and a recruit the NEW seal pressed onto it. Built at one width and
    // scaled: the screen moves it, this only draws it.
    public class RevealCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform _inner;
        [SerializeField] private Image _glow;
        [SerializeField] private GameObject _back;
        [SerializeField] private Image _ink;
        [SerializeField] private GameObject _face;
        [SerializeField] private GameObject _page;
        [SerializeField] private GameObject _std;
        [SerializeField] private Image _wash;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private GameObject _countRow;
        [SerializeField] private Image _countIcon;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GameObject _hero;
        [SerializeField] private Image _heroFace;
        [SerializeField] private Image _heroArt;
        [SerializeField] private TMP_Text _heroName;
        [SerializeField] private GameObject _supplies;
        [SerializeField] private TMP_Text _suppliesName;
        [SerializeField] private RectTransform _supplyGrid;
        [SerializeField] private SupplyCell _supplyPrefab;
        [SerializeField] private GameObject _bag;
        [SerializeField] private TMP_Text _bagName;
        [SerializeField] private RectTransform _bagGrid;
        [SerializeField] private RevealRowView _rowPrefab;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private GameObject _stamp;
        [SerializeField] private Image _dim;
        [SerializeField] private List<Sprite> _inks = new();
        [SerializeField] private Sprite _faceCommon;
        [SerializeField] private Sprite _faceRare;
        [SerializeField] private Sprite _faceLegendary;
        [SerializeField] private Sprite _gold;
        [SerializeField] private Sprite _blue;

        private readonly List<RevealRowView> _rows = new();
        private readonly List<SupplyCell> _cells = new();

        public RevealCardData Data { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public RectTransform Inner => _inner;
        public Image Glow => _glow;
        public ProgressBar Bar => _bar;
        public GameObject Stamp => _stamp;
        public IReadOnlyList<RevealRowView> Rows => _rows;

        public void Show(RevealCardData card)
        {
            Data = card;
            _ink.sprite = _inks[Mathf.Clamp(card.Ink, 0, _inks.Count - 1)];
            _glow.gameObject.SetActive(false);
            _glow.color = card.Glow;

            // A whole hero is the roster's card, not a page.
            _page.SetActive(card.Kind != PrizeKind.Hero);
            _std.SetActive(card.Kind is PrizeKind.Currency or PrizeKind.Item or PrizeKind.Fragments or PrizeKind.RelicFragment);
            _hero.SetActive(card.Kind == PrizeKind.Hero);
            _supplies.SetActive(card.Kind == PrizeKind.Supplies);
            _bag.SetActive(card.Kind == PrizeKind.Bag);

            _wash.color = card.Wash;
            _icon.gameObject.SetActive(card.Icon != null);
            _icon.sprite = card.Icon;
            _art.gameObject.SetActive(card.Art != null && card.Kind == PrizeKind.Fragments);
            _art.sprite = card.Art;
            SetMissing(card.Missing);
            _name.text = card.Name;
            _countRow.SetActive(!string.IsNullOrEmpty(card.Count));
            _count.text = card.Count;
            _countIcon.gameObject.SetActive(card.CountIcon != null);
            _countIcon.sprite = card.CountIcon;

            _heroFace.sprite = card.Rarity switch
            {
                HeroRarity.Legendary => _faceLegendary,
                HeroRarity.Rare => _faceRare,
                _ => _faceCommon,
            };
            _heroArt.sprite = card.Kind == PrizeKind.Hero ? card.Art : null;
            _heroName.text = card.Name;

            _suppliesName.text = card.Name;
            for (var i = 0; i < card.Supplies.Count; i++)
            {
                if (i == _cells.Count) _cells.Add(Instantiate(_supplyPrefab, _supplyGrid));
                _cells[i].gameObject.SetActive(true);
                _cells[i].Show(card.Supplies[i].Icon, card.Supplies[i].Count);
            }

            for (var i = card.Supplies.Count; i < _cells.Count; i++) _cells[i].gameObject.SetActive(false);

            _bagName.text = card.Name;
            for (var i = 0; i < card.Rows.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _bagGrid));
                _rows[i].gameObject.SetActive(true);
                _rows[i].Show(card.Rows[i]);
            }

            for (var i = card.Rows.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            _bar.gameObject.SetActive(false);
            if (card.Bar != null) SetBar(card.Bar.From, card.Bar.FromText);
            _stamp.SetActive(false);
            SetDown(true);
            SetDim(0);
        }

        // The bag's two columns across whatever width the board gave it.
        public void FitBag()
        {
            if (!_bagGrid.TryGetComponent<GridLayoutGroup>(out var grid)) return;
            var padding = ((RectTransform)_bag.transform).GetComponent<VerticalLayoutGroup>().padding.horizontal;
            grid.cellSize = new Vector2((Rect.sizeDelta.x - padding - grid.spacing.x) / 2, grid.cellSize.y);
        }

        public void SetDown(bool down)
        {
            _back.SetActive(down);
            _face.SetActive(!down);
            // The bar rides under a face, never a back.
            _bar.gameObject.SetActive(!down && Data.Bar != null);
        }

        public void SetBar(float share, string text) => _bar.Set(share, text, Data.Bar.Gold ? _gold : _blue);

        public void SetMissing(bool missing) => _art.color = missing ? new Color(0, 0, 0, 0.5f) : Color.white;

        // Home but not the point any more: dimmed. 0 is lit.
        public void SetDim(float dim) => _dim.color = new Color(0, 0, 0, dim);
    }
}
