using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Economy
{
    // What buildings make waits inside them until the player collects it. A store fills in whole units against
    // its own anchor at its building's rate — the Townhall's own Gold, a house's rent — and stops when full:
    // nothing is banked past the cap. Collecting moves it all to the treasury and starts a stopped store again.
    // The store is read on demand; it changes only at moments of its own (full, a rate changing, a collect), so
    // an absence replays exactly.
    public class Stores : ITimedSystem
    {
        public const string GOLD = "Gold";

        private const double MS_PER_MINUTE = 60_000;

        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IEconomySettings _settings;
        private readonly ITreasury _treasury;
        private readonly IBonuses _bonuses;
        private readonly IBoosts _boosts;
        private readonly Harmony _harmony;
        private readonly Adjacency _adjacency;

        private readonly Relics.IRelicAura _aura;

        public Stores(CityState city, ICatalog<IBuildingDefinition> buildings, IEconomySettings settings, ITreasury treasury,
            Construction construction, IBonuses bonuses = null, IBoosts boosts = null, Harmony harmony = null, Adjacency adjacency = null,
            Relics.IRelicAura aura = null)
        {
            _aura = aura;
            _harmony = harmony;
            _adjacency = adjacency;
            _boosts = boosts;
            _bonuses = bonuses;
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _treasury = treasury;

            // A level finishing, a building placed or moved, change what buildings make — its own, and through Harmony and
            // its neighbours everyone's: what they made until then is counted at the old rates.
            construction.JobCompleting += (job, district) => SettleAll(job.CompletesAt);
            construction.JobCompleted += (job, district) => WakeAll(job.CompletesAt);
            construction.CityChanging += SettleAll;
            construction.CityChanged += WakeAll;
        }

        // A store was collected: the district, what moved to the treasury, and when.
        public event Action<DistrictState, IReadOnlyDictionary<string, double>, double> Collected;

        // A haul landed in a store: the district, the currency, the amount.
        public event Action<DistrictState, string, double> Deposited;

        // Gold a minute the district makes now.
        public double GoldPerMinute(DistrictState district)
        {
            if (!district.Built) return 0;

            var production = _buildings.Get(district.DefinitionId).Production;
            var own = At(production.GoldPerMinutePerLevel, district.Level) * _bonuses.Multiplier(TechStats.OWN_GOLD);
            // The rent: a Harmony surplus at its base, then the tree, then a Rent boost running; the neighbours' Gold on
            // top. An empty house pays nothing, its neighbours' share neither.
            var residents = Residents(district);
            if (residents <= 0) return own;
            var surplus = _harmony?.Multiplier ?? 1;
            var rate = _bonuses.Apply(TechStats.TAX_RATE, _settings.GoldPerPopulationPerMinute * surplus, TargetKind.District, district.DefinitionId)
                       * (_boosts?.Multiplier(BoostKind.Rent) ?? 1)
                       // The Tribute Crown's aura over the house.
                       * Relics.RelicAuraExtensions.Over(_aura, Relics.RelicStats.TAX_RATE, district);
            var rent = residents * rate * (1 + At(production.TaxBonusPerLevel, district.Level)) + (_adjacency?.GoldOf(district) ?? 0);
            return own + Math.Max(0, rent);
        }

        // What a building makes by itself a minute (the Townhall's own Gold), with nobody in it.
        public bool MakesItsOwn(DistrictState district)
            => district.Built && At(_buildings.Get(district.DefinitionId).Production.GoldPerMinutePerLevel, district.Level) > 0;

        public double Capacity(DistrictState district)
            => At(_buildings.Get(district.DefinitionId).Production.StorageCapacityPerLevel, district.Level)
               * _bonuses.Multiplier(TechStats.STORAGE_CAPACITY, TargetKind.District, district.DefinitionId);

        // How many of the city's villagers live in this house: houses fill in build order.
        public int Residents(DistrictState district)
        {
            var left = _city.Population;

            foreach (var house in _city.Districts)
            {
                var room = HousingOf(house);
                var here = Math.Min(left, room);
                if (house == district) return here;
                left -= here;
            }

            return 0;
        }

        // Villagers the city can house.
        public int Housing => _city.Districts.Sum(HousingOf);

        // What the store holds at a moment, banked and made since, never past its capacity.
        public double Held(DistrictState district, double now)
        {
            var banked = district.Store.Held.Values.Sum();
            return Math.Min(Capacity(district), banked + Made(district, now));
        }

        public bool IsFull(DistrictState district, double now) => Capacity(district) > 0 && Held(district, now) >= Capacity(district);

        // Ready to collect: full, or holding the seconds of its making the settings ask — or anything at all from
        // a building that makes nothing now.
        public bool IsReady(DistrictState district, double now)
        {
            var held = Held(district, now);
            if (held <= 0) return false;
            if (IsFull(district, now)) return true;

            var perSecond = GoldPerMinute(district) / 60;
            return perSecond <= 0 || held >= _settings.CollectSeconds * perSecond;
        }

        // Moves the whole store to the treasury; a stopped store starts making again from now.
        public IReadOnlyDictionary<string, double> Collect(DistrictState district, double now)
        {
            Settle(district, now);

            var moved = new Dictionary<string, double>(district.Store.Held);
            foreach (var line in moved) _treasury.Add(line.Key, line.Value);
            district.Store.Held.Clear();

            Wake(district, now);
            if (moved.Count > 0) Collected?.Invoke(district, moved, now);
            return moved;
        }

        // What the store holds of one currency at a moment: its banked share, and for Gold what it has made since.
        public double HeldOf(DistrictState district, string currency, double now)
        {
            var banked = district.Store.Held.TryGetValue(currency, out var n) ? n : 0;
            if (currency != GOLD) return banked;
            var others = district.Store.Held.Where(h => h.Key != GOLD).Sum(h => h.Value);
            return Math.Max(0, Held(district, now) - others);
        }

        // Takes up to `amount` of one currency out of a store at `now`: what it made until then is counted first, and a
        // store that had stopped full starts making again. Returns what it gave.
        public double Take(DistrictState district, string currency, double amount, double now)
        {
            Settle(district, now);
            if (!district.Store.Held.TryGetValue(currency, out var held) || held <= 0 || amount <= 0) return 0;
            var taken = Math.Min(held, amount);
            district.Store.Held[currency] = held - taken;
            Wake(district, now);
            return taken;
        }

        // A crew's haul lands in the store, whole, even past its capacity.
        public void Deposit(DistrictState district, string currency, double amount)
        {
            if (amount <= 0) return;
            district.Store.Held.TryGetValue(currency, out var held);
            district.Store.Held[currency] = held + amount;
            Deposited?.Invoke(district, currency, amount);
        }

        // Counts what the district has made until `now` at its current rate, before something changes the rate.
        public void Settle(DistrictState district, double now)
        {
            var since = district.Store.AccruingSince;
            var perMs = GoldPerMinute(district) / MS_PER_MINUTE;
            if (since == null || perMs <= 0 || now <= since) return;

            var units = Math.Floor((now - since.Value) * perMs);
            if (units <= 0) return;

            var room = Math.Max(0, Capacity(district) - district.Store.Held.Values.Sum());
            Bank(district, Math.Min(units, room));
            district.Store.AccruingSince = since + units / perMs;
        }

        // Counts what every building has made until `now`, before something changes the rates (a villager moving
        // in).
        public void SettleAll(double now)
        {
            foreach (var district in _city.Districts) Settle(district, now);
        }

        // Starts every store that has room and is not already making.
        public void WakeAll(double now)
        {
            foreach (var district in _city.Districts) Wake(district, now);
        }

        public double? NextBoundary(double after)
        {
            double? next = null;

            foreach (var district in _city.Districts)
            {
                var full = FullAt(district);
                if (full > after && (next == null || full < next)) next = full;
            }

            return next;
        }

        // A store that fills stops: what it made is banked to the cap and nothing more is counted.
        public void ApplyDue(double time)
        {
            foreach (var district in _city.Districts)
            {
                if (!(FullAt(district) <= time)) continue;

                var room = Math.Max(0, Capacity(district) - district.Store.Held.Values.Sum());
                Bank(district, room);
                district.Store.AccruingSince = null;
            }
        }

        public void RunUntil(double time) { }

        private void Wake(DistrictState district, double now)
        {
            if (district.Store.AccruingSince != null || GoldPerMinute(district) <= 0) return;
            if (district.Store.Held.Values.Sum() >= Capacity(district)) return;

            district.Store.AccruingSince = now;
        }

        // When a making store reaches its capacity; null when it is not making.
        private double? FullAt(DistrictState district)
        {
            var since = district.Store.AccruingSince;
            var perMs = GoldPerMinute(district) / MS_PER_MINUTE;
            if (since == null || perMs <= 0) return null;

            var room = Math.Max(0, Capacity(district) - district.Store.Held.Values.Sum());
            return since + room / perMs;
        }

        private double Made(DistrictState district, double now)
        {
            var since = district.Store.AccruingSince;
            var perMs = GoldPerMinute(district) / MS_PER_MINUTE;
            if (since == null || perMs <= 0 || now <= since) return 0;

            return Math.Floor((now - since.Value) * perMs);
        }

        private void Bank(DistrictState district, double units)
        {
            if (units <= 0) return;
            district.Store.Held.TryGetValue(GOLD, out var held);
            district.Store.Held[GOLD] = held + units;
        }

        // Beds: whole villagers, so the tree's ranks add whole beds to a house that has any.
        private int HousingOf(DistrictState district)
        {
            if (!district.Built) return 0;

            var beds = At(_buildings.Get(district.DefinitionId).Production.PopulationCapacityPerLevel, district.Level);
            return beds <= 0 ? 0 : (int)Math.Floor(_bonuses.Apply(TechStats.POPULATION_CAPACITY, beds, TargetKind.District, district.DefinitionId));
        }

        private static double At(IReadOnlyList<double> perLevel, int level)
            => perLevel.Count == 0 ? 0 : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];

        private static double At(IReadOnlyList<int> perLevel, int level)
            => perLevel.Count == 0 ? 0 : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];
    }
}
