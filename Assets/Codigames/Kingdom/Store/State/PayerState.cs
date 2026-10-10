using System.Collections.Generic;

namespace Codigames.Kingdom.Store.State
{
    // One purchase: what, for how much, when.
    public class Purchase
    {
        public string Sku { get; set; }
        public int PriceCents { get; set; }
        public double At { get; set; }
    }

    // The payer this kingdom plays as, chosen once, and what it has spent: the running month's spend (its month an
    // index, UTC), every purchase, and the taps the budget could not cover.
    public class PayerState
    {
        // Null until chosen.
        public PayerProfile? Profile { get; set; }
        public double ChosenAt { get; set; }
        public int MonthIndex { get; set; }
        public int SpentCentsThisMonth { get; set; }
        public List<Purchase> Purchases { get; set; } = new();
        public int Refusals { get; set; }
    }
}
