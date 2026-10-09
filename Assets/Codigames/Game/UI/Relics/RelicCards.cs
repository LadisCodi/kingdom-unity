using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Relics;
using Codigames.Game.UI.Data;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Relics
{
    // A relic as its card and the Bag's tab show it (the web's relicCard and relicRows): every relic met, the city
    // relics first; where each stands; its slots; and, asleep, what waking it costs.
    public class RelicCards
    {
        private readonly Kingdom.Relics.Relics _relics;
        private readonly Shrines _shrines;
        private readonly RelicCollection _catalog;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _settings;
        private readonly PriceTerms _prices;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        public RelicCards(Kingdom.Relics.Relics relics, Shrines shrines, RelicCollection catalog, CityState city,
            ICatalog<IBuildingDefinition> buildings, IConstructionSettings settings, PriceTerms prices, NumberFormat numbers, Localizer localizer)
        {
            _relics = relics;
            _shrines = shrines;
            _catalog = catalog;
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _prices = prices;
            _numbers = numbers;
            _localizer = localizer;
        }

        public RelicAsset Get(string id) => _catalog.Get<RelicAsset>(id);

        public RelicStatus Status(string id)
        {
            if (!_relics.IsRestored(id)) return RelicStatus.Broken;
            if (Get(id).Kind == RelicKind.World) return RelicStatus.Bag;
            if (_shrines.IsAwake(id)) return RelicStatus.Awake;
            return _shrines.HostOf(id) != null ? RelicStatus.Asleep : RelicStatus.Bag;
        }

        public IReadOnlyList<RelicSlotData> Slots(string id)
        {
            var relic = Get(id);
            return Enumerable.Range(0, Kingdom.Relics.Relics.SLOTS).Select(s => new RelicSlotData
            {
                Art = relic.Fragment(s), Count = _relics.SlotCount(id, s), CountText = _numbers.Exact(_relics.SlotCount(id, s)), Keystone = s == Kingdom.Relics.Relics.KEYSTONE,
            }).ToList();
        }

        public RelicCardData Card(string id, double now)
        {
            var relic = Get(id);
            var status = Status(id);
            var held = _relics.DistinctHeld(id);
            var left = _shrines.WindowEndsAt(id) is { } ends ? Math.Max(0, ends - now) : 0;
            return new RelicCardData
            {
                Id = id,
                Name = _localizer.Tr(relic.Name),
                Art = relic.Icon,
                World = relic.Kind == RelicKind.World,
                Status = status,
                Level = _relics.IsRestored(id) ? _localizer.Tr("Lv {level}", ("level", _numbers.Exact(_relics.Level(id)))) : null,
                Slots = Slots(id),
                Ready = _relics.CanRestore(id) ? _localizer.Tr("Ready to restore") : null,
                Held = _numbers.Exact(held) + " / " + _numbers.Exact(Kingdom.Relics.Relics.SLOTS),
                Awake = status == RelicStatus.Awake ? _localizer.Tr("Awake · {time}", ("time", _numbers.Duration(Math.Ceiling(left / 1000)))) : null,
                Where = status == RelicStatus.Bag ? _localizer.Tr("In the Bag") : null,
                ActivateLabel = _localizer.Tr("Activate"),
                Activate = status == RelicStatus.Asleep
                    ? _prices.Of(new Dictionary<string, double> { [Kingdom.Magic.ManaPool.MANA] = _shrines.ActivationCost(id) })
                    : Array.Empty<PriceTerm>(),
            };
        }

        // Every relic met, city then world, each in the collection's order.
        public IReadOnlyList<string> Met()
            => _catalog.Items.OfType<RelicAsset>().Where(r => _relics.IsMet(r.Id))
                .OrderBy(r => r.Kind == RelicKind.City ? 0 : 1).Select(r => r.Id).ToList();

        public RelicTabData Tab(double now)
        {
            var met = Met();
            var standing = _shrines.All.Count();
            var max = ShrineCap();
            return new RelicTabData
            {
                Shrines = _localizer.Tr("Shrines {standing} / {max}", ("standing", _numbers.Exact(standing)), ("max", _numbers.Exact(max))),
                ShrinesNote = standing >= max ? null : _localizer.Tr("More from the Build menu"),
                CityHead = _localizer.Tr("City"),
                WorldHead = _localizer.Tr("World"),
                City = met.Where(id => Get(id).Kind == RelicKind.City).Select(id => Card(id, now)).ToList(),
                World = met.Where(id => Get(id).Kind == RelicKind.World).Select(id => Card(id, now)).ToList(),
            };
        }

        // How many Shrines the realm allows at this Townhall.
        private int ShrineCap()
        {
            var shrine = _buildings.Items.FirstOrDefault(b => b.HostsRelic);
            return shrine == null ? 0 : CityQueries.MaxCount(shrine, CityQueries.TownhallLevel(_city, _settings)) ?? 0;
        }
    }
}
