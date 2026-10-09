using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.Fog
{
    // The fog over the province. A cell is Revealed (the kingdom's), Discovered (seen: next to revealed ground,
    // or inside a building's ring) or Undiscovered. A finished building reveals its own ground and the rings its
    // definition gives; the rest is paid for in Gold, a share a tap, a cell at a time, only next to revealed
    // ground and within the rings the Townhall's level reaches. The price grows with the ring and with how much
    // is already revealed.
    public class FogOfWar : IRevealedGround, IExploredGround
    {
        public const string GOLD = "Gold";

        private readonly FogState _state;
        private readonly CityState _city;
        private readonly IProvinceMap _map;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly IConstructionSettings _construction;
        private readonly IFogSettings _settings;
        private readonly ITreasury _treasury;
        private readonly IResearchGates _gates;
        private readonly IBonuses _bonuses;

        public FogOfWar(FogState state, CityState city, IProvinceMap map, ICatalog<IBuildingDefinition> buildings,
            IConstructionSettings construction, IFogSettings settings, ITreasury treasury, IResearchGates gates = null,
            IBonuses bonuses = null)
        {
            _gates = gates;
            _bonuses = bonuses;
            _state = state;
            _city = city;
            _map = map;
            _buildings = buildings;
            _construction = construction;
            _settings = settings;
            _treasury = treasury;

            // A kingdom that has never seen the fog (new, or saved before it) starts with its buildings' rings.
            if (_state.Revealed.Count == 0)
            {
                foreach (var district in _city.Districts.Where(d => d.Built)) RevealAround(district);
            }
        }

        // Cells changed state: their ground and what stands on them come into view, or become the kingdom's.
        public event Action<IReadOnlyCollection<Vector2Int>> Changed;

        // A share of a cell was paid: the cell, and the taps paid so far.
        public event Action<Vector2Int, int> Tapped;

        public int RevealedCount => _state.Revealed.Count;

        public int TapsToReveal => _settings.TapsToReveal;

        public bool IsRevealed(Vector2Int cell) => _state.Revealed.Contains(cell);

        public Visibility VisibilityAt(Vector2Int cell)
        {
            if (_state.Revealed.Contains(cell)) return Visibility.Revealed;
            if (_state.Discovered.Contains(cell) || NextToRevealed(cell)) return Visibility.Discovered;
            return Visibility.Undiscovered;
        }

        public int TapsDone(Vector2Int cell) => _state.Progress.TryGetValue(cell, out var done) ? done : 0;

        // The frontier stays connected: a cell may be paid for only when it touches revealed ground.
        public bool IsReachable(Vector2Int cell) => !IsRevealed(cell) && NextToRevealed(cell);

        public int Reach => At(_settings.ReachPerTownhallLevel, CityQueries.TownhallLevel(_city, _construction));

        public bool IsWithinReach(Vector2Int cell) => Rings(cell) <= Reach;

        // A Discovered cell the player can clear now: reachable and within reach.
        public bool IsPayable(Vector2Int cell) => _map.Contains(cell) && IsReachable(cell) && IsWithinReach(cell);

        // The whole cell's Gold: its ring's price, grown with the revealed count, three significant figures.
        public double Cost(Vector2Int cell)
        {
            var growth = _settings.CountStep <= 0 ? 1 : Math.Pow(_settings.CountGrowth, Math.Floor((double)RevealedCount / _settings.CountStep));
            var cost = Math.Max(_settings.MinCost, Math.Round(RingCost(Rings(cell)) * growth, MidpointRounding.AwayFromZero));
            return Prices.RoundPrice(cost);
        }

        // What one tap charges: the price split in shares that sum to it exactly, the first ones a unit dearer when
        // the taps do not divide it.
        public double TapCost(Vector2Int cell)
        {
            var total = Cost(cell);
            var taps = Math.Max(1, _settings.TapsToReveal);
            var share = Math.Floor(total / taps);
            var remainder = total - share * taps;
            return share + (TapsDone(cell) < remainder ? 1 : 0);
        }

        public RevealResult Tap(Vector2Int cell)
        {
            if (IsRevealed(cell)) return RevealResult.AlreadyRevealed;
            if (!_map.Contains(cell) || !IsReachable(cell)) return RevealResult.NotReachable;
            if (!IsWithinReach(cell)) return RevealResult.OutOfReach;
            if (TerrainTech(cell) != null) return RevealResult.NeedsResearch;

            var payment = TapCost(cell);
            if (!_treasury.TryPay(new Dictionary<string, double> { [GOLD] = payment })) return RevealResult.NotEnoughGold;

            var done = TapsDone(cell) + 1;
            if (done < _settings.TapsToReveal)
            {
                _state.Progress[cell] = done;
                Tapped?.Invoke(cell, done);
                return RevealResult.Paid;
            }

            _state.Progress.Remove(cell);
            Reveal(new[] { cell });
            return RevealResult.Revealed;
        }

        // A finished building's rings: its ground and its reveal radius revealed, its discover radius seen.
        public void RevealAround(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            var fog = building.Fog;
            var reveal = fog.RevealRadiusPerLevel.Count > 0 ? At(fog.RevealRadiusPerLevel, district.Level) : fog.RevealRadius;

            var revealed = Around(district, building, reveal).ToList();
            var discover = (int)Math.Floor(_bonuses.Apply(TechStats.DISCOVER_RADIUS, fog.DiscoverRadius));
            var seen = Around(district, building, Math.Max(reveal, discover)).Where(c => !_state.Revealed.Contains(c) && !revealed.Contains(c)).ToList();
            foreach (var cell in seen) _state.Discovered.Add(cell);

            Reveal(revealed, seen);
        }

        // Every standing building's rings again: what a farther sight now sees.
        public void RevealAroundAll()
        {
            foreach (var district in _city.Districts.Where(d => d.Built).ToList()) RevealAround(district);
        }

        // The technology a cell's terrain waits for, while it is not researched; null otherwise.
        public string TerrainTech(Vector2Int cell)
        {
            var tech = _map.Contains(cell) ? _gates?.TerrainTech(_map.TerrainAt(cell)) : null;
            return tech != null && !_gates.IsOpen(tech) ? tech : null;
        }

        // The rings from the Townhall's footprint to a cell.
        public int Rings(Vector2Int cell) => CityQueries.DistanceFromTownhall(_city, _buildings, _construction, cell);

        private void Reveal(IReadOnlyCollection<Vector2Int> cells, IReadOnlyCollection<Vector2Int> alsoSeen = null)
        {
            var changed = new HashSet<Vector2Int>(alsoSeen ?? Array.Empty<Vector2Int>());

            foreach (var cell in cells)
            {
                if (!_state.Revealed.Add(cell)) continue;
                _state.Discovered.Remove(cell);
                changed.Add(cell);

                // Its neighbours come into view with it.
                foreach (var neighbour in GridMath.Neighbours(cell)) changed.Add(neighbour);
            }

            if (changed.Count > 0) Changed?.Invoke(changed);
        }

        private IEnumerable<Vector2Int> Around(DistrictState district, IBuildingDefinition building, int radius)
            => GridMath.AroundRect(district.Anchor, building.Width, building.Height, radius).Where(_map.Contains);

        private bool NextToRevealed(Vector2Int cell) => GridMath.Neighbours(cell).Any(_state.Revealed.Contains);

        private double RingCost(int ring)
        {
            var rings = _settings.CostPerRing;
            if (rings.Count == 0) return _settings.MinCost;
            if (ring <= rings.Count) return rings[Math.Max(ring, 1) - 1];

            return Math.Round(rings[rings.Count - 1] * Math.Pow(Math.Max(1, _settings.FallbackGrowth), ring - rings.Count),
                MidpointRounding.AwayFromZero);
        }

        private static int At(IReadOnlyList<int> perLevel, int level)
            => perLevel.Count == 0 ? int.MaxValue : perLevel[Math.Min(Math.Max(level, 1), perLevel.Count) - 1];
    }
}
