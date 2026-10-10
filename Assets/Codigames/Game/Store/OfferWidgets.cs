using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Store;

namespace Codigames.Game.Store
{
    public enum OfferWidgetState
    {
        Ready,
        Sale,
        Waiting,
    }

    // One offer the map's widget turns through: what it is, where it stands, and the instant its words count to — its
    // window's close on sale, tomorrow's part waiting.
    public readonly struct WidgetOffer
    {
        public readonly IProductDefinition Product;
        public readonly OfferWidgetState State;
        public readonly double At;

        public WidgetOffer(IProductDefinition product, OfferWidgetState state, double at)
        {
            Product = product;
            State = state;
            At = at;
        }
    }

    // THE OFFERS ON THE MAP (the web's Game.offerWidgets): every widget offer on sale, or bought with tomorrow's part
    // still to claim — ready first, then on sale, then waiting. The widget shows them one at a time; a splash opened from
    // it shows them all in a row along its top.
    public class OfferWidgets
    {
        private readonly Kingdom.Store.Store _store;
        private readonly Offers _offers;

        public OfferWidgets(Kingdom.Store.Store store, Offers offers)
        {
            _store = store;
            _offers = offers;
        }

        public List<WidgetOffer> At(double now)
        {
            var ready = _offers.NextDayReady(now).ToList();
            var waiting = _offers.NextDayWaiting(now).ToList();
            var list = new List<WidgetOffer>();
            foreach (var product in _store.All.Where(p => p.Shelf == ProductShelf.Offer && p.Widget))
            {
                if (ready.Any(d => d.Sku == product.Id)) list.Add(new WidgetOffer(product, OfferWidgetState.Ready, now));
                else if (waiting.FirstOrDefault(d => d.Sku == product.Id) is { } wait) list.Add(new WidgetOffer(product, OfferWidgetState.Waiting, wait.ClaimableAt));
                else if (_offers.OfferOn(product, now)) list.Add(new WidgetOffer(product, OfferWidgetState.Sale, _offers.Window(product.Id)?.Closes ?? 0));
            }

            return list.OrderBy(w => (int)w.State).ToList();
        }

        // A picture of what an offer is for, while it has no icon of its own (the web's kindIcon).
        public static string Kind(IProductDefinition product)
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
    }
}
