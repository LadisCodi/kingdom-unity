using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog.State;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Fog
{
    // What the people who fled left on the ground, found at a steady pace whichever way the kingdom explores: one
    // is due every few cells paid for (the first on the very first) and lands on a cell that reveal brought into
    // view, so it is seen as a closed chest beside the frontier. Where it lands and what it is are rolled on its
    // own number, never on the moment. Revealed, a tap picks it up, free; it pays seconds of what the city makes of
    // its coin, priced then. It waits for ever, so nothing here is on the timeline.
    public class Treasures
    {
        public const string KNOWLEDGE = "Knowledge";
        private const string STONE = "Stone";
        private const string TREASURE_YIELD = "treasureYield";

        private readonly FogState _state;
        private readonly FogOfWar _fog;
        private readonly IProvinceMap _map;
        private readonly Footprints _footprints;
        private readonly GroundState _ground;
        private readonly ISiteGround _sites;
        private readonly ITreasureSettings _settings;
        private readonly ITreasury _treasury;
        private readonly IProduction _production;
        private readonly IResearchGates _gates;
        private readonly ICatalog<IHarvestSource> _sources;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly uint _seed;
        private readonly IBonuses _bonuses;

        // Every so many treasures carry a relic fragment; null where relics are not in play.
        private readonly Relics.IRelicDrops _relicDrops;

        public Treasures(FogState state, FogOfWar fog, IProvinceMap map, Footprints footprints, GroundState ground, ISiteGround sites,
            ITreasureSettings settings, ITreasury treasury, IProduction production, IResearchGates gates,
            ICatalog<IHarvestSource> sources, ICatalog<IBuildingDefinition> buildings, Construction construction, uint seed,
            IBonuses bonuses = null, Relics.IRelicDrops relicDrops = null)
        {
            _relicDrops = relicDrops;
            _state = state;
            _fog = fog;
            _map = map;
            _footprints = footprints;
            _ground = ground;
            _sites = sites;
            _settings = settings;
            _treasury = treasury;
            _production = production;
            _gates = gates;
            _sources = sources;
            _buildings = buildings;
            _seed = seed;
            _bonuses = bonuses;

            _fog.PaidReveal += OnPaidReveal;
            construction.DistrictPlaced += OnDistrictPlaced;
            construction.Planted += OnPlanted;
        }

        // A treasure was set down on a cell.
        public event Action<Vector2Int, Treasure> Placed;

        // A treasure was picked up: its cell, and what it paid.
        public event Action<Vector2Int, IReadOnlyDictionary<string, double>> PickedUp;

        public Treasure At(Vector2Int cell) => _state.Treasures.TryGetValue(cell, out var treasure) ? treasure : null;

        public IEnumerable<KeyValuePair<Vector2Int, Treasure>> All => _state.Treasures;

        // Is one owed and not yet placed?
        public bool IsDue => _state.PaidReveals > 0
                             && _state.TreasuresPlaced < 1 + (_state.PaidReveals - 1) / Math.Max(1, _settings.EveryReveals);

        // What it would pay if it were picked up now.
        public IReadOnlyDictionary<string, double> Reward(Treasure treasure)
        {
            if (treasure.N == 0) return new Dictionary<string, double> { [_settings.FirstCoin] = _settings.FirstAmount };
            if (treasure.Coin == KNOWLEDGE) return new Dictionary<string, double> { [KNOWLEDGE] = _settings.Knowledge };

            var floor = _settings.Floor.TryGetValue(treasure.Coin, out var least) ? least : 0;
            var made = _production.MakesPerSecond(treasure.Coin) * _settings.WorkSeconds;
            return new Dictionary<string, double>
            {
                [treasure.Coin] = Prices.RoundPrice(Math.Max(floor, made)) * _bonuses.Multiplier(TREASURE_YIELD),
            };
        }

        // Picks up the treasure on a revealed cell; null when there is none to pick up.
        public IReadOnlyDictionary<string, double> PickUp(Vector2Int cell)
        {
            var treasure = At(cell);
            if (treasure == null || !_fog.IsRevealed(cell)) return null;

            var reward = Reward(treasure);
            foreach (var line in reward) _treasury.Add(line.Key, line.Value);
            _relicDrops?.ForTreasure(treasure.N);
            _state.Treasures.Remove(cell);
            PickedUp?.Invoke(cell, reward);
            return reward;
        }

        private void OnPaidReveal(IReadOnlyList<Vector2Int> revealed, IReadOnlyList<Vector2Int> fresh)
        {
            _state.PaidReveals += revealed.Count;
            if (!IsDue) return;

            // A cell the reveal brought into view first, else any discovered neighbour of what it cleared; bare
            // ground before a feature, since a chest set down in a wood is a chest the trees hide.
            var candidates = BareFirst(Holding(fresh));
            if (candidates.Count == 0) candidates = BareFirst(Holding(revealed.SelectMany(GridMath.Neighbours)));
            if (candidates.Count == 0) return;

            var n = _state.TreasuresPlaced;
            var cell = candidates[(int)Math.Floor(Rand.Value(_seed, "treasure", n, "cell") * candidates.Count)];
            var treasure = new Treasure { N = n, Coin = CoinFor(n) };
            _state.Treasures[cell] = treasure;
            _state.TreasuresPlaced = n + 1;
            Placed?.Invoke(cell, treasure);
        }

        // Building over a treasure picks it up rather than burying it.
        // A crop plot planted over one picks it up, as a building does: it is never buried.
        private void OnPlanted(string definitionId, Vector2Int cell)
        {
            if (At(cell) != null) PickUp(cell);
        }

        private void OnDistrictPlaced(DistrictState district)
        {
            var building = _buildings.Get(district.DefinitionId);
            foreach (var cell in GridMath.Rect(district.Anchor, building.Width, building.Height).Where(c => At(c) != null).ToList()) PickUp(cell);
        }

        // In a fixed order (by the cell's "x,y"), whatever order they came in.
        private List<Vector2Int> Holding(IEnumerable<Vector2Int> cells)
            => cells.Distinct().Where(CanHold).OrderBy(c => $"{c.X},{c.Y}", StringComparer.Ordinal).ToList();

        private List<Vector2Int> BareFirst(List<Vector2Int> cells)
        {
            var bare = cells.Where(c => !_ground.Features.ContainsKey(c)).ToList();
            return bare.Count > 0 ? bare : cells;
        }

        // A cell the player could pay for right now, under no site or block, with no treasure on it already.
        private bool CanHold(Vector2Int cell)
            => _map.Contains(cell) && !_state.Treasures.ContainsKey(cell) && _fog.VisibilityAt(cell) == Visibility.Discovered
               && _fog.IsPayable(cell) && _fog.TerrainTech(cell) == null && !_sites.Holds(cell) && (_footprints?.SizeOf(cell) ?? 1) == 1;

        // The first is authored; the rest weighed among the coins the player can already read.
        private string CoinFor(int n)
        {
            if (n == 0) return _settings.FirstCoin;

            var pool = _settings.Weights.Where(w => w.Value > 0 && (w.Key != STONE || StoneShown())).ToList();
            var roll = Rand.Value(_seed, "treasure", n, "coin") * pool.Sum(w => w.Value);
            foreach (var (coin, weight) in pool)
            {
                if (roll < weight) return coin;
                roll -= weight;
            }

            return pool.Count > 0 ? pool[pool.Count - 1].Key : _settings.FirstCoin;
        }

        // Stone is on the plank once it is held or a source of it is open.
        private bool StoneShown()
            => _treasury.Get(STONE) > 0
               || _sources.Items.Any(s => s.Currency == STONE && _gates?.HarvestTech(s.Id) is { } tech && _gates.IsOpen(tech));
    }
}
