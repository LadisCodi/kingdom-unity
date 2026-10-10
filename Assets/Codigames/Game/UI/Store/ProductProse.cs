using System;
using System.Collections.Generic;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.Heroes;
using Codigames.Game.UI.Bag;
using Codigames.Kingdom.Heroes;
using Codigames.Kingdom.Store;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Store
{
    // A product in words (the web's bundleLines): what it hands over besides its Gems, one line a thing, the things
    // for good first; its price in dollars; how long until the budget refills.
    public class ProductProse
    {
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ItemCollection _items;
        private readonly ItemProse _itemProse;
        private readonly HeroCollection _heroes;

        public ProductProse(NumberFormat numbers, Localizer localizer, ItemCollection items, ItemProse itemProse, HeroCollection heroes)
        {
            _numbers = numbers;
            _localizer = localizer;
            _items = items;
            _itemProse = itemProse;
            _heroes = heroes;
        }

        public string Price(IProductDefinition product) => _numbers.Usd(Kingdom.Store.Store.PriceCents(product));

        public IReadOnlyList<string> Lines(IProductDefinition product)
        {
            var lines = new List<string>();
            if (product.Hero != null && _heroes.TryGet(product.Hero, out var found) && found is HeroAsset hero)
            {
                var name = _localizer.Tr(hero.Name);
                lines.Add(hero.Rarity switch
                {
                    HeroRarity.Legendary => _localizer.Tr("{name}, legendary hero", ("name", name)),
                    HeroRarity.Rare => _localizer.Tr("{name}, rare hero", ("name", name)),
                    _ => _localizer.Tr("{name}, common hero", ("name", name)),
                });
            }

            if (product.Builders > 0)
                lines.Add(_localizer.Trn(product.Builders, "A builder, for good", "{n} builders, for good", ("n", _numbers.Exact(product.Builders))));
            if (product.Explorers > 0)
                lines.Add(_localizer.Trn(product.Explorers, "A second explorer, for good", "{n} explorers, for good", ("n", _numbers.Exact(product.Explorers))));
            if (product.HeroSlots > 0)
                lines.Add(_localizer.Trn(product.HeroSlots, "A hero slot, for good", "{n} hero slots, for good", ("n", _numbers.Exact(product.HeroSlots))));
            foreach (var (id, count) in product.Items)
                if (_items.TryGet(id, out var item) && item is ItemAsset asset)
                    lines.Add(_numbers.Exact(count) + "× " + _itemProse.Name(asset));
            return lines;
        }

        // "in 3 days": the web's describeWait.
        public string Wait(double ms)
        {
            var minutes = Math.Max(1, (int)Math.Round(ms / 60_000));
            if (minutes < 60) return _localizer.Trn(minutes, "in {n} minute", "in {n} minutes", ("n", _numbers.Exact(minutes)));
            var hours = (int)Math.Round(minutes / 60.0);
            if (hours < 24) return _localizer.Trn(hours, "in {n} hour", "in {n} hours", ("n", _numbers.Exact(hours)));
            var days = (int)Math.Round(hours / 24.0);
            return _localizer.Trn(days, "in {n} day", "in {n} days", ("n", _numbers.Exact(days)));
        }
    }
}
