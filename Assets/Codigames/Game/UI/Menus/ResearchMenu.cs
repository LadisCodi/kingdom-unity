using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data.Research;
using Codigames.Game.UI.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The research book: one page of parchment on a small stack, pinned at its top corners, read top to bottom —
    // three columns of cards, a chapter heading wherever the next band begins, quill-drawn arrows between them —
    // with a ribbon per open book hanging under it. The page is laid out in page pixels and scaled to its width.
    // View only: the ResearchMenuPresenter fills it.
    public class ResearchMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private LayoutElement _flowHost;
        [SerializeField] private RectTransform _flow;
        [SerializeField] private RectTransform _edges;
        [SerializeField] private RectTransform _chapters;
        [SerializeField] private RectTransform _cards;
        [SerializeField] private TechCardView _cardPrefab;
        [SerializeField] private ChapterBarView _chapterPrefab;
        [SerializeField] private RectTransform _bookmarks;
        [SerializeField] private BookmarkView _bookmarkPrefab;

        [Header("The quill's ink")]
        [SerializeField] private Sprite _edgeVertical;
        [SerializeField] private Sprite _edgeHorizontal;
        [SerializeField] private Sprite _edgeElbow;
        [SerializeField] private Sprite _edgeHead;
        [SerializeField] private Color _ink = Color.white;

        private readonly List<TechCardView> _shownCards = new();
        private readonly List<ChapterBarView> _shownChapters = new();
        private readonly List<Image> _shownEdges = new();
        private readonly List<BookmarkView> _shownMarks = new();
        private float _height;

        public event Action CloseTapped;

        // A card was tapped: its technology.
        public event Action<string> CardTapped;

        // A ribbon was tapped: its book.
        public event Action<string> BookmarkTapped;

        public void SetTitle(string title) => _title.text = title;

        public void SetPage(ResearchPageData page)
        {
            _height = page.Height;
            Fit();

            Fill(_shownChapters, page.Chapters.Count, () => Instantiate(_chapterPrefab, _chapters), (view, i) => view.Show(page.Chapters[i]));
            Fill(_shownEdges, page.Edges.Count, NewEdge, (edge, i) => Place(edge, page.Edges[i]));
        }

        public void SetCards(IReadOnlyList<TechCardData> cards)
            => Fill(_shownCards, cards.Count, NewCard, (view, i) => view.Show(cards[i]));

        public void SetBookmarks(IReadOnlyList<BookmarkData> marks)
            => Fill(_shownMarks, marks.Count, NewBookmark, (view, i) => view.Show(marks[i]));

        // Brings a card to the middle of the page.
        public void CentreOn(string id)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var card in _shownCards)
            {
                if (!card.gameObject.activeSelf || card.Id != id) continue;

                var content = _scroll.content;
                var viewport = _scroll.viewport.rect.height;
                var rect = (RectTransform)card.transform;
                var centre = rect.TransformPoint(rect.rect.center);
                var cardY = content.rect.yMax - content.InverseTransformPoint(centre).y;
                var range = content.rect.height - viewport;
                if (range <= 0) return;

                _scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((cardY - viewport / 2) / range);
                return;
            }
        }

        public void ScrollToTop() => _scroll.verticalNormalizedPosition = 1;

        protected override void SubscribeToEventsInternal() => _close.onClick.AddListener(OnClose);

        protected override void UnsubscribeFromEventsInternal() => _close.onClick.RemoveListener(OnClose);

        private void OnClose() => CloseTapped?.Invoke();

        private float _fittedWidth;

        // The paper's width is known only once the layout has run: the page follows it.
        private void LateUpdate()
        {
            var width = ((RectTransform)_flowHost.transform).rect.width;
            if (!Mathf.Approximately(width, _fittedWidth)) Fit();
        }

        // The page, scaled so its three columns fill the paper's width.
        private void Fit()
        {
            if (_flowHost == null) return;

            var width = ((RectTransform)_flowHost.transform).rect.width;
            if (width <= 0) return;

            _fittedWidth = width;
            var scale = width / TechPageLayout.PageWidth;
            _flow.localScale = new Vector3(scale, scale, 1);
            _flow.sizeDelta = new Vector2(TechPageLayout.PageWidth, _height);
            _flowHost.preferredHeight = _height * scale + 60 * scale;
        }

        private TechCardView NewCard()
        {
            var card = Instantiate(_cardPrefab, _cards);
            WireClicks(card.gameObject);
            card.Tapped += id => CardTapped?.Invoke(id);
            return card;
        }

        private BookmarkView NewBookmark()
        {
            var mark = Instantiate(_bookmarkPrefab, _bookmarks);
            WireClicks(mark.gameObject);
            mark.Tapped += tome => BookmarkTapped?.Invoke(tome);
            return mark;
        }

        private Image NewEdge()
        {
            var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            edge.transform.SetParent(_edges, false);
            edge.raycastTarget = false;
            edge.color = _ink;
            var rect = edge.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            return edge;
        }

        // Where a piece of ink sits, in page pixels from the top-left; the runs repeat their stroke along their length.
        private void Place(Image edge, EdgePiece piece)
        {
            const float seam = 0.5f;
            var band = TechPageLayout.EDGE_BAND / 2;
            var rect = edge.rectTransform;
            rect.localRotation = Quaternion.identity;
            switch (piece.Kind)
            {
                case EdgePieceKind.Vertical:
                    edge.sprite = _edgeVertical;
                    edge.type = Image.Type.Tiled;
                    edge.pixelsPerUnitMultiplier = _edgeVertical.rect.width / TechPageLayout.EDGE_BAND;
                    rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(piece.X - band, -(piece.Y - seam));
                    rect.sizeDelta = new Vector2(TechPageLayout.EDGE_BAND, piece.Length + 2 * seam);
                    break;
                case EdgePieceKind.Horizontal:
                    edge.sprite = _edgeHorizontal;
                    edge.type = Image.Type.Tiled;
                    edge.pixelsPerUnitMultiplier = _edgeHorizontal.rect.height / TechPageLayout.EDGE_BAND;
                    rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(piece.X - seam, -(piece.Y - band));
                    rect.sizeDelta = new Vector2(piece.Length + 2 * seam, TechPageLayout.EDGE_BAND);
                    break;
                case EdgePieceKind.Elbow:
                    var size = TechPageLayout.ELBOW_R + band;
                    edge.sprite = _edgeElbow;
                    edge.type = Image.Type.Simple;
                    rect.pivot = new Vector2(band / size, band / size);
                    rect.anchoredPosition = new Vector2(piece.X, -piece.Y);
                    rect.sizeDelta = new Vector2(size, size);
                    rect.localRotation = Quaternion.Euler(0, 0, -90 * (int)piece.Turn);
                    break;
                default:
                    edge.sprite = _edgeHead;
                    edge.type = Image.Type.Simple;
                    rect.pivot = new Vector2(0.5f, 0);
                    rect.anchoredPosition = new Vector2(piece.X, -piece.Y);
                    rect.sizeDelta = new Vector2(12, 6.33f);
                    break;
            }
        }

        private static void Fill<T>(List<T> shown, int count, Func<T> create, Action<T, int> show) where T : Component
        {
            for (var i = 0; i < count; i++)
            {
                if (i == shown.Count) shown.Add(create());
                shown[i].gameObject.SetActive(true);
                show(shown[i], i);
            }

            for (var i = count; i < shown.Count; i++) shown[i].gameObject.SetActive(false);
        }
    }
}
