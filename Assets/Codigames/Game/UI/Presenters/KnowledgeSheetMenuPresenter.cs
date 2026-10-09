using System;
using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The Knowledge sheet: the bar, when the next point drips in, and buying it — one point for Gold (dearer
    // every point, for ever), one or ten for Gems (always the same).
    public class KnowledgeSheetMenuPresenter : AbstractMenuPresenter<KnowledgeSheetMenu>, IPopupMenuPresenter, ITickable
    {
        private static readonly (int Count, string Till)[] OFFERS =
        {
            (1, KnowledgeMarket.GOLD), (1, KnowledgeMarket.GEMS), (10, KnowledgeMarket.GEMS),
        };

        private readonly UIManager _ui;
        private readonly KnowledgeBar _bar;
        private readonly KnowledgeMarket _market;
        private readonly ITreasury _treasury;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;

        private double _shownSecond = -1;

        public KnowledgeSheetMenuPresenter(IMenuViewFactory views, UIManager ui, KnowledgeBar bar, KnowledgeMarket market,
            ITreasury treasury, IClock clock, NumberFormat numbers, Localizer localizer, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _bar = bar;
            _market = market;
            _treasury = treasury;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<KnowledgeSheetMenu>();

        public void Tick()
        {
            if (!IsShown) return;

            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second != _shownSecond) Refresh();
        }

        protected override void BindInternal(KnowledgeSheetMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
        }

        protected override void UnbindInternal(KnowledgeSheetMenu view) => _treasury.Changed -= OnTreasuryChanged;

        protected override void SubscribeToViewEventsInternal(KnowledgeSheetMenu view)
        {
            view.CloseTapped += RequestClose;
            view.OfferTapped += OnOffer;
        }

        protected override void UnsubscribeFromViewEventsInternal(KnowledgeSheetMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.OfferTapped -= OnOffer;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();

        private void OnOffer(int index)
        {
            var (count, till) = OFFERS[index];
            if (_market.Buy(count, till) == BuyResult.Bought)
                _sounds.Play(till == KnowledgeMarket.GEMS ? SoundIds.GEM_SPEND : SoundIds.UPGRADE_BOUGHT);
            else _sounds.Play(SoundIds.ERROR);
            Refresh();
        }

        private void Refresh()
        {
            var now = _clock.NowMs;
            _shownSecond = Math.Floor(now / 1000.0);

            var value = _bar.Amount;
            var cap = _bar.Cap;
            var next = _bar.NextUnitAt;
            var fullMs = _bar.FullAt(now) - now;
            var hint = value >= cap
                ? value > cap
                    ? _localizer.Tr("{n} past the bar — nothing is dripping", ("n", _numbers.Exact(value - cap)))
                    : _localizer.Tr("Full — nothing is dripping")
                : next == null ? "" : _localizer.Tr("Full in {time}", ("time", _numbers.Countdown(Math.Ceiling(fullMs / 1000))));
            var note = next == null ? "" : _localizer.Tr("+1 in {time}", ("time", _numbers.Countdown(Math.Ceiling((next.Value - now) / 1000))));
            View.Show(hint, (float)Math.Min(1, value / cap), $"{_numbers.Exact(value)} / {_numbers.Exact(cap)}", note);

            for (var i = 0; i < OFFERS.Length; i++)
            {
                var (count, till) = OFFERS[i];
                var price = _market.Price(count, till);
                View.ShowOffer(i, _numbers.Exact(count), _numbers.Exact(price), _treasury.Get(till) >= price);
            }
        }
    }
}
