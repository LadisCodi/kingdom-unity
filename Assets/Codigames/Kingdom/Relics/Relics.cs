using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Relics.State;
using Codigames.Modules.Core;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Relics
{
    public enum RelicLevelResult
    {
        Levelled,
        NotRestored,
        MissingFragments,
        NotEnoughStardust,
    }

    public enum RelicRestoreResult
    {
        Restored,
        Missing,
        AlreadyRestored,
    }

    public enum FragmentPackResult
    {
        Opened,
        NothingMet,
        NotEnoughGems,
    }

    // A fragment that landed: which relic, which slot (5 is the keystone).
    public readonly struct FragmentDrop
    {
        public FragmentDrop(string relic, int slot)
        {
            Relic = relic;
            Slot = slot;
        }

        public string Relic { get; }
        public int Slot { get; }
    }

    // RELICS FOUND AND RESTORED (the web's relics.ts and artifacts.ts): a relic is six fragments — five pieces and a
    // keystone. Six distinct restore it at level 1; every level after takes one of each again and Stardust, with no
    // ceiling. The first fragment is found by play at the relic's door; a drop never rolls a relic not met. Rolls hash
    // the event that paid them. Found and bound (bought) are counted apart, and a level spends the bound first.
    public class Relics : IRelicDrops
    {
        public const int SLOTS = 6;
        public const int KEYSTONE = 5;
        private const string STARDUST = "Stardust";
        private const string GEMS = "Gems";

        private readonly RelicsState _state;
        private readonly ICatalog<IRelicDefinition> _relics;
        private readonly IRelicSettings _settings;
        private readonly ITreasury _treasury;
        private readonly uint _seed;

        public Relics(RelicsState state, ICatalog<IRelicDefinition> relics, IRelicSettings settings, ITreasury treasury, uint seed,
            Research.Researching research = null)
        {
            // A band of the tree finished pays its fragments, rolled on the band.
            if (research != null) research.EraFinished += (tome, era, fragments) => Drop(null, fragments, "band", tome, era);
            _state = state;
            _relics = relics;
            _settings = settings;
            _treasury = treasury;
            _seed = seed;
        }

        // A relic's level or fragments moved.
        public event Action Changed;

        // Fragments landed, for whatever shows them.
        public event Action<IReadOnlyList<FragmentDrop>> Found;

        public IReadOnlyList<IRelicDefinition> All => _relics.Items;
        public IRelicDefinition Get(string id) => _relics.Get(id);
        public IRelicSettings Settings => _settings;

        // ---- what is held

        public int Level(string id) => _state.Levels.TryGetValue(id, out var n) ? n : 0;
        public bool IsRestored(string id) => Level(id) >= 1;

        public int SlotCount(string id, int slot)
            => _state.Held.TryGetValue(id, out var f) ? f.Found[slot] + f.Bound[slot] : 0;

        public bool IsMet(string id) => IsRestored(id) || Enumerable.Range(0, SLOTS).Any(s => SlotCount(id, s) > 0);

        public int DistinctHeld(string id) => Enumerable.Range(0, SLOTS).Count(s => SlotCount(id, s) > 0);

        public bool CanRestore(string id) => !IsRestored(id) && DistinctHeld(id) == SLOTS;

        // Stardust the next level asks from `level`, rounded as every curve's price is.
        public double LevelStardust(int level)
            => Prices.RoundPrice(_settings.LevelStardustBase * Math.Pow(_settings.LevelStardustGrowth, Math.Max(0, level - 1)));

        // ---- restoring and levelling

        public RelicRestoreResult Restore(string id)
        {
            if (IsRestored(id)) return RelicRestoreResult.AlreadyRestored;
            if (DistinctHeld(id) < SLOTS) return RelicRestoreResult.Missing;
            SpendSet(id);
            _state.Levels[id] = 1;
            Changed?.Invoke();
            return RelicRestoreResult.Restored;
        }

        public RelicLevelResult? LevelUpBlock(string id)
        {
            if (!IsRestored(id)) return RelicLevelResult.NotRestored;
            if (DistinctHeld(id) < SLOTS) return RelicLevelResult.MissingFragments;
            if (_treasury.Get(STARDUST) < LevelStardust(Level(id))) return RelicLevelResult.NotEnoughStardust;
            return null;
        }

        // A level is a whole set and its Stardust: one of each of the six — bound before found — and nothing spent
        // when either falls short.
        public RelicLevelResult LevelUp(string id)
        {
            if (LevelUpBlock(id) is { } block) return block;
            var level = Level(id);
            if (!_treasury.TryPay(new Dictionary<string, double> { [STARDUST] = LevelStardust(level) })) return RelicLevelResult.NotEnoughStardust;
            SpendSet(id);
            _state.Levels[id] = level + 1;
            Changed?.Invoke();
            return RelicLevelResult.Levelled;
        }

        // A relic handed over whole (a tutorial line's gift): restored at level 1, no fragments needed.
        public bool Give(string id)
        {
            if (IsRestored(id)) return false;
            _state.Levels[id] = 1;
            Changed?.Invoke();
            return true;
        }

        // ---- the drops

        // `n` fragments of met relics of the kind (null: any), rolled on the event that paid them. Nothing when none of
        // the kind has been met.
        public IReadOnlyList<FragmentDrop> Drop(RelicKind? kind, int n, params object[] parts) => Drop(kind, n, false, parts);

        public IReadOnlyList<FragmentDrop> Drop(RelicKind? kind, int n, bool bound, params object[] parts)
        {
            var dropped = new List<FragmentDrop>();
            var pool = _relics.Items.Where(r => (kind == null || r.Kind == kind) && IsMet(r.Id)).ToList();
            if (pool.Count == 0 || n <= 0) return dropped;
            for (var i = 0; i < n; i++)
            {
                var head = new object[] { "fragment" }.Concat(parts).Concat(new object[] { i }).ToArray();
                var relic = pool[Math.Min(pool.Count - 1, (int)Math.Floor(Rand.Value(_seed, head.Concat(new object[] { "relic" }).ToArray()) * pool.Count))];
                var slot = Rand.Value(_seed, head.Concat(new object[] { "slot" }).ToArray()) < 1.0 / Math.Max(1, _settings.KeystoneOneIn)
                    ? KEYSTONE
                    : Math.Min(4, (int)Math.Floor(Rand.Value(_seed, head.Concat(new object[] { "piece" }).ToArray()) * 5));
                var f = Own(relic.Id);
                (bound ? f.Bound : f.Found)[slot] += 1;
                dropped.Add(new FragmentDrop(relic.Id, slot));
            }

            Announce(dropped);
            return dropped;
        }

        // A relic's door: the first piece of every relic behind it not met yet.
        public IReadOnlyList<FragmentDrop> OpenDoor(string door)
        {
            var dropped = new List<FragmentDrop>();
            foreach (var relic in _relics.Items.Where(r => r.Door == door && !IsMet(r.Id)))
            {
                Own(relic.Id).Found[0] += 1;
                dropped.Add(new FragmentDrop(relic.Id, 0));
            }

            Announce(dropped);
            return dropped;
        }

        public IReadOnlyList<FragmentDrop> ForLair(string lair, int tier)
        {
            var tiers = _settings.PerLairTier;
            var share = tier >= 1 && tier <= tiers.Count ? tiers[tier - 1] : 0;
            return OpenDoor(lair).Concat(Drop(RelicKind.City, share, "lair", lair)).ToList();
        }

        public IReadOnlyList<FragmentDrop> ForTreasure(int n)
        {
            var every = _settings.TreasureEvery;
            return every > 0 && n > 0 && n % every == 0 ? Drop(RelicKind.City, 1, "treasure", n) : Array.Empty<FragmentDrop>();
        }

        // THE FRAGMENT PACK, sold in the store: bound fragments of the met relics, for Gems.
        public FragmentPackResult OpenPack(out IReadOnlyList<FragmentDrop> drops)
        {
            drops = Array.Empty<FragmentDrop>();
            if (!_relics.Items.Any(r => IsMet(r.Id))) return FragmentPackResult.NothingMet;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GEMS] = _settings.FragmentPackGems })) return FragmentPackResult.NotEnoughGems;
            _state.Packs += 1;
            drops = Drop(null, _settings.FragmentPackSize, true, "pack", _state.Packs);
            return FragmentPackResult.Opened;
        }

        // ---- a city relic's level cycle

        // What a city relic's level-ups have raised by `level`, round the cycle; a window step past the last authored
        // window raises the number instead. A world relic has no cycle: every level is its effect.
        public (int Window, int Radius, int Effect) Steps(string id, int level)
        {
            if (Get(id).Kind != RelicKind.City) return (0, 0, Math.Max(0, level - 1));
            int window = 0, radius = 0, effect = 0;
            var cycle = _settings.Cycle;
            for (var l = 2; l <= level && cycle.Count > 0; l++)
            {
                var axis = cycle[(l - 2) % cycle.Count];
                if (axis == RelicAxis.Window && window >= _settings.WindowMinutes.Count - 1) effect++;
                else if (axis == RelicAxis.Window) window++;
                else if (axis == RelicAxis.Radius) radius++;
                else effect++;
            }

            return (window, radius, effect);
        }

        // What the level-up from `level` raises on a city relic; null on a world relic.
        public RelicAxis? NextAxis(string id, int level)
        {
            if (Get(id).Kind != RelicKind.City) return null;
            var now = Steps(id, level);
            var next = Steps(id, level + 1);
            if (next.Window > now.Window) return RelicAxis.Window;
            if (next.Radius > now.Radius) return RelicAxis.Radius;
            return RelicAxis.Effect;
        }

        public double WindowMs(string id, int level)
            => _settings.WindowMinutes[Math.Min(Steps(id, level).Window, _settings.WindowMinutes.Count - 1)] * 60_000.0;

        public int Radius(string id, int level) => Get(id).ActivationRadius + Steps(id, level).Radius;

        // The number at a level: base, and what each effect step adds; never below nothing.
        public double Value(string id, int level)
        {
            var relic = Get(id);
            return Math.Max(0, relic.PassiveBase + relic.PassivePerLevel * Steps(id, Math.Max(1, level)).Effect);
        }

        public double Value(string id) => Value(id, Level(id));

        // ---- helpers

        private RelicFragments Own(string id)
        {
            if (!_state.Held.TryGetValue(id, out var f)) _state.Held[id] = f = new RelicFragments();
            return f;
        }

        private void SpendSet(string id)
        {
            var f = Own(id);
            for (var s = 0; s < SLOTS; s++)
            {
                if (f.Bound[s] > 0) f.Bound[s] -= 1;
                else f.Found[s] -= 1;
            }
        }

        private void Announce(List<FragmentDrop> dropped)
        {
            if (dropped.Count == 0) return;
            Changed?.Invoke();
            Found?.Invoke(dropped);
        }
    }
}
