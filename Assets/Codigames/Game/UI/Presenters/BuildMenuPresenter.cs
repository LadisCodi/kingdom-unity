using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Modules.Core;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Research;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The build menu's logic: the open tab's buildings as rows, what can be built first, then what cannot be
    // paid for yet, then what is capped; a tap on a row that can be built picks it, any other shakes it.
    public class BuildMenuPresenter : AbstractMenuPresenter<BuildMenu>, IClosableMenuPresenter
    {
        private const string FIRST_TAB = "Economy";
        private const string DECORATION = "Decoration";

        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly ITreasury _treasury;
        private readonly IBuildingCards _cards;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly TechProse _prose;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly CityState _city;
        private readonly IConstructionSettings _settings;
        private readonly Harmony _harmony;
        private readonly IHarmonySettings _harmonySettings;
        private readonly Kit.UiIcons _icons;
        private readonly PriceTerms _prices;

        // Remembered between openings, so placement's way back lands on the same tab.
        private string _tab = FIRST_TAB;

        public BuildMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, ITreasury treasury,
            IBuildingCards cards, NumberFormat numbers, Localizer localizer, ISoundService sounds,
            TechProse prose, ICatalog<IBuildingDefinition> buildings, CityState city, IConstructionSettings settings, Harmony harmony,
            IHarmonySettings harmonySettings, Kit.UiIcons icons, PriceTerms prices) : base(views)
        {
            _prices = prices;
            _harmony = harmony;
            _harmonySettings = harmonySettings;
            _icons = icons;
            _buildings = buildings;
            _city = city;
            _settings = settings;
            _prose = prose;
            _sounds = sounds;
            _ui = ui;
            _construction = construction;
            _treasury = treasury;
            _cards = cards;
            _numbers = numbers;
            _localizer = localizer;
        }

        // A building was picked to be placed: its id.
        public event Action<string> Picked;

        public void RequestClose() => _ = _ui.HideMenu<BuildMenu>();

        protected override void BindInternal(BuildMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
            _construction.JobStarted += OnJob;
            _construction.JobCompleted += OnJobCompleted;
        }

        protected override void UnbindInternal(BuildMenu view)
        {
            _treasury.Changed -= OnTreasuryChanged;
            _construction.JobStarted -= OnJob;
            _construction.JobCompleted -= OnJobCompleted;
        }

        protected override void SubscribeToViewEventsInternal(BuildMenu view)
        {
            view.CloseTapped += RequestClose;
            view.TabTapped += OnTab;
            view.RowTapped += OnRow;
        }

        protected override void UnsubscribeFromViewEventsInternal(BuildMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.TabTapped -= OnTab;
            view.RowTapped -= OnRow;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();
        private void OnJob(ConstructionJob job) => Refresh();
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh();

        private void OnTab(string tab)
        {
            if (tab == _tab) return;
            _tab = tab;
            Refresh();
            View.ScrollToTop();
        }

        private void OnRow(string id)
        {
            if (_construction.BuildRefusal(id) != ConstructionRefusal.None)
            {
                _sounds.Play(SoundIds.ERROR);
                View.Shake(id);
                return;
            }

            Picked?.Invoke(id);
        }

        private void Refresh()
        {
            View.SetOpenTab(_tab);

            var rows = _cards.Cards
                .Where(c => c.Buildable && c.BuildTab == _tab)
                .Select(c => (Card: c, Offer: _construction.Offer(c.Id)))
                .OrderBy(r => Rank(r.Offer.Refusal))
                .Select(r => Row(r.Card, r.Offer))
                .ToList();

            View.SetRows(rows);
            View.SetNote(_tab == DECORATION ? DecorationNote() : null);

            foreach (var tab in new[] { "Economy", "Military", DECORATION })
                View.SetTabBadge(tab, _cards.Cards.Count(c => c.Buildable && c.BuildTab == tab
                                                            && _construction.BuildRefusal(c.Id) == ConstructionRefusal.None));
        }

        // What can be built first, then what is waiting on money or a builder, then what is capped, then what a
        // technology has still to open.
        private static int Rank(ConstructionRefusal refusal) => refusal switch
        {
            ConstructionRefusal.None => 0,
            ConstructionRefusal.AtCap => 2,
            ConstructionRefusal.NeedsHarmony => 2,
            ConstructionRefusal.NeedsResearch => 3,
            _ => 1,
        };

        // Over the decorations: Harmony supplied of demanded and what a surplus pays, once anything demands it; before
        // that, what a decoration is for — once one is known.
        private BuildNoteData DecorationNote()
        {
            var demand = _harmony.Demand();
            if (demand > 0)
            {
                var supply = _harmony.Supply;
                var tier = _harmony.Tier;
                var first = _harmonySettings.SurplusTiers.FirstOrDefault();
                var note = tier is { } paying
                    ? _localizer.Tr("+{pct}% taxes", ("pct", _numbers.Exact(Math.Round(paying.Bonus * 100))))
                    : _localizer.Tr("{at}% pays +{pct}%", ("at", _numbers.Exact(Math.Round(first.At * 100))), ("pct", _numbers.Exact(Math.Round(first.Bonus * 100))));
                return new BuildNoteData
                {
                    Icon = _icons.Get("harmony"),
                    Text = "<b><size=123%>" + _numbers.Exact(supply) + "</size></b> " + _localizer.Tr("supplied of {n} demanded", ("n", _numbers.Exact(demand))),
                    Note = note,
                    Tone = supply < demand ? BuildNoteTone.Short : tier != null ? BuildNoteTone.Paying : BuildNoteTone.Plain,
                };
            }

            var known = _cards.Cards.Any(c => c.Buildable && c.BuildTab == DECORATION && _construction.BuildRefusal(c.Id) != ConstructionRefusal.NeedsResearch);
            return known
                ? new BuildNoteData { Icon = _icons.Get("Housing"), Text = _localizer.Tr("A house beside a decoration earns more Gold"), Strong = true }
                : null;
        }

        // The cap's words: the Townhall level that lets one more stand, or that the realm allows no more.
        private string CapReason(string id, int count)
        {
            var building = _buildings.Get(id);
            var level = CityQueries.TownhallLevel(_city, _settings);
            for (var next = level + 1; next <= building.Gates.MaxCountPerTownhallLevel.Count; next++)
                if (CityQueries.MaxCount(building, next) > count)
                    return _localizer.Tr("Needs Townhall level {n}", ("n", _numbers.Exact(next)));
            return _localizer.Tr("You have as many as the realm allows");
        }

        private BuildRowData Row(IBuildingCard card, BuildOffer offer)
        {
            // Not yet opened: what opens it, by name, in place of a promise and a price — there is nothing to pay yet.
            if (offer.Refusal == ConstructionRefusal.NeedsResearch)
            {
                return new BuildRowData(card.Id, _localizer.Capitalized(_localizer.Tr(card.DisplayName)), string.Empty,
                    string.Empty, card.ArtFor(1), Array.Empty<PriceTerm>(), string.Empty, string.Empty, false,
                    _localizer.Tr("Research {tech}", ("tech", _prose.Name(offer.RequiredTech))), known: false);
            }

            var price = _prices.Of(offer.Price, offer.Goods);

            var built = offer.Cap.HasValue
                ? _localizer.Tr("Built {count}/{max}", ("count", _numbers.Number(offer.Count)), ("max", _numbers.Number(offer.Cap.Value)))
                : _localizer.Tr("Built {n}", ("n", _numbers.Number(offer.Count)));

            var atCap = offer.Refusal == ConstructionRefusal.AtCap;
            var why = atCap ? CapReason(card.Id, offer.Count)
                : offer.Refusal == ConstructionRefusal.NeedsHarmony
                    ? _localizer.Tr("Needs {n} more Harmony", ("n", _numbers.Exact(_construction.HarmonyShort(_buildings.Get(card.Id), 1))))
                    : null;

            return new BuildRowData(card.Id, _localizer.Capitalized(_localizer.Tr(card.DisplayName)),
                offer.Numbered && !atCap ? "#" + _numbers.Number(offer.Ordinal) : string.Empty,
                _localizer.Tr(card.Promise), card.ArtFor(1), atCap ? Array.Empty<PriceTerm>() : price,
                _numbers.Duration(offer.Seconds), built, offer.Refusal == ConstructionRefusal.None, why);
        }
    }
}
