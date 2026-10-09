using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Modifiers;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Lairs.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Randomness;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Lairs
{
    // The lairs' raids (Docs/features/18-garrisons-and-raids.md §3–§4). A lair found is armed: its first raid after its
    // warning, then three a local day, each in its own slice of the hours the player plays, at a moment hashed from
    // the lair, the day and the slice — the same in a replayed absence as in a watched one. A raid takes seconds of
    // what the city MAKES of each material, never more than half of what its stores hold, never the Townhall's own
    // Gold nor the purse; the lair carries a day of raids, and what it cannot carry is lost.
    public class Lairs : ITimedSystem
    {
        private const string LAIR_KNOWLEDGE = "lairKnowledge";
        private const string KNOWLEDGE_YIELD = "knowledgeYield";
        private const double HOUR_MS = 3_600_000;
        private const double DAY_MS = 24 * HOUR_MS;
        private static readonly string[] RAIDABLE = { "Gold", "Food", "Wood", "Stone" };

        private readonly LairsState _state;
        private readonly LairGround _ground;
        private readonly ILairSettings _settings;
        private readonly CityState _city;
        private readonly Stores _stores;
        private readonly Workforce _crews;
        private readonly uint _seed;
        private readonly IBonuses _bonuses;
        // The kingdom's modifier stack: a Legendary's boon reaches the lump.
        private IModifiers Stack { get; }

        public Lairs(LairsState state, LairGround ground, ILairSettings settings, CityState city, Stores stores, Workforce crews, uint seed,
            IBonuses bonuses = null, IModifiers modifiers = null)
        {
            _bonuses = bonuses;
            Stack = modifiers;
            _state = state;
            _ground = ground;
            _settings = settings;
            _city = city;
            _stores = stores;
            _crews = crews;
            _seed = seed;
        }

        // A lair's state moved outside its clock: a fight won, beaten, claimed.
        public event Action<string> Changed;

        // A lair was found and armed: its id, when.
        public event Action<string, double> Armed;

        // A raid took from the stores: the lair, when, and what it took.
        public event Action<string, double, IReadOnlyDictionary<string, double>> Raided;

        public IReadOnlyList<ILairSite> All => _ground.All;

        public LairState StateOf(string id) => _state.Lairs.TryGetValue(id, out var lair) ? lair : null;

        public ILairSite Site(string id) => _ground.All.FirstOrDefault(l => l.Id == id);

        public Garrison GarrisonOf(ILairSite lair) => _settings.Garrisons[Math.Min(Math.Max(lair.Tier, 1), _settings.Garrisons.Count) - 1];

        public int Fights(ILairSite lair) => Math.Max(1, GarrisonOf(lair).Fights);

        // A fight's power on the path: a share of the guard's at the first, rising evenly to all of it at the last.
        public int FightPower(ILairSite lair, int index)
        {
            var n = Fights(lair);
            if (index >= n - 1) return lair.Power;
            var first = _settings.FirstFightPower;
            return Math.Max(1, (int)Math.Round(lair.Power * (first + (1 - first) * Math.Max(0, index) / (n - 1.0)), MidpointRounding.AwayFromZero));
        }

        // Hero XP a fight on its path teaches: the tier's share of the whole path.
        public double FightXp(ILairSite lair) => Economy.Prices.RoundPrice(GarrisonOf(lair).HeroXp / Fights(lair));

        // What clearing it pays on top of its hoard: a fight's Hero XP, and the first-clear Knowledge lump.
        // The last fight's Hero XP and the first-clear Knowledge lump, each raised by the spoils the party that beat it
        // carried — Seasoned and Lore.
        public (double HeroXp, double Knowledge) ClearReward(ILairSite lair)
        {
            var state = StateOf(lair.Id);
            var lump = Math.Max(0, Math.Round(Stack.Apply(KNOWLEDGE_YIELD,
                _bonuses.Apply(KNOWLEDGE_YIELD, _settings.FirstClearKnowledge * (_bonuses?.Multiplier(LAIR_KNOWLEDGE) ?? 1))), MidpointRounding.AwayFromZero));
            return (Economy.Prices.RoundPrice(FightXp(lair) * (1 + (state?.SpoilsSeasoned ?? 0))),
                Math.Round(lump * (1 + (state?.SpoilsLore ?? 0)), MidpointRounding.AwayFromZero));
        }



        // Carries all it can of a material: what raids take past it is lost.
        public bool HoardFull(ILairSite lair, string currency, double now)
            => StateOf(lair.Id) is { } state && state.Hoard.TryGetValue(currency, out var held) && held > 0 && held >= HoardCap(lair, currency, now);

        // Fights won on its path so far.
        public int Won(ILairSite lair)
            => StateOf(lair.Id) is { } state ? Math.Min(Fights(lair), state.Defeated ? Fights(lair) : state.Won) : 0;

        // The next fight, 0-based: the last is the lair's own garrison.
        public int FightIndex(ILairSite lair) => Math.Min(Won(lair), Fights(lair) - 1);

        // Beaten, its reward waiting to be claimed.
        public bool AwaitsClaim(string id) => StateOf(id) is { Defeated: true, Cleared: false };

        // One fight on its path is won; short of the last the lair still stands and still raids. The last beats it: its
        // clock stops for good. Returns whether this win beat it.
        public bool WinFight(string id)
        {
            var lair = Site(id);
            var state = StateOf(id);
            if (lair == null || state == null || state.Cleared || state.Defeated) return false;
            var won = Won(lair) + 1;
            if (won >= Fights(lair))
            {
                state.Won = Fights(lair);
                state.Defeated = true;
                state.NextRaidAt = null;
            }
            else
            {
                state.Won = won;
            }

            Changed?.Invoke(id);
            return state.Defeated;
        }

        // The claim: the hoard comes home and the ground it held is the city's. Only a beaten lair is claimed; returns what
        // its hoard gave back.
        public IReadOnlyDictionary<string, double> Claim(string id, ITreasury treasury)
        {
            var state = StateOf(id);
            if (state == null || state.Cleared || !state.Defeated) return new Dictionary<string, double>();
            var hoard = new Dictionary<string, double>(state.Hoard);
            foreach (var (currency, amount) in hoard)
                if (amount > 0) treasury.Add(currency, amount);
            state.Cleared = true;
            state.NextRaidAt = null;
            state.Hoard.Clear();
            Changed?.Invoke(id);
            return hoard;
        }

        public int FoundCount => _state.Lairs.Count;

        public int ClearedCount => _state.Lairs.Values.Count(l => l.Cleared);

        // The standing lairs' next raid, soonest first.
        public IEnumerable<(ILairSite Lair, double At)> Coming
            => _ground.All.Select(l => (l, StateOf(l.Id)))
                .Where(x => x.Item2 is { Cleared: false, Defeated: false, NextRaidAt: not null })
                .Select(x => (x.l, x.Item2.NextRaidAt.Value)).OrderBy(x => x.Item2);

        // The device's local time moved: every lair on the daily schedule goes back on it from `t`, but one still
        // inside its first warning keeps the countdown it has shown.
        public void SetUtcOffset(int minutes, double t)
        {
            if (_state.UtcOffsetMinutes == minutes) return;
            _state.UtcOffsetMinutes = minutes;
            foreach (var site in _ground.All)
            {
                var lair = StateOf(site.Id);
                if (lair == null || lair.Cleared || lair.NextRaidAt == null) continue;
                if (lair.NextRaidAt == lair.ArmedAt + site.WarningMinutes * 60_000) continue;
                lair.NextRaidAt = RaidTimeAfter(site.Id, t);
            }
        }

        // The next raid strictly after `after`, on the daily schedule.
        public double RaidTimeAfter(string id, double after)
        {
            var offset = _state.UtcOffsetMinutes * 60_000.0;
            var open = _settings.WindowStartHour * HOUR_MS;
            var span = Math.Max(HOUR_MS, _settings.WindowEndHour * HOUR_MS - open);
            var perDay = Math.Max(1, _settings.RaidsPerDay);
            var slice = span / perDay;
            for (var day = (long)Math.Floor((after + offset) / DAY_MS); ; day++)
            {
                var midnight = day * DAY_MS - offset;
                for (var k = 0; k < perDay; k++)
                {
                    var at = Math.Floor(midnight + open + slice * (k + Rand.Value(_seed, id, "raid", day, k)));
                    if (at > after) return at;
                }
            }
        }

        // What the city makes of a material a second: the crews', and for Gold the rent — never the Townhall's own.
        public double RatePerSecond(string currency, double now)
        {
            var gathered = _crews.GatherPerSecond(currency);
            if (currency != Stores.GOLD) return gathered;
            return gathered + _city.Districts.Where(Raidable).Sum(d => _stores.GoldPerMinute(d)) / 60;
        }

        // What a raid would take now.
        public IReadOnlyDictionary<string, double> Take(ILairSite lair, double now)
        {
            var seconds = GarrisonOf(lair).TakeSeconds;
            var took = new Dictionary<string, double>();
            foreach (var currency in RAIDABLE)
            {
                var produced = RatePerSecond(currency, now) * seconds;
                if (produced <= 0) continue;
                var take = Math.Floor(Math.Min(produced, Stored(currency, now) * _settings.TakeFractionMax));
                if (take > 0) took[currency] = take;
            }

            return took;
        }

        // The most a lair carries of a material: a day of raids at the city's rate now.
        public double HoardCap(ILairSite lair, string currency, double now)
            => Math.Floor(Math.Max(1, _settings.RaidsPerDay) * RatePerSecond(currency, now) * GarrisonOf(lair).TakeSeconds);

        public double? NextBoundary(double after)
        {
            double? earliest = null;
            foreach (var lair in _state.Lairs.Values)
            {
                if (lair.Cleared || lair.NextRaidAt == null || lair.NextRaidAt <= after) continue;
                if (earliest == null || lair.NextRaidAt < earliest) earliest = lair.NextRaidAt;
            }

            return earliest;
        }

        // Every raid due by `t`, in lair order; a raid that finds nothing still moves its clock on.
        public void ApplyDue(double t)
        {
            Arm(t);
            foreach (var site in _ground.All)
            {
                var lair = StateOf(site.Id);
                if (lair == null) continue;
                while (lair.NextRaidAt != null && lair.NextRaidAt <= t && !lair.Cleared && !lair.Defeated)
                {
                    var at = lair.NextRaidAt.Value;
                    var took = new Dictionary<string, double>();
                    foreach (var (currency, amount) in Take(site, at))
                    {
                        var room = Math.Max(0, HoardCap(site, currency, at) - (lair.Hoard.TryGetValue(currency, out var h) ? h : 0));
                        var got = TakeFromStores(currency, amount, at);
                        if (got <= 0) continue;
                        took[currency] = got;
                        var kept = Math.Min(got, room);
                        if (kept > 0) lair.Hoard[currency] = (lair.Hoard.TryGetValue(currency, out var held) ? held : 0) + kept;
                    }

                    lair.NextRaidAt = RaidTimeAfter(site.Id, at);
                    if (took.Count > 0) Raided?.Invoke(site.Id, at, took);
                }
            }
        }

        // A lair is armed at the boundary that finds it (ApplyDue), never between two.
        public void RunUntil(double time)
        {
        }

        private void Arm(double t)
        {
            foreach (var site in _ground.All)
            {
                if (_state.Lairs.TryGetValue(site.Id, out var lair))
                {
                    if (!lair.Cleared && !lair.Defeated && lair.NextRaidAt == null) lair.NextRaidAt = RaidTimeAfter(site.Id, t);
                    continue;
                }

                if (!_ground.IsFound(site)) continue;
                _state.Lairs[site.Id] = new LairState { ArmedAt = t, NextRaidAt = t + site.WarningMinutes * 60_000 };
                Armed?.Invoke(site.Id, t);
            }
        }

        // The Townhall's own Gold is never raided: it is the city's floor.
        private bool Raidable(DistrictState district) => district.Built && !_stores.MakesItsOwn(district);

        private double Stored(string currency, double now) => _city.Districts.Where(Raidable).Sum(d => _stores.HeldOf(d, currency, now));

        // Each store gives its share of what they hold between them, rounded up, in district order.
        private double TakeFromStores(string currency, double amount, double now)
        {
            var total = Stored(currency, now);
            if (total <= 0 || amount <= 0) return 0;
            var left = Math.Min(amount, total);
            foreach (var district in _city.Districts.Where(Raidable))
            {
                if (left <= 0) break;
                var here = _stores.HeldOf(district, currency, now);
                if (here <= 0) continue;
                left -= _stores.Take(district, currency, Math.Min(left, Math.Ceiling(amount * here / total)), now);
            }

            return Math.Min(amount, total) - left;
        }
    }
}
