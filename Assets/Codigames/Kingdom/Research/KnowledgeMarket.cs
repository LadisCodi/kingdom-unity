using System;
using System.Collections.Generic;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Research.State;

namespace Codigames.Kingdom.Research
{
    // Knowledge for Gold or Gems, into the bar (over its cap if it must). With Gold every point costs more than
    // the last, for ever, so it is a valve for a rich city and never a second faucet; with Gems a point costs
    // the same every time.
    public class KnowledgeMarket
    {
        public const string GOLD = "Gold";
        public const string GEMS = "Gems";

        private readonly KnowledgeState _state;
        private readonly ITreasury _treasury;
        private readonly IKnowledgeSettings _settings;

        public KnowledgeMarket(KnowledgeState state, ITreasury treasury, IKnowledgeSettings settings)
        {
            _state = state;
            _treasury = treasury;
            _settings = settings;
        }

        // What `count` points bought with Gold: one rounded price for the lot.
        public double GoldPrice(int count)
        {
            if (count <= 0) return 0;

            double price = 0;
            for (var n = _state.BoughtWithGold + 1; n <= _state.BoughtWithGold + count; n++)
            {
                price += Math.Round(_settings.GoldPriceBase * Math.Pow(n, _settings.GoldPriceExponent), MidpointRounding.AwayFromZero);
            }

            return Prices.RoundPrice(price);
        }

        public double GemPrice(int count) => Math.Max(0, count) * _settings.GemsPerPoint;

        public double Price(int count, string till) => till == GOLD ? GoldPrice(count) : GemPrice(count);

        public BuyResult Buy(int count, string till)
        {
            if (count <= 0) return BuyResult.NothingToBuy;

            var price = Price(count, till);
            if (!_treasury.TryPay(new Dictionary<string, double> { [till] = price })) return BuyResult.CannotAfford;

            if (till == GOLD) _state.BoughtWithGold += count;
            _treasury.Add(KnowledgeBar.KNOWLEDGE, count);
            return BuyResult.Bought;
        }
    }
}
