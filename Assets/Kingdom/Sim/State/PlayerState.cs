using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // What belongs to the player, not the kingdom: Gems, the payer profile, the offers.
    public sealed class PlayerState
    {
        public Dictionary<string, double> Wallet { get; set; }
        public PayerState Payer { get; set; }
        public OffersState Offers { get; set; }
    }
}
