using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.City;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Bag
{
    // What an item is, in words (the web's itemText.ts): the size printed on its tile, its name with that size
    // ("1h Wood chest"), and the one line on what one does or is worth now.
    public class ItemProse
    {
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly BuildingCollection _buildings;

        public ItemProse(NumberFormat numbers, Localizer localizer, BuildingCollection buildings)
        {
            _numbers = numbers;
            _localizer = localizer;
            _buildings = buildings;
        }

        // "10m", "1h", "8h" — or, for what has no duration, its value: "25%", "5"; a key, nothing.
        public string Size(IItemDefinition item) => item.Kind switch
        {
            ItemKind.Flask => _numbers.Exact(item.Value) + "%",
            ItemKind.Tome => _numbers.Exact(item.Value),
            ItemKind.Key => string.Empty,
            _ => _numbers.Duration(item.Seconds),
        };

        public string Name(ItemAsset item)
            => _localizer.Tr("{size} {name}", ("size", Size(item)), ("name", _localizer.Tr(item.Name))).Trim();

        // What one is worth now, or does.
        public string Line(ItemAsset item, IReadOnlyDictionary<string, double> worth)
        {
            var time = _numbers.Duration(item.Seconds);
            switch (item.Kind)
            {
                case ItemKind.Chest when worth.Count > 0:
                    var coin = worth.First();
                    return _localizer.Tr("{time} of {coin} — {n} now", ("time", time), ("coin", _localizer.Tr(coin.Key)),
                        ("n", _numbers.Exact(coin.Value)));
                case ItemKind.Speedup:
                    return item.Speeds switch
                    {
                        SpeedupKind.Construction => _localizer.Tr("Takes {time} off a build or an upgrade", ("time", time)),
                        SpeedupKind.Training => _localizer.Tr("Takes {time} off a training line", ("time", time)),
                        SpeedupKind.Workshop => _localizer.Tr("Takes {time} off the item a workshop is making", ("time", time)),
                        _ => _localizer.Tr("Takes {time} off any build, training or workshop", ("time", time)),
                    };
                case ItemKind.Choice:
                    return _localizer.Tr("{time} of the coin you pick", ("time", time));
                case ItemKind.Boost when item.Boost.HasValue:
                    var pct = _numbers.Exact(item.Value);
                    return item.Boost.Value switch
                    {
                        BoostKind.Rent => _localizer.Tr("Houses pay +{n}% for {time}", ("n", pct), ("time", time)),
                        BoostKind.Harvest => _localizer.Tr("A tap takes +{n}% for {time}", ("n", pct), ("time", time)),
                        _ => _localizer.Tr("Mana fills +{n}% for {time}", ("n", pct), ("time", time)),
                    };
                case ItemKind.Flask:
                    return _localizer.Tr("Fills {n}% of the Mana pool", ("n", _numbers.Exact(item.Value)));
                case ItemKind.Tome:
                    return _localizer.Tr("{n} Knowledge, past the bar's cap", ("n", _numbers.Exact(item.Value)));
                case ItemKind.Key:
                    return _localizer.Tr("One call on its banner, in the store");
                case ItemKind.Part:
                    var ruin = _buildings.Entries.OfType<BuildingAsset>().FirstOrDefault(b => b.Repair.Item == item.Id);
                    return ruin == null
                        ? _localizer.Tr("A piece of something ruined")
                        : _localizer.Tr("Repairs the {name}", ("name", _localizer.Tr(ruin.DisplayName)));
                default:
                    return string.Empty;
            }
        }

        // What a running boost raises: "Houses pay +25%".
        public string BoostWhat(BoostKind kind, double multiplier)
        {
            var pct = _numbers.Exact(System.Math.Round((multiplier - 1) * 100));
            return kind switch
            {
                BoostKind.Rent => _localizer.Tr("Houses pay +{n}%", ("n", pct)),
                BoostKind.Harvest => _localizer.Tr("A tap takes +{n}%", ("n", pct)),
                _ => _localizer.Tr("Mana fills +{n}%", ("n", pct)),
            };
        }
    }
}
