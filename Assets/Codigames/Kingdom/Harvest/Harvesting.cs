using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;
using Codigames.Modules.Randomness;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Harvest
{
    // Taking from the ground by hand. A cell is a depot: a tap costs Mana, is worth some seconds of work at the
    // cell's own rhythm, pays at least one unit (a fraction owed is carried to the next tap) and never more than
    // the cell holds. Emptied, a forest or a crop grows back in place; a bush, a herd or a shoal is gone, and
    // comes back next to where it stood. Both happen at their own moments, so this is on the timeline.
    public class Harvesting : ITimedSystem, IPlanting
    {
        private const string RESPAWN_PREFIX = "respawn";
        private const double MIN_RECOVERY_MS = 1000;

        private readonly HarvestState _state;
        private readonly GroundState _ground;
        private readonly CityState _city;
        private readonly IProvinceMap _map;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ICatalog<IFeatureDefinition> _features;
        private readonly ICatalog<IHarvestSource> _sources;
        private readonly ITerrainYields _yields;
        private readonly ITapSettings _tap;
        private readonly ITreasury _treasury;
        private readonly ManaPool _mana;
        private readonly uint _seed;
        private readonly IRevealedGround _revealed;
        private readonly IResearchGates _gates;
        private readonly IBonuses _bonuses;

        public Harvesting(HarvestState state, GroundState ground, CityState city, IProvinceMap map,
            ICatalog<IBuildingDefinition> buildings, ICatalog<IFeatureDefinition> features, ICatalog<IHarvestSource> sources,
            ITerrainYields yields, ITapSettings tap, ITreasury treasury, ManaPool mana, uint seed, IRevealedGround revealed,
            IResearchGates gates = null, IBonuses bonuses = null)
        {
            _gates = gates;
            _bonuses = bonuses;
            _revealed = revealed;
            _state = state;
            _ground = ground;
            _city = city;
            _map = map;
            _buildings = buildings;
            _features = features;
            _sources = sources;
            _yields = yields;
            _tap = tap;
            _treasury = treasury;
            _mana = mana;
            _seed = seed;
        }

        // A tap took from a cell: the cell, and what it paid.
        public event Action<Vector2Int, TapResult> Tapped;

        // A cell's depot changed: drawn on, emptied, or full again.
        public event Action<Vector2Int> DepotChanged;

        // A feature left the ground (emptied for good) or came back to it.
        public event Action<Vector2Int> FeatureRemoved;
        public event Action<Vector2Int, string> FeatureAppeared;

        // A source by id; null when there is none.
        public IHarvestSource Source(string id) => _sources.TryGet(id, out var source) ? source : null;

        // The source a cell yields; null when nothing can be taken from it.
        public IHarvestSource SourceAt(Vector2Int cell)
        {
            if (!_revealed.IsRevealed(cell)) return null;
            if (!_ground.Features.TryGetValue(cell, out var featureId)) return null;
            if (CityQueries.At(_city, _buildings, cell) != null) return null;
            if (!_features.TryGet(featureId, out var feature) || feature.Source == null) return null;
            return _sources.TryGet(feature.Source, out var source) ? source : null;
        }

        // What a cell holds when full, after the ground under it; 0 for bedrock.
        public int FullStock(Vector2Int cell, IHarvestSource source)
        {
            if (source.Stock <= 0) return 0;

            var yield = _yields.YieldOf(_map.TerrainAt(cell), source.Currency);
            var held = source.Stock * yield * _bonuses.Multiplier(TechStats.CELL_STOCK, TargetKind.Harvest, source.Id);
            return Math.Max(1, (int)Math.Round(held, MidpointRounding.AwayFromZero));
        }

        // The technology that opens a source while it is not researched; null once it is (or nothing gates it).
        public string MissingTech(IHarvestSource source)
        {
            var tech = _gates?.HarvestTech(source.Id);
            return tech != null && !_gates.IsOpen(tech) ? tech : null;
        }

        // Units one extraction takes out of a kind of cell, tap or crew: a fraction, carried.
        public double UnitsPerStrike(IHarvestSource source)
            => source.UnitsPerStrike * _bonuses.Multiplier(TechStats.HARVEST_YIELD, TargetKind.Harvest, source.Id);

        // Seconds of work one tap is worth.
        public double TapWorkSeconds => _bonuses.Apply(TechStats.TAP_WORK_SECONDS, _tap.WorkSeconds);

        // What a cell holds now; 0 for bedrock.
        public int UnitsAt(Vector2Int cell)
        {
            var source = SourceAt(cell);
            if (source == null) return 0;
            return _state.Depots.TryGetValue(cell, out var depot) ? depot.Units : FullStock(cell, source);
        }

        public bool IsExhausted(Vector2Int cell, double now)
            => _state.Depots.TryGetValue(cell, out var depot) && depot.ExhaustedUntil > now;

        public double? ExhaustedUntil(Vector2Int cell)
            => _state.Depots.TryGetValue(cell, out var depot) ? depot.ExhaustedUntil : null;

        public TapResult Tap(Vector2Int cell, double now)
        {
            var source = SourceAt(cell);
            if (source == null) return new TapResult(TapRefusal.NothingThere);
            var missing = MissingTech(source);
            if (missing != null) return new TapResult(TapRefusal.NeedsResearch, requiredTech: missing);
            if (IsExhausted(cell, now)) return new TapResult(TapRefusal.Exhausted);
            if (!_mana.TrySpend(_tap.ManaCost, now)) return new TapResult(TapRefusal.NoMana);

            var owed = TapWorkSeconds * UnitsPerStrike(source) / source.SecondsPerStrike;
            _state.Carry.TryGetValue(source.Currency, out var carried);
            var total = owed + carried;
            var wanted = Math.Max(1, Math.Floor(total));
            _state.Carry[source.Currency] = Math.Max(0, total - wanted);

            var paid = Draw(cell, source, wanted, now, out var emptied);
            _treasury.Add(source.Currency, paid);
            var result = new TapResult(TapRefusal.None, source.Currency, paid, emptied);
            Tapped?.Invoke(cell, result);
            return result;
        }

        // A crew's strike: takes up to `want` units from the cell (all of them from bedrock), the cell emptying as a
        // tap empties it. Returns what it took.
        public double Draw(Vector2Int cell, double want, double now)
        {
            var source = SourceAt(cell);
            if (source == null || want <= 0 || IsExhausted(cell, now) || MissingTech(source) != null) return 0;
            return Draw(cell, source, want, now, out _);
        }

        public int Count(string feature) => _ground.Features.Values.Count(f => f == feature);

        // How long one of a feature grows once planted or moved, in seconds.
        public double GrowSecondsOf(string feature)
            => _features.TryGet(feature, out var definition) && definition.Source != null ? Source(definition.Source)?.GrowSeconds ?? 0 : 0;

        // A planted feature lands growing for its source's growth, flat: an emptied cell whose wait is its growth,
        // which cannot be tapped or worked and comes back full. With no growth it is full at once.
        public void Plant(string feature, Vector2Int cell, double now)
        {
            _ground.Features[cell] = feature;
            var growMs = GrowSecondsOf(feature) * 1000;
            if (growMs <= 0) _state.Depots.Remove(cell);
            else _state.Depots[cell] = new CellDepot { Units = 0, ExhaustedUntil = now + growMs, WaitMs = growMs, Growing = true };
            FeatureAppeared?.Invoke(cell, feature);
            DepotChanged?.Invoke(cell);
        }

        // Takes the feature off a cell, all it held with it: the cell is bare ground. Returns what stood there.
        public string Lift(Vector2Int cell)
        {
            if (!_ground.Features.TryGetValue(cell, out var feature)) return null;
            _ground.Features.Remove(cell);
            _state.Depots.Remove(cell);
            FeatureRemoved?.Invoke(cell);
            DepotChanged?.Invoke(cell);
            return feature;
        }

        // Planted or moved and still coming up.
        public bool IsGrowing(Vector2Int cell, double now)
            => _state.Depots.TryGetValue(cell, out var depot) && depot.Growing && depot.ExhaustedUntil > now;

        // How far through its wait an emptied or growing cell is, 0 to 1; null when it is not waiting.
        public double? Regrowth(Vector2Int cell, double now)
        {
            if (!_state.Depots.TryGetValue(cell, out var depot) || !(depot.ExhaustedUntil > now)) return null;
            if (depot.WaitMs <= 0) return 0;
            return Math.Clamp(1 - (depot.ExhaustedUntil.Value - now) / depot.WaitMs, 0, 1);
        }

        // Is the cell's feature one of these (what plantables sow)?
        public bool Sown(Vector2Int cell, ICollection<string> sown) => _ground.Features.TryGetValue(cell, out var feature) && sown.Contains(feature);

        // What is left of a cell's stock, 0 to 1: 1 for one never drawn on, or bedrock.
        public double StockLeft(Vector2Int cell)
        {
            var source = SourceAt(cell);
            if (source == null || source.Stock <= 0 || !_state.Depots.TryGetValue(cell, out var depot)) return 1;
            var full = FullStock(cell, source);
            return full <= 0 ? 1 : Math.Clamp((double)depot.Units / full, 0, 1);
        }

        // When an emptied cell is full again; null when it is not empty.
        public double? RecoversAt(Vector2Int cell) => ExhaustedUntil(cell);

        public double? NextBoundary(double after)
        {
            double? next = null;

            foreach (var depot in _state.Depots.Values)
            {
                if (depot.ExhaustedUntil > after && (next == null || depot.ExhaustedUntil < next)) next = depot.ExhaustedUntil;
            }

            foreach (var respawn in _state.Respawns)
            {
                if (respawn.DueAt > after && (next == null || respawn.DueAt < next)) next = respawn.DueAt;
            }

            return next;
        }

        public void ApplyDue(double time)
        {
            foreach (var cell in _state.Depots.Where(d => d.Value.ExhaustedUntil <= time).Select(d => d.Key).ToList())
            {
                _state.Depots.Remove(cell);
                DepotChanged?.Invoke(cell);
            }

            foreach (var respawn in _state.Respawns.Where(r => r.DueAt <= time).ToList()) Return(respawn, time);
        }

        public void RunUntil(double time) { }

        // Takes from the cell: all it wants from bedrock, else what the depot still holds; the last unit empties it.
        private double Draw(Vector2Int cell, IHarvestSource source, double want, double now, out bool emptied)
        {
            emptied = false;
            if (source.Stock <= 0) return want;

            var depot = DepotOf(cell, source);
            var taken = Math.Min(want, depot.Units);
            depot.Units -= (int)taken;

            emptied = depot.Units == 0;
            if (emptied) Empty(cell, source, now);
            else DepotChanged?.Invoke(cell);
            return taken;
        }

        private CellDepot DepotOf(Vector2Int cell, IHarvestSource source)
        {
            if (!_state.Depots.TryGetValue(cell, out var depot))
            {
                depot = new CellDepot { Units = FullStock(cell, source) };
                _state.Depots[cell] = depot;
            }

            return depot;
        }

        private void Empty(Vector2Int cell, IHarvestSource source, double now)
        {
            if (source.RecoverySeconds > 0)
            {
                var speed = Math.Max(1, _bonuses.Multiplier(TechStats.REGROWTH_SPEED, TargetKind.Harvest, source.Id));
                var wait = Math.Max(MIN_RECOVERY_MS, source.RecoverySeconds * 1000 / speed);
                _state.Depots[cell].ExhaustedUntil = now + wait;
                _state.Depots[cell].WaitMs = wait;
                DepotChanged?.Invoke(cell);
                return;
            }

            var featureId = _ground.Features[cell];
            _state.Depots.Remove(cell);
            _ground.Features.Remove(cell);
            FeatureRemoved?.Invoke(cell);

            if (source.RespawnSeconds <= 0) return;

            _state.Respawns.Add(new Respawn
            {
                Id = RESPAWN_PREFIX + "-" + _state.NextRespawn++,
                FeatureId = featureId,
                Origin = cell,
                DueAt = now + RespawnMs(source),
            });
        }

        private double RespawnMs(IHarvestSource source)
            => source.RespawnSeconds * 1000 / Math.Max(1, _bonuses.Multiplier(TechStats.RESPAWN_SPEED, TargetKind.Harvest, source.Id));

        // A finite feature comes back on a free cell of its terrain next to where it stood; with none free, it
        // tries again after another wait.
        private void Return(Respawn respawn, double time)
        {
            var terrain = _features.TryGet(respawn.FeatureId, out var feature) ? feature.RespawnTerrain : null;
            var free = GridMath.Neighbours(respawn.Origin)
                .Where(c => _map.Contains(c) && _map.TerrainAt(c) == terrain && !_ground.Features.ContainsKey(c)
                            && CityQueries.At(_city, _buildings, c) == null)
                .ToArray();

            if (free.Length == 0)
            {
                var wait = feature != null && _sources.TryGet(feature.Source, out var source) ? RespawnMs(source) : MIN_RECOVERY_MS;
                respawn.DueAt = time + wait;
                return;
            }

            var cell = Rand.Pick(_seed, free, respawn.Id);
            _state.Respawns.Remove(respawn);
            _ground.Features[cell] = respawn.FeatureId;
            FeatureAppeared?.Invoke(cell, respawn.FeatureId);
        }
    }
}
