using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Research;
using Codigames.Game.UI.Data.Research;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Research;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using Vector2 = UnityEngine.Vector2;

namespace Codigames.Game.UI.Presenters
{
    // The research book: lays out the open book's page (cards, chapters, arrows), keeps every card to the
    // research and the purses, turns to another book on its ribbon, and opens a technology's sheet on a tap.
    // Every book opens centred on its earliest card the kingdom may research now; with none, on its last done.
    public class ResearchMenuPresenter : AbstractMenuPresenter<ResearchMenu>, IClosableMenuPresenter
    {
        private static readonly string[] ROMAN = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        private readonly UIManager _ui;
        private readonly Researching _research;
        private readonly ICatalog<ITechnology> _technologies;
        private readonly ITechnologyCards _cards;
        private readonly TechTreeAsset _tree;
        private readonly IBookshelf _shelf;
        private readonly ITreasury _treasury;
        private readonly FogOfWar _fog;
        private readonly TechProse _prose;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        private readonly Dictionary<string, (float Top, int Column)> _slots = new();
        private string _tome = Bookshelf.KINGDOM;
        private int _revealedAtLayout = -1;

        public ResearchMenuPresenter(IMenuViewFactory views, UIManager ui, Researching research, ICatalog<ITechnology> technologies,
            ITechnologyCards cards, TechTreeAsset tree, IBookshelf shelf, ITreasury treasury, FogOfWar fog, TechProse prose,
            NumberFormat numbers, Localizer localizer) : base(views)
        {
            _ui = ui;
            _research = research;
            _technologies = technologies;
            _cards = cards;
            _tree = tree;
            _shelf = shelf;
            _treasury = treasury;
            _fog = fog;
            _prose = prose;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void RequestClose() => _ = _ui.HideMenu<ResearchMenu>();

        protected override void BindInternal(ResearchMenu view)
        {
            if (!_shelf.IsOpen(_tome)) _tome = Bookshelf.KINGDOM;
            Layout();
            CentreOnFrontier();

            _research.Poured += OnPoured;
            _research.Researched += OnResearched;
            _treasury.Changed += OnTreasuryChanged;
            _fog.Changed += OnFogChanged;
        }

        protected override void UnbindInternal(ResearchMenu view)
        {
            _research.Poured -= OnPoured;
            _research.Researched -= OnResearched;
            _treasury.Changed -= OnTreasuryChanged;
            _fog.Changed -= OnFogChanged;
        }

        protected override void SubscribeToViewEventsInternal(ResearchMenu view)
        {
            view.CloseTapped += RequestClose;
            view.CardTapped += OnCard;
            view.BookmarkTapped += OnBookmark;
        }

        protected override void UnsubscribeFromViewEventsInternal(ResearchMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.CardTapped -= OnCard;
            view.BookmarkTapped -= OnBookmark;
        }

        private void OnPoured(string id, double amount) => ShowCards();
        private void OnResearched(string id) => ShowCards();
        private void OnTreasuryChanged(string currency, double amount) => ShowCards();

        // A cell revealed may open a chapter.
        private void OnFogChanged(IReadOnlyCollection<Vector2Int> cells)
        {
            if (_fog.RevealedCount != _revealedAtLayout) Layout();
        }

        private void OnCard(string id) => _ = _ui.ShowMenu<TechSheetMenu, string>(id);

        private void OnBookmark(string tome)
        {
            if (tome == _tome || !_shelf.IsOpen(tome)) return;

            _tome = tome;
            Layout();
            CentreOnFrontier();
        }

        // The whole page: rows, chapters, connectors; then the cards and the ribbons.
        private void Layout()
        {
            _revealedAtLayout = _fog.RevealedCount;
            var rows = TechPageLayout.Rows(_technologies.Items, _tome);
            var (tops, height) = TechPageLayout.RowTops(rows);

            _slots.Clear();
            var index = new Dictionary<string, int>();
            var chapters = new List<ChapterData>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].IsGate)
                {
                    chapters.Add(Chapter(rows[i].Era, tops[i]));
                    continue;
                }

                for (var column = 0; column < rows[i].Slots.Count; column++)
                {
                    var id = rows[i].Slots[column];
                    if (id == null) continue;

                    _slots[id] = (tops[i], column);
                    index[id] = i;
                }
            }

            View.SetTitle(_localizer.Tr("Chapter I"));
            View.SetPage(new ResearchPageData(_tome, height, chapters, Edges(rows, index)));
            ShowCards();
            ShowBookmarks();
        }

        private ChapterData Chapter(int era, float top)
        {
            var shortfall = _research.EraShortfall(_tome, era);
            var gate = shortfall > 0
                ? _localizer.Trn(shortfall, "Reveal {n} more cell", "Reveal {n} more cells", ("n", _numbers.Exact(shortfall)))
                : "";
            return new ChapterData(_localizer.Tr("Chapter {n}", ("n", era < ROMAN.Length ? ROMAN[era] : era.ToString())), gate, top);
        }

        private List<EdgePiece> Edges(IReadOnlyList<PageRow> rows, IReadOnlyDictionary<string, int> index)
        {
            var pieces = new List<EdgePiece>();
            var drawn = new HashSet<(EdgePieceKind, float, float, float, ElbowTurn)>();

            void Draw(IReadOnlyList<(float X, float Y)> points)
            {
                foreach (var piece in TechPageLayout.Pieces(points))
                {
                    if (drawn.Add((piece.Kind, piece.X, piece.Y, piece.Length, piece.Turn))) pieces.Add(piece);
                }
            }

            foreach (var (id, to) in _slots)
            {
                var tech = _technologies.Get(id);
                var drew = false;
                foreach (var required in tech.Requires)
                {
                    if (!_slots.TryGetValue(required, out var from)) continue;

                    drew = true;
                    var clear = ColumnClear(rows, index[required], index[id], from.Column);
                    Draw(TechPageLayout.EdgePath(from.Top, from.Column, to.Top, to.Column, clear));
                }

                // A requirement with no end on this page: a stub in the gutter above the card.
                if (!drew && tech.Requires.Count > 0)
                {
                    var x = TechPageLayout.ColumnCentre(to.Column);
                    Draw(new List<(float, float)> { (x, to.Top - TechPageLayout.ROW_GAP / 2), (x, to.Top) });
                }
            }

            return pieces;
        }

        // Is the source's column empty on every row between the two? Then its connector may run straight down it.
        private static bool ColumnClear(IReadOnlyList<PageRow> rows, int from, int to, int column)
        {
            for (var i = from + 1; i < to; i++)
            {
                if (!rows[i].IsGate && rows[i].Slots[column] != null) return false;
            }

            return true;
        }

        private void ShowCards()
        {
            var cards = new List<TechCardData>(_slots.Count);
            foreach (var (id, slot) in _slots)
            {
                var tech = _technologies.Get(id);
                var card = _cards.Card(id);
                var state = _research.StateOf(id);
                var need = tech.Knowledge;
                var poured = state == TechState.Done ? need : _research.PouredInto(id);
                cards.Add(new TechCardData(id, _prose.Name(id), card?.Icon, state,
                    $"{_numbers.Exact(poured)} / {_numbers.Exact(need)}", need <= 0 ? 1 : (float)(poured / need),
                    state == TechState.Progress && _research.IsFilled(id), _research.IsActionable(id), tech.Planned,
                    new Vector2(TechPageLayout.ColumnLeft(slot.Column), slot.Top)));
            }

            View.SetCards(cards);
        }

        private void ShowBookmarks()
        {
            var marks = _tree.Books.Where(b => _shelf.IsOpen(b.Id))
                .Select(b => new BookmarkData(b.Id, b.Emblem, b.Tint, b.Id == _tome))
                .ToList();
            View.SetBookmarks(marks);
        }

        // The earliest card the kingdom may research now, short of its price or not; with none, the last done.
        private void CentreOnFrontier()
        {
            var shown = _technologies.Items.Where(t => _slots.ContainsKey(t.Id)).Select(t => t.Id).ToList();
            var focus = shown.FirstOrDefault(id => _research.Refusal(id) == ResearchRefusal.None)
                        ?? shown.LastOrDefault(_research.IsComplete);
            if (focus == null) View.ScrollToTop();
            else View.CentreOn(focus);
        }
    }
}
