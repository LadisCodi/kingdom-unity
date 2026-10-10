using System;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Store;

namespace Codigames.Game.Store
{
    // What the offers read off the kingdom (the web's offers.ts reads state directly): the doors by the data's ids, the
    // Townhall, the needs felt, and the slots a pack would open. There are no Mana refills nor explorers yet: Mana out is
    // the pool below one, and neither Mana low (the refill ad) nor explorers out is felt.
    public class OfferContext : IOfferContext
    {
        private readonly Doors _doors;
        private readonly CityState _city;
        private readonly IConstructionSettings _construction;
        private readonly Builders _builders;
        private readonly Kingdom.Heroes.Heroes _heroes;
        private readonly ManaPool _mana;

        public OfferContext(Doors doors, CityState city, IConstructionSettings construction, Builders builders, Kingdom.Heroes.Heroes heroes,
            ManaPool mana)
        {
            _doors = doors;
            _city = city;
            _construction = construction;
            _builders = builders;
            _heroes = heroes;
            _mana = mana;
        }

        public bool IsDoorOpen(string door) => Enum.TryParse<DoorId>(door, true, out var id) && _doors.IsOpen(id);

        public int TownhallLevel => CityQueries.TownhallLevel(_city, _construction);

        public bool Feels(string need) => need switch
        {
            "manaOut" => _mana.Amount < 1,
            "heroesBenched" => _heroes.Owned.Count > _heroes.Slots,
            _ => false,
        };

        public bool SlotsFit(IProductDefinition product)
            => _builders.Count + product.Builders <= _builders.Ceiling && _heroes.Slots + product.HeroSlots <= _heroes.SlotCeiling;
    }
}
