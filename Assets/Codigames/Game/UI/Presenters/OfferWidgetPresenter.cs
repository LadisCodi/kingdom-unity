using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Store;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Kingdom.Store;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The offers widget's logic (the web's offerWidget + Game.offerWidgets): every widget offer on sale, or bought with
    // tomorrow's part still to claim — ready first, then on sale, then waiting — one at a time, the next each lap of
    // its sign. Shown on the bare map only.
    public class OfferWidgetPresenter : AbstractMenuPresenter<OfferWidget>, ITickable
    {
        private enum State
        {
            Ready,
            Sale,
            Waiting,
        }

        private readonly UIManager _ui;
        private readonly Offers _offers;
        private readonly Kingdom.Store.Store _store;
        private readonly OfferTiles _tiles;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private int _turn;
        private string _lead;
        private string _key;

        public OfferWidgetPresenter(IMenuViewFactory views, UIManager ui, Offers offers, Kingdom.Store.Store store, OfferTiles tiles,
            NumberFormat numbers, Localizer localizer, IClock clock) : base(views)
        {
            _ui = ui;
            _offers = offers;
            _store = store;
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
            var list = Widgets(now);
            var shown = list.Count > 0 && !_ui.HasOverlayOpen;
            View.SetShown(shown);
            if (!shown) return;
            var (product, state, at) = list[_turn % list.Count];
            var key = product.Id + ":" + state;
            if (key != _key)
            {
                View.ShowOffer((product as ProductAsset)?.Icon, _tiles.Hero(product)?.Portrait, Kind(product), _key != null && list.Count > 1);
                _key = key;
                _lead = product.Id;
            }

            var left = Math.Max(0, Math.Ceiling((at - now) / 1000));
            View.SetWords(state switch
            {
                State.Ready => _localizer.Tr("Claim!"),
                State.Waiting => _localizer.Tr("Tomorrow in {time}", ("time", _numbers.Countdown(left))),
                _ => at > 0 ? _localizer.Tr(product.DisplayName) + " · " + _numbers.Countdown(left) : _localizer.Tr(product.DisplayName),
            });
        }

        private List<(IProductDefinition Product, State State, double At)> Widgets(double now)
        {
            var list = new List<(IProductDefinition, State, double)>();
            foreach (var product in _store.All.Where(p => p.Shelf == ProductShelf.Offer && p.Widget))
            {
                if (_offers.NextDayReady(now).Any(d => d.Sku == product.Id)) list.Add((product, State.Ready, now));
                else if (_offers.NextDayWaiting(now).FirstOrDefault(d => d.Sku == product.Id) is { } waiting) list.Add((product, State.Waiting, waiting.ClaimableAt));
                else if (_offers.OfferOn(product, now)) list.Add((product, State.Sale, _offers.Window(product.Id)?.Closes ?? 0));
            }

            return list.OrderBy(w => (int)w.Item2).ToList();
        }

        // A picture of what an offer is for, while it has no icon of its own (the web's kindIcon).
        private static string Kind(IProductDefinition product)
        {
            if (product.Explorers > 0) return "compass";
            if (product.HeroSlots > 0) return "helmet";
            return product.OpensOn switch
            {
                "manaLow" => "flask",
                "buildersBusy" or "townhall" => "speedup",
                _ => "chest",
            };
        }

        private void OnLapped() => _turn++;

        private void OnTapped()
        {
            if (_lead != null) _ = _ui.ShowMenu<OfferSplashMenu, OfferSplashOrder>(new OfferSplashOrder { Sku = _lead });
        }
    }
}
