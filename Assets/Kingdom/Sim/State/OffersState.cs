using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The store's offer windows and next-day deliveries.
    public sealed class OffersState
    {
        public Dictionary<string, OfferWindow> Windows { get; set; }
        public double Townhall { get; set; }
        public List<NextDayDelivery> NextDay { get; set; }
    }
}
