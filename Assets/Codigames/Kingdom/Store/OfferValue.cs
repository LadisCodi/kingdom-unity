using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Store
{
    // THE VALUE A PACK PRINTS (the web's skuValuePercent), computed, never authored: what everything it hands over would
    // cost in Gems at the game's own Gem prices, over the Gems its price buys at the best pack, in percent, to the
    // nearest ten. 100 is a Gem pack. An authored percentage could lie, and would go stale the first time a price moved.
    public class OfferValue
    {
        private readonly ICatalog<IProductDefinition> _products;
        private readonly ICatalog<IItemDefinition> _items;
        private readonly ICatalog<IBannerDefinition> _banners;
        private readonly ICatalog<IHeroDefinition> _heroes;
        private readonly IHeroLadderSettings _ladder;
        private readonly IRushSettings _rush;
        private readonly Research.IKnowledgeSettings _knowledge;
        private readonly IManaRefillPrice _mana;
        private readonly Func<double> _builderCost;
        private readonly Func<double> _heroSlotCost;
        private readonly Func<double> _explorerCost;

        public OfferValue(ICatalog<IProductDefinition> products, ICatalog<IItemDefinition> items, ICatalog<IBannerDefinition> banners,
            ICatalog<IHeroDefinition> heroes, IHeroLadderSettings ladder, IRushSettings rush, Research.IKnowledgeSettings knowledge,
            IManaRefillPrice mana, Func<double> builderCost, Func<double> heroSlotCost, Func<double> explorerCost = null)
        {
            _products = products;
            _items = items;
            _banners = banners;
            _heroes = heroes;
            _ladder = ladder;
            _rush = rush;
            _knowledge = knowledge;
            _mana = mana;
            _builderCost = builderCost;
            _heroSlotCost = heroSlotCost;
            _explorerCost = explorerCost ?? (() => 0);
        }

        // Gems a dollar buys at the best Gem pack.
        public double GemsPerDollar()
        {
            var best = 1.0;
            foreach (var p in _products.Items)
                if (p.Shelf == ProductShelf.Gems && p.PriceUsd > 0) best = Math.Max(best, p.Gems / p.PriceUsd);
            return best;
        }

        // Time at the Finish price, Mana at the first refill's, Knowledge at its fixed price, a key at the store's.
        public double ItemGemWorth(string id)
        {
            if (!_items.TryGet(id, out var item)) return 0;
            return item.Kind switch
            {
                ItemKind.Speedup or ItemKind.Chest or ItemKind.Choice => GemRush.GemsToFinish(item.Seconds, _rush.SecondsPerGem),
                ItemKind.Boost => GemRush.GemsToFinish(item.Seconds * item.Value / 100, _rush.SecondsPerGem),
                ItemKind.Flask => Math.Round(_mana.FirstRefillGems * item.Value / 100, MidpointRounding.AwayFromZero),
                ItemKind.Tome => Math.Max(0, item.Value) * _knowledge.GemsPerPoint,
                ItemKind.Key => KeyCost(id),
                _ => 0,
            };
        }

        private double KeyCost(string id)
        {
            var banner = _banners.Items.FirstOrDefault(b => b.Key == id) ?? _banners.Items.FirstOrDefault();
            return banner?.KeyGemCost ?? 0;
        }

        // What a hero costs in keys to be sure of: the cheapest banner's guarantee for its rarity.
        public double HeroGemWorth(string id)
        {
            if (!_heroes.TryGet(id, out var hero)) return 0;
            var prices = new List<double>();
            foreach (var banner in _banners.Items)
            {
                if (!(banner.Weight(hero.Rarity) > 0)) continue;
                var pity = hero.Rarity == HeroRarity.Legendary ? banner.LegendaryPityAt : banner.HardPityAt;
                if (pity > 0) prices.Add(banner.KeyGemCost * pity);
            }

            return prices.Count == 0 ? 0 : prices.Min();
        }

        // Everything a product hands over, in Gems, its next day's part too. A fragment is its share of its hero; Hero
        // XP has no Gem price.
        public double GemWorth(IProductDefinition p)
        {
            double Worth(IReadOnlyDictionary<string, int> items) => items.Sum(i => ItemGemWorth(i.Key) * i.Value);
            var hero = p.Hero == null ? 0 : HeroGemWorth(p.Hero);
            var fragment = p.Hero == null || !_heroes.TryGet(p.Hero, out var h) ? 0 : hero / Math.Max(1, _ladder.RecruitFragments(h.Rarity));
            return p.Gems + Worth(p.Items) + p.NextDayGems + Worth(p.NextDayItems)
                   + Math.Round(fragment * p.NextDayFragments, MidpointRounding.AwayFromZero)
                   + hero
                   + p.Builders * _builderCost()
                   + p.HeroSlots * _heroSlotCost()
                   + p.Explorers * _explorerCost();
        }

        public int ValuePercent(IProductDefinition p)
        {
            var paid = p.PriceUsd * GemsPerDollar();
            if (!(paid > 0)) return 0;
            return (int)Math.Round(GemWorth(p) / paid * 10, MidpointRounding.AwayFromZero) * 10;
        }
    }
}
