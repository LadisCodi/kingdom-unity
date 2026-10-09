using Codigames.Game.Data.Bag;
using Codigames.Game.UI.Kit;
using Codigames.Kingdom.Bag;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Bag
{
    // An item as a tile (the web's itemArt.tileArt), the same in the Bag and in the speed-up picker: a picture drawn
    // for this very item carries its duration in its art, a borrowed one gets its size printed over it and a typed
    // speed-up its badge.
    public class ItemTiles
    {
        private readonly ItemProse _prose;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;

        public ItemTiles(ItemProse prose, UiIcons icons, NumberFormat numbers)
        {
            _prose = prose;
            _icons = icons;
            _numbers = numbers;
        }

        public BagTileData Tile(ItemAsset item, int count, bool fresh = false, bool picked = false)
        {
            var own = item.Icon != null && item.Icon.name == item.Id;
            return new BagTileData
            {
                Id = item.Id,
                Icon = item.Icon,
                Tier = item.Tier,
                Size = own && item.Seconds > 0 ? string.Empty : _prose.Size(item),
                Count = _numbers.Exact(count),
                Badge = own || item.Speeds == null ? null : item.Speeds switch
                {
                    SpeedupKind.Construction => _icons.Get("build"),
                    SpeedupKind.Training => _icons.Get("helmet"),
                    SpeedupKind.Workshop => _icons.Get("anvil"),
                    _ => null,
                },
                Fresh = fresh,
                Picked = picked,
            };
        }
    }
}
