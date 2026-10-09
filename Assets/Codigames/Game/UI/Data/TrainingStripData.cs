using System.Collections.Generic;

namespace Codigames.Game.UI.Data
{
    // The Townhall's villager line on its card: how many live in the city of how many it houses, who is on the
    // way, and the next one's price.
    public sealed class TrainingStripData
    {
        public TrainingStripData(string villagers, string onTheWay, IReadOnlyList<PriceTerm> price, bool canTrain, string reason)
        {
            Villagers = villagers;
            OnTheWay = onTheWay;
            Price = price;
            CanTrain = canTrain;
            Reason = reason;
        }

        // "Villagers 1/4".
        public string Villagers { get; }

        // "2 on the way · next in 12s"; empty when nobody is.
        public string OnTheWay { get; }

        public IReadOnlyList<PriceTerm> Price { get; }
        public bool CanTrain { get; }

        // Why no more can be trained, when the price does not say it already.
        public string Reason { get; }
    }
}
