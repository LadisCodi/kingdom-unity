using System.Collections.Generic;

namespace Codigames.Kingdom.Store.State
{
    // An offer's window: when it opened, when it closes (null: until sold out), how many it has sold.
    public class OfferWindow
    {
        public double Opened { get; set; }
        public double? Closes { get; set; }
        public int Bought { get; set; }
    }

    // A bought product's next-day part, waiting for the start of the next day (UTC) and then to be claimed.
    public class NextDayDelivery
    {
        public string Sku { get; set; }
        public double ClaimableAt { get; set; }
    }

    // The store's offers: each one's window, the Townhall level last seen opening them, the next-day parts unclaimed.
    public class OffersState
    {
        public Dictionary<string, OfferWindow> Windows { get; set; } = new();
        public int Townhall { get; set; }
        public List<NextDayDelivery> NextDay { get; set; } = new();
    }
}
