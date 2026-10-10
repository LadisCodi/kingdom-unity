using System.Collections.Generic;
using System.Linq;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Unlocks;
using Codigames.Kingdom.Store;
using Codigames.Modules.Clock;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.Store
{
    // When an offer's splash raises itself (the web's Game.offerSplash): on the bare map, the payer asked — first a
    // next-day part ready to claim; else a splash offer on sale whose window opened before this session began (it is the
    // next session that shows it), once a session. What the session closed stays closed until the next one.
    public class OfferSplashGate : ITickable
    {
        private readonly Offers _offers;
        private readonly Kingdom.Store.Store _store;
        private readonly UIManager _ui;
        private readonly UnlockSplashPresenter _unlocks;
        private readonly PayerGate _payer;
        private readonly IClock _clock;
        private readonly double _sessionStartedAt;
        private readonly HashSet<string> _closed = new();
        private double _second = -1;

        public OfferSplashGate(Offers offers, Kingdom.Store.Store store, UIManager ui, UnlockSplashPresenter unlocks, PayerGate payer, IClock clock)
        {
            _offers = offers;
            _store = store;
            _ui = ui;
            _unlocks = unlocks;
            _payer = payer;
            _clock = clock;
            _sessionStartedAt = clock.NowMs;
        }

        public void Closed(string sku, bool claim) => _closed.Add((claim ? "claim:" : "buy:") + sku);

        public void Tick()
        {
            var second = System.Math.Floor(_clock.NowMs / 1000.0);
            if (second == _second) return;
            _second = second;
            if (_payer.Due || _ui.HasOverlayOpen || _unlocks.IsPending || _ui.IsShown<OfferSplashMenu>()) return;
            var now = _clock.NowMs;
            var ready = _offers.NextDayReady(now).Select(d => d.Sku).FirstOrDefault(s => !_closed.Contains("claim:" + s));
            if (ready != null)
            {
                _ = _ui.ShowMenu<OfferSplashMenu, OfferSplashOrder>(new OfferSplashOrder { Sku = ready, Auto = true });
                return;
            }

            var offer = _offers.OffersOn(now).FirstOrDefault(p => p.Splash && _offers.Window(p.Id).Opened < _sessionStartedAt && !_closed.Contains("buy:" + p.Id));
            if (offer != null) _ = _ui.ShowMenu<OfferSplashMenu, OfferSplashOrder>(new OfferSplashOrder { Sku = offer.Id, Auto = true });
        }
    }
}
