using System;
using System.Collections.Generic;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;

namespace Codigames.Kingdom.City
{
    public enum BuyBuilderResult
    {
        Purchased,
        AtMax,
        NotEnoughGems,
    }

    // THE CREW (the web's grantBuilder / buyBuilder): a builder handed over (a product, an event) and one bought for
    // Gems are apart, so a gift never has to know the price. The price climbs with every builder past the start —
    // a gift makes the next one bought dearer, which is the right way round — up to the ceiling.
    public class Builders
    {
        private const string GEMS = "Gems";

        private readonly CityState _city;
        private readonly IConstructionSettings _settings;
        private readonly ITreasury _treasury;

        public Builders(CityState city, IConstructionSettings settings, ITreasury treasury)
        {
            _city = city;
            _settings = settings;
            _treasury = treasury;
        }

        public event Action Changed;

        public int Count => _city.Builders;
        public int Ceiling => _settings.MaxBuilders;
        public bool AtCeiling => _city.Builders >= _settings.MaxBuilders;

        public double GemCost => Math.Round(_settings.BuilderGemCostBase
                                            * Math.Pow(_settings.BuilderGemCostGrowth, Math.Max(0, _city.Builders - _settings.StartBuilders)),
            MidpointRounding.AwayFromZero);

        public bool Grant()
        {
            if (AtCeiling) return false;
            _city.Builders++;
            Changed?.Invoke();
            return true;
        }

        public BuyBuilderResult Buy()
        {
            if (AtCeiling) return BuyBuilderResult.AtMax;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GEMS] = GemCost })) return BuyBuilderResult.NotEnoughGems;
            _city.Builders++;
            Changed?.Invoke();
            return BuyBuilderResult.Purchased;
        }
    }
}
