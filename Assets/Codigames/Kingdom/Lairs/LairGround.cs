using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Lairs.State;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Core;
using Codigames.Modules.Grid;

namespace Codigames.Kingdom.Lairs
{
    // The ground a lair holds (Docs/proposals/lairs.md §2–§3): a ring of its radius round its footprint, Chebyshev.
    // A lair is found once any of that ground is revealed — seen from afar is not enough — and from the sweep that
    // arms it until it is cleared its ground is not the city's: no tap, no plot, no crew, no claim.
    public class LairGround : ILairGround
    {
        private readonly IProvinceSites _sites;
        private readonly LairsState _state;
        private readonly IRevealedGround _revealed;

        public LairGround(IProvinceSites sites, LairsState state, IRevealedGround revealed)
        {
            _sites = sites;
            _state = state;
            _revealed = revealed;
        }

        public IReadOnlyList<ILairSite> All => _sites.Lairs;

        public IEnumerable<Vector2Int> Zone(ILairSite lair) => GridMath.AroundRect(lair.Anchor, lair.Size, lair.Size, lair.Radius);

        public bool IsFound(ILairSite lair) => Zone(lair).Any(_revealed.IsRevealed);

        public bool IsCleared(string id) => _state.Lairs.TryGetValue(id, out var lair) && lair.Cleared;

        // Armed (the sweep that found it) and not yet cleared.
        public bool Holds(string id) => _state.Lairs.TryGetValue(id, out var lair) && !lair.Cleared;

        // The lair holding a cell, in lair order; null for free ground.
        public string HoldingAt(Vector2Int cell)
        {
            foreach (var lair in _sites.Lairs)
            {
                if (Covers(lair, cell) && Holds(lair.Id)) return lair.Id;
            }

            return null;
        }

        // A standing lair's own footprint, found or not: never building ground.
        public bool Stands(Vector2Int cell)
            => _sites.Lairs.Any(l => !IsCleared(l.Id) && GridMath.Rect(l.Anchor, l.Size, l.Size).Contains(cell));

        private static bool Covers(ILairSite lair, Vector2Int cell)
            => cell.X >= lair.Anchor.X - lair.Radius && cell.X < lair.Anchor.X + lair.Size + lair.Radius
               && cell.Y >= lair.Anchor.Y - lair.Radius && cell.Y < lair.Anchor.Y + lair.Size + lair.Radius;
    }

    // Port: whether a lair holds a cell — asked by the taps, the plots, the crews and the claims.
    public interface ILairGround
    {
        string HoldingAt(Vector2Int cell);

        // A standing lair's footprint covers the cell.
        bool Stands(Vector2Int cell);
    }
}
