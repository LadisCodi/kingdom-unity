using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Research;
using Codigames.Modules.Feedback;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Data.Research;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Research;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // A technology's sheet: what it is, and the three ways to fill it — every missing point bought with Gems,
    // one from the bar, as much as the bar allows — then Research, which pays its price and completes it on the
    // press. Nothing announces it: the press is the news (but the last card of a chapter says what it paid).
    public class TechSheetMenuPresenter : AbstractDataMenuPresenter<TechSheetMenu, string>, IPopupMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly Researching _research;
        private readonly KnowledgeBar _bar;
        private readonly KnowledgeMarket _market;
        private readonly ICatalog<ITechnology> _technologies;
        private readonly ITechnologyCards _cards;
        private readonly ITreasury _treasury;
        private readonly ICurrencyIcons _icons;
        private readonly TechProse _prose;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;

        public TechSheetMenuPresenter(IMenuViewFactory views, UIManager ui, Researching research, KnowledgeBar bar,
            KnowledgeMarket market, ICatalog<ITechnology> technologies, ITechnologyCards cards, ITreasury treasury,
            ICurrencyIcons icons, TechProse prose, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds,
            IQuickInfoMessageService messages) : base(views)
        {
            _ui = ui;
            _research = research;
            _bar = bar;
            _market = market;
            _technologies = technologies;
            _cards = cards;
            _treasury = treasury;
            _icons = icons;
            _prose = prose;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
        }

        private ITechnology Tech => _technologies.Get(Data);

        // What the "as much as it can" button pours: the least of what the bar holds and what is missing.
        private int Most => (int)Math.Min(_research.Missing(Data), Math.Floor(_bar.Amount));

        public void RequestClose() => _ = _ui.HideMenu<TechSheetMenu>();

        protected override void BindInternal(TechSheetMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
            _research.Poured += OnPoured;
            _research.EraFinished += OnEraFinished;
        }

        protected override void UnbindInternal(TechSheetMenu view)
        {
            _treasury.Changed -= OnTreasuryChanged;
            _research.Poured -= OnPoured;
            _research.EraFinished -= OnEraFinished;
        }

        protected override void SubscribeToViewEventsInternal(TechSheetMenu view)
        {
            view.CloseTapped += RequestClose;
            view.BuyWithGemsTapped += OnBuyWithGems;
            view.PourOneTapped += OnPourOne;
            view.PourMostTapped += OnPourMost;
            view.ResearchTapped += OnResearch;
        }

        protected override void UnsubscribeFromViewEventsInternal(TechSheetMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.BuyWithGemsTapped -= OnBuyWithGems;
            view.PourOneTapped -= OnPourOne;
            view.PourMostTapped -= OnPourMost;
            view.ResearchTapped -= OnResearch;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();
        private void OnPoured(string id, double amount) => Refresh();

        private void OnEraFinished(string tome, int era, int fragments)
            => _messages.Show(new QuickInfoMessageData(_localizer.Trn(fragments, "Chapter {era} complete — {n} relic fragment",
                "Chapter {era} complete — {n} relic fragments", ("era", _numbers.Exact(era)), ("n", _numbers.Exact(fragments)))));

        private void OnBuyWithGems()
        {
            var missing = (int)Math.Ceiling(_research.Missing(Data));
            if (missing <= 0) return;

            if (_market.Buy(missing, KnowledgeMarket.GEMS) != BuyResult.Bought)
            {
                _sounds.Play(SoundIds.ERROR);
                return;
            }

            _sounds.Play(SoundIds.GEM_SPEND);
            Pour(missing);
        }

        private void OnPourOne() => Pour(1);

        private void OnPourMost() => Pour(int.MaxValue);

        private void Pour(int max)
        {
            var result = _research.Pour(Data, _clock.NowMs, max);
            if (result == PourResult.Poured) _sounds.Play(SoundIds.RESEARCH);
            else if (result == PourResult.NothingHeld) _sounds.Play(SoundIds.ERROR);
            else if (result == PourResult.Refused) Refuse();
            Refresh();
        }

        private void OnResearch()
        {
            var result = _research.Complete(Data, _clock.NowMs);
            if (result == ResearchResult.Researched)
            {
                _sounds.Play(SoundIds.RESEARCH_COMPLETE);
                RequestClose();
                return;
            }

            if (result == ResearchResult.Refused) Refuse();
            else _sounds.Play(SoundIds.ERROR);
            Refresh();
        }

        private void Refuse()
        {
            _sounds.Play(SoundIds.ERROR);
            var refusal = _research.Refusal(Data);
            if (refusal == ResearchRefusal.MissingRequirement)
                _messages.Show(new QuickInfoMessageData(_localizer.Tr("Requires another technology first")));
            else if (refusal == ResearchRefusal.EraLocked)
                _messages.Show(new QuickInfoMessageData(_localizer.Tr("Reveal {n} more cells to read on",
                    ("n", _numbers.Exact(_research.EraShortfall(Tech.Tome, Tech.Era))))));
        }

        private void Refresh()
        {
            var tech = Tech;
            var sheet = new TechSheetData
            {
                Id = Data,
                Name = _prose.Name(Data),
                Icon = _cards.Card(Data)?.Icon,
                Says = _prose.Line(tech),
                Planned = tech.Planned,
                State = _research.StateOf(Data),
            };

            if (sheet.State == TechState.Locked) sheet.Requirements = Requirements(tech);
            if (sheet.State == TechState.Progress) FillProgress(sheet, tech);
            View.Show(sheet);
        }

        private List<RequirementData> Requirements(ITechnology tech)
        {
            var rows = tech.Requires
                .Select(id => new RequirementData(RequirementKind.Technology, _localizer.Tr("Research {tech}", ("tech", _prose.Name(id))),
                    _research.IsComplete(id)))
                .ToList();
            var shortfall = _research.EraShortfall(tech.Tome, tech.Era);
            if (shortfall > 0)
            {
                rows.Add(new RequirementData(RequirementKind.Cells,
                    _localizer.Trn(shortfall, "Reveal {n} more cell", "Reveal {n} more cells", ("n", _numbers.Exact(shortfall))), false));
            }

            return rows;
        }

        private void FillProgress(TechSheetData sheet, ITechnology tech)
        {
            var need = tech.Knowledge;
            var poured = _research.PouredInto(Data);
            var missing = (int)Math.Ceiling(_research.Missing(Data));
            var gems = _market.GemPrice(missing);

            sheet.NeedsKnowledge = need > 0;
            sheet.Fraction = need <= 0 ? 1 : (float)(poured / need);
            sheet.Bar = $"{_numbers.Exact(poured)} / {_numbers.Exact(need)}";
            sheet.Filled = missing == 0;
            sheet.GemsPrice = _numbers.Exact(gems);
            sheet.CanBuyWithGems = _treasury.Get(KnowledgeMarket.GEMS) >= gems;
            sheet.CanPour = Most > 0;
            sheet.PourMost = "+" + _numbers.Exact(Most);
            sheet.Price = tech.Price
                .Where(line => line.Value > 0)
                .OrderBy(line => line.Key == KnowledgeMarket.GOLD ? 0 : 1)
                .Select(line => new CostChipData(_icons.IconOf(line.Key), _numbers.Exact(line.Value), _treasury.Get(line.Key) < line.Value))
                .ToList();
            sheet.CanResearch = sheet.Filled && _research.CanAfford(Data);
            sheet.Note = sheet.Filled ? "" : _localizer.Tr("Assign all its Knowledge to research it");
        }
    }
}
