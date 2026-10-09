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
    public class Harvesting : ITimedSystem
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

        public Harvesting(HarvestState state, GroundState ground, CityState city, IProvinceMap map,
            ICatalog<IBuildingDefinition> buildings, ICatalog<IFeatureDefinition> features, ICatalog<IHarvestSource> sources,
            ITerrainYields yields, ITapSettings tap, ITreasury treasury, ManaPool mana, uint seed, IRevealedGround revealed)
        {
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

        // A cell's depot changed: drawn on, emptied, or full again.
        public event Action<Vector2Int> DepotChanged;

        // A feature left the ground (emptied for good) or came back to it.
        public event Action<Vector2Int> FeatureRemoved;
        public event Action<Vector2Int, string> FeatureAppeared;

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
            return Math.Max(1, (int)Math.Round(source.Stock * yield, MidpointRounding.AwayFromZero));
        }

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
            if (IsExhausted(cell, now)) return new TapResult(TapRefusal.Exhausted);
            if (!_mana.TrySpend(_tap.ManaCost, now)) return new TapResult(TapRefusal.NoMana);

            var owed = _tap.WorkSeconds * source.UnitsPerStrike / source.SecondsPerStrike;
            _state.Carry.TryGetValue(source.Currency, out var carried);
            var total = owed + carried;
            var wanted = Math.Max(1, Math.Floor(total));
            _state.Carry[source.Currency] = Math.Max(0, total - wanted);

            if (source.Stock <= 0)
            {
                _treasury.Add(source.Currency, wanted);
                return new TapResult(TapRefusal.None, source.Currency, wanted);
            }

            // What the cell cannot give is lost, not carried.
            var depot = DepotOf(cell, source);
            var paid = Math.Min(wanted, depot.Units);
            depot.Units -= (int)paid;
            _treasury.Add(source.Currency, paid);

            var emptied = depot.Units == 0;
            if (emptied) Empty(cell, source, now);
            else DepotChanged?.Invoke(cell);

            return new TapResult(TapRefusal.None, source.Currency, paid, emptied);
        }

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
                _state.Depots[cell].ExhaustedUntil = now + Math.Max(MIN_RECOVERY_MS, source.RecoverySeconds * 1000);
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
                DueAt = now + source.RespawnSeconds * 1000,
            });
        }

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
                var wait = feature != null && _sources.TryGet(feature.Source, out var source) ? source.RespawnSeconds * 1000 : MIN_RECOVERY_MS;
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
