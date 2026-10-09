using System;
using Codigames.Game.UI.Kit;
using Codigames.Kingdom.City;
using Codigames.Modules.Localization;
using UnityEngine;

namespace Codigames.Game.UI.Buildings
{
    // How a building's figures read (the web's upgradeStats.ts words): the card's tile says a short name, the
    // upgrade sheet a long one, both the same value; a level's gain carries its sign in the figure's own words.
    public class BuildingStatProse
    {
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly UiIcons _icons;

        public BuildingStatProse(NumberFormat numbers, Localizer localizer, UiIcons icons)
        {
            _numbers = numbers;
            _localizer = localizer;
            _icons = icons;
        }

        public Sprite Icon(BuildingStat stat) => _icons.Get(stat.Kind switch
        {
            StatKind.Beds => "bed",
            StatKind.Range => "showme",
            StatKind.Crew => "workers",
            StatKind.Haul => "plus",
            StatKind.Speed => "clock",
            StatKind.Storage => stat.Currency ?? "Gold",
            StatKind.Rent => "Gold",
            StatKind.Income => "Gold",
            StatKind.Fog => "Townhall",
            _ => "hourglass",
        });

        public string Label(StatKind kind) => kind switch
        {
            StatKind.Beds => _localizer.Tr("Beds"),
            StatKind.Range => _localizer.Tr("Exploration range"),
            StatKind.Crew => _localizer.Tr("Workers"),
            StatKind.Haul => _localizer.Tr("Per delivery"),
            StatKind.Speed => _localizer.Tr("Work speed"),
            StatKind.Storage => _localizer.Tr("Storage"),
            StatKind.Rent => _localizer.Tr("Rent each"),
            StatKind.Income => _localizer.Tr("Gold /h"),
            StatKind.Fog => _localizer.Tr("Fog reach"),
            _ => _localizer.Tr("Training time"),
        };

        // The card's word: eight letters at most, three tiles to a row.
        public string Short(StatKind kind) => kind switch
        {
            StatKind.Beds => _localizer.Tr("Beds"),
            StatKind.Range => _localizer.Tr("Range"),
            StatKind.Crew => _localizer.Tr("Crew"),
            StatKind.Haul => _localizer.Tr("Haul"),
            StatKind.Speed => _localizer.Tr("Speed"),
            StatKind.Storage => _localizer.Tr("Storage"),
            StatKind.Rent => _localizer.Tr("Rent"),
            StatKind.Income => _localizer.Tr("Income"),
            StatKind.Fog => _localizer.Tr("Fog"),
            _ => _localizer.Tr("tile::Training"),
        };

        public string Value(BuildingStat stat) => stat.Kind switch
        {
            StatKind.Haul => "+" + _numbers.Exact(stat.Value),
            StatKind.Speed => "×" + _numbers.Number(stat.Value, 2),
            StatKind.Storage => _numbers.Exact(stat.Value),
            StatKind.Rent => "+" + _numbers.Exact(stat.Value) + "%",
            StatKind.Fog => _localizer.Tr("ring {n}", ("n", _numbers.Exact(stat.Value))),
            StatKind.TrainTime => _numbers.Duration(stat.Value),
            _ => _numbers.Number(stat.Value, 2),
        };

        public string Delta(StatChange change)
        {
            var d = Math.Abs(change.Delta);
            var sign = change.Delta < 0 ? "−" : "+";
            return sign + change.Now.Kind switch
            {
                StatKind.Speed => "×" + _numbers.Number(d, 2),
                StatKind.Storage => _numbers.Exact(d),
                StatKind.Rent => _numbers.Exact(d) + "%",
                StatKind.Fog => _localizer.Trn((int)d, "{n} ring", "{n} rings", ("n", _numbers.Exact(d))),
                StatKind.TrainTime => _numbers.Duration(d),
                _ => _numbers.Number(d, 2),
            };
        }
    }
}
