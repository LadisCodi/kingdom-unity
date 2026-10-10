using System.Collections.Generic;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Heroes;
using Codigames.Kingdom.Store;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Store
{
    // What a product hands over, as tiles (the web's offerTiles): now — its hero, its items, its Gems; tomorrow — its
    // hero's fragments, Gems, Hero XP, items.
    public class OfferTiles
    {
        private const string GEMS = "Gems";
        private const string HERO_XP = "HeroXp";

        private readonly HeroCollection _heroes;
        private readonly ItemCollection _items;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;

        public OfferTiles(HeroCollection heroes, ItemCollection items, ICurrencyIcons icons, NumberFormat numbers)
        {
            _heroes = heroes;
            _items = items;
            _icons = icons;
            _numbers = numbers;
        }

        public List<OfferTileData> Now(IProductDefinition product)
        {
            var tiles = new List<OfferTileData>();
            if (Hero(product) is { } hero) tiles.Add(new OfferTileData { Icon = hero.Portrait != null ? hero.Portrait : hero.Art, Count = "×1", Hero = true });
            Items(tiles, product.Items);
            if (product.Gems > 0) tiles.Add(Coin(GEMS, product.Gems));
            return tiles;
        }

        public List<OfferTileData> NextDay(IProductDefinition product)
        {
            var tiles = new List<OfferTileData>();
            if (product.NextDayFragments > 0 && Hero(product) is { } hero)
                tiles.Add(new OfferTileData { Icon = hero.Fragment, Count = "×" + _numbers.Exact(product.NextDayFragments) });
            if (product.NextDayGems > 0) tiles.Add(Coin(GEMS, product.NextDayGems));
            if (product.NextDayHeroXp > 0) tiles.Add(Coin(HERO_XP, product.NextDayHeroXp));
            Items(tiles, product.NextDayItems);
            return tiles;
        }

        public HeroAsset Hero(IProductDefinition product)
            => product.Hero != null && _heroes.TryGet(product.Hero, out var found) ? found as HeroAsset : null;

        private void Items(List<OfferTileData> tiles, IReadOnlyDictionary<string, int> items)
        {
            foreach (var (id, count) in items)
                if (_items.TryGet(id, out var item) && item is ItemAsset asset)
                    tiles.Add(new OfferTileData { Icon = asset.Icon, Count = "×" + _numbers.Exact(count) });
        }

        private OfferTileData Coin(string currency, int count) => new() { Icon = _icons.IconOf(currency), Count = "×" + _numbers.Exact(count) };
    }
}
