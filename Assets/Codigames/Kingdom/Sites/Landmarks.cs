using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.Sites
{
    // The province's sanctuaries: what paid fog is for. One is claimed once, for the Gold its site asks, and gives
    // for good: a bigger Mana pool, a lump of Knowledge, and a lantern held up over the map — the cells round it
    // discovered, never revealed, so the fog stays the thing the Gold is spent on.
    public class Landmarks
    {
        private const string GOLD = "Gold";
        private const string LANDMARK_KNOWLEDGE = "landmarkKnowledge";
        private const string KNOWLEDGE_YIELD = "knowledgeYield";

        private readonly SitesState _state;
        private readonly IProvinceSites _sites;
        private readonly FogOfWar _fog;
        private readonly ITreasury _treasury;
        private readonly IKnowledgeSettings _knowledge;
        private readonly int _discoverRadius;
        private readonly IBonuses _bonuses;
        private readonly Drip _mana;
        private readonly Lairs.ILairGround _lairs;

        public Landmarks(SitesState state, IProvinceSites sites, FogOfWar fog, ITreasury treasury, IKnowledgeSettings knowledge,
            int discoverRadius, IBonuses bonuses = null, Drip mana = null, Lairs.ILairGround lairs = null)
        {
            _lairs = lairs;
            _mana = mana;
            _state = state;
            _sites = sites;
            _fog = fog;
            _treasury = treasury;
            _knowledge = knowledge;
            _discoverRadius = discoverRadius;
            _bonuses = bonuses;
        }

        // A landmark was claimed.
        public event Action<ILandmarkSite> Claimed;

        public IReadOnlyList<ILandmarkSite> All => _sites.Landmarks;

        public int ClaimedCount => _state.Claimed.Count;

        public bool IsClaimed(string id) => _state.Claimed.Contains(id);

        // The landmark standing on a cell; null when none does.
        public ILandmarkSite At(Vector2Int cell) => All.FirstOrDefault(l => Cells(l).Contains(cell));

        public IEnumerable<Vector2Int> Cells(ILandmarkSite site) => GridMath.Rect(site.Anchor, site.Size, site.Size);

        public double ClaimCost(ILandmarkSite site) => Math.Max(1, Prices.RoundPrice(site.ClaimCost));

        // What a claim pays in Knowledge: the lump, raised by the tree, whole points.
        public double KnowledgeLump
            => Math.Max(0, Math.Round(_knowledge.LandmarkClaimLump * _bonuses.Multiplier(LANDMARK_KNOWLEDGE) * _bonuses.Multiplier(KNOWLEDGE_YIELD),
                MidpointRounding.AwayFromZero));

        public int DiscoverRadius => _discoverRadius;

        // Claims it at `now`: a pool its raise left below the new cap starts filling from then.
        public ClaimResult Claim(string id, double now)
        {
            var site = All.FirstOrDefault(l => l.Id == id);
            if (site == null) return ClaimResult.NotFound;
            if (IsClaimed(id)) return ClaimResult.AlreadyClaimed;
            if (Cells(site).Any(c => !_fog.IsRevealed(c))) return ClaimResult.NotRevealed;
            if (_lairs?.HoldingAt(site.Anchor) != null) return ClaimResult.LairHeld;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GOLD] = ClaimCost(site) })) return ClaimResult.CannotAfford;

            _state.Claimed.Add(id);
            _treasury.Add(KnowledgeBar.KNOWLEDGE, KnowledgeLump);
            _fog.DiscoverAround(site.Anchor, site.Size, _discoverRadius);
            _mana?.Wake(now);
            Claimed?.Invoke(site);
            return ClaimResult.Claimed;
        }
    }
}
