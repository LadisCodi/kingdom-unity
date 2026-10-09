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
        private readonly Footprints _footprints;

        public FogOfWar(FogState state, CityState city, IProvinceMap map, ICatalog<IBuildingDefinition> buildings,
            IConstructionSettings construction, IFogSettings settings, ITreasury treasury, IResearchGates gates = null,
            IBonuses bonuses = null, Footprints footprints = null)
        {
            _footprints = footprints;
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

        // A cell (or a block) was paid for and cleared: its cells, and the cells it brought into view.
        public event Action<IReadOnlyList<Vector2Int>, IReadOnlyList<Vector2Int>> PaidReveal;

        public int RevealedCount => _state.Revealed.Count;

        public int TapsToReveal => _settings.TapsToReveal;

        public bool IsRevealed(Vector2Int cell) => _state.Revealed.Contains(cell);

        // A block is seen when any of its cells is.
        public Visibility VisibilityAt(Vector2Int cell)
        {
            if (_state.Revealed.Contains(cell)) return Visibility.Revealed;
            if (Block(cell).Any(c => _state.Discovered.Contains(c) || NextToRevealed(c))) return Visibility.Discovered;
            return Visibility.Undiscovered;
        }

        // The taps paid on a cell's block (a cell is a block of one).
        public int TapsDone(Vector2Int cell) => _state.Progress.TryGetValue(AnchorOf(cell), out var done) ? done : 0;

        // The frontier stays connected: a block may be paid for only when one of its cells touches revealed ground.
        public bool IsReachable(Vector2Int cell) => !IsRevealed(cell) && Block(cell).Any(NextToRevealed);

        public int Reach => At(_settings.ReachPerTownhallLevel, CityQueries.TownhallLevel(_city, _construction));

        // One cell of a block inside the reach opens the whole of it.
        public bool IsWithinReach(Vector2Int cell) => Block(cell).Any(c => Rings(c) <= Reach);

        // The cells that clear together: a block's, or the cell alone.
        public IEnumerable<Vector2Int> Block(Vector2Int cell) => _footprints?.CellsOf(cell) ?? new[] { cell };

        // A Discovered cell the player can clear now: reachable and within reach.
        public bool IsPayable(Vector2Int cell) => _map.Contains(cell) && IsReachable(cell) && IsWithinReach(cell);

        // The whole block's Gold, the sum of its cells': each its ring's price, grown with the revealed count, three
        // significant figures.
        public double Cost(Vector2Int cell)
        {
            var growth = _settings.CountStep <= 0 ? 1 : Math.Pow(_settings.CountGrowth, Math.Floor((double)RevealedCount / _settings.CountStep));
            return Block(cell).Sum(c => Prices.RoundPrice(Math.Max(_settings.MinCost, Math.Round(RingCost(Rings(c)) * growth, MidpointRounding.AwayFromZero))));
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
            var block = Block(cell).ToList();
            if (done < _settings.TapsToReveal)
            {
                _state.Progress[AnchorOf(cell)] = done;
                foreach (var part in block) Tapped?.Invoke(part, done);
                return RevealResult.Paid;
            }

            _state.Progress.Remove(AnchorOf(cell));
            var fresh = block.SelectMany(GridMath.Neighbours).Distinct()
                .Where(c => _map.Contains(c) && !block.Contains(c) && VisibilityAt(c) == Visibility.Undiscovered).ToList();
            Reveal(block);
            PaidReveal?.Invoke(block, fresh);
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

        // Brings a square's surroundings into view without making them the kingdom's: what a claim lights up.
        public void DiscoverAround(Vector2Int anchor, int size, int radius)
        {
            var seen = GridMath.AroundRect(anchor, size, size, radius).Where(c => _map.Contains(c) && !_state.Revealed.Contains(c)).ToList();
            foreach (var cell in seen) _state.Discovered.Add(cell);
            if (seen.Count > 0) Changed?.Invoke(seen);
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

            // A block clears whole, whatever revealed one of its cells.
            foreach (var cell in cells.SelectMany(Block).Where(_map.Contains).Distinct().ToList())
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

        private Vector2Int AnchorOf(Vector2Int cell) => _footprints?.AnchorOf(cell) ?? cell;

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
