using System;
using Codigames.Game.Data.Store;
using Codigames.Game.Store;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The offers widget's logic (the web's offerWidget + Game.offerWidgets): every widget offer on sale, or bought with
    // tomorrow's part still to claim — ready first, then on sale, then waiting — one at a time, the next each lap of
    // its sign. Shown on the bare map only.
    public class OfferWidgetPresenter : AbstractMenuPresenter<OfferWidget>, ITickable
    {
        private readonly UIManager _ui;
        private readonly OfferWidgets _widgets;
        private readonly OfferTiles _tiles;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private int _turn;
        private string _lead;
        private string _key;

        public OfferWidgetPresenter(IMenuViewFactory views, UIManager ui, OfferWidgets widgets, OfferTiles tiles, NumberFormat numbers,
            Localizer localizer, IClock clock) : base(views)
        {
            _ui = ui;
            _widgets = widgets;
            _tiles = tiles;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
        }

        protected override void SubscribeToViewEventsInternal(OfferWidget view)
        {
            view.Tapped += OnTapped;
            view.Lapped += OnLapped;
        }

        protected override void UnsubscribeFromViewEventsInternal(OfferWidget view)
        {
            view.Tapped -= OnTapped;
            view.Lapped -= OnLapped;
        }

        public void Tick()
        {
            if (View == null || !IsShown) return;
            var now = _clock.NowMs;
            var list = _widgets.At(now);
            var shown = list.Count > 0 && !_ui.HasOverlayOpen;
            View.SetShown(shown);
            if (!shown) return;
            var offer = list[_turn % list.Count];
            var product = offer.Product;
            var key = product.Id + ":" + offer.State;
            if (key != _key)
            {
                View.ShowOffer((product as ProductAsset)?.Icon, _tiles.Hero(product)?.Portrait, OfferWidgets.Kind(product), _key != null && list.Count > 1);
                _key = key;
                _lead = product.Id;
            }

            var left = Math.Max(0, Math.Ceiling((offer.At - now) / 1000));
            View.SetWords(offer.State switch
            {
                OfferWidgetState.Ready => _localizer.Tr("Claim!"),
                OfferWidgetState.Waiting => _localizer.Tr("Tomorrow in {time}", ("time", _numbers.Countdown(left))),
                _ => offer.At > 0 ? _localizer.Tr(product.DisplayName) + " · " + _numbers.Countdown(left) : _localizer.Tr(product.DisplayName),
            });
        }

        private void OnLapped() => _turn++;

        private void OnTapped()
        {
            if (_lead != null) _ = _ui.ShowMenu<OfferSplashMenu, OfferSplashOrder>(new OfferSplashOrder { Sku = _lead, Browse = true });
        }
    }
}
