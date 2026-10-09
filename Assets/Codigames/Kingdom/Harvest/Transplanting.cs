using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Harvest
{
    // Moving a tree or a crop plot (Docs/features/27-plantables.md §4): picked up from where it stands and put down
    // on any revealed land a building could stand on, where it grows again from nothing for its whole growth —
    // whatever it held — and the cell it left is bare ground. That wait is the whole price. Put back where it
    // started, it is a cancel. A crop plot moves from the start; a tree once Transplanting is researched.
    public class Transplanting
    {
        public const string TRANSPLANTING = "Transplanting";
        private const string TREES = "Trees";

        private static readonly HashSet<string> MOVABLE = new() { TREES, "Crops" };

        private readonly GroundState _ground;
        private readonly Harvesting _harvesting;
        private readonly Placement _placement;
        private readonly IRevealedGround _revealed;
        private readonly IResearchGates _gates;

        public Transplanting(GroundState ground, Harvesting harvesting, Placement placement, IRevealedGround revealed, IResearchGates gates = null)
        {
            _ground = ground;
            _harvesting = harvesting;
            _placement = placement;
            _revealed = revealed;
            _gates = gates;
        }

        // A feature moved: from, to.
        public event Action<Vector2Int, Vector2Int> Moved;

        // Why what stands on the cell cannot be picked up; None when it can.
        public TransplantRefusal PickUpRefusal(Vector2Int cell)
        {
            if (!_ground.Features.TryGetValue(cell, out var feature) || !MOVABLE.Contains(feature)) return TransplantRefusal.NotMovable;
            if (!_revealed.IsRevealed(cell)) return TransplantRefusal.NotRevealed;
            if (feature == TREES && _gates != null && !_gates.IsOpen(TRANSPLANTING)) return TransplantRefusal.NeedsResearch;
            return TransplantRefusal.None;
        }

        // Why the feature lifted from `from` may not be put down on `to`; None when it may.
        public PlacementProblem LandingProblem(Vector2Int from, Vector2Int to) => _placement.FeatureProblem(to, from);

        // How long it grows where it lands, in seconds.
        public double GrowSeconds(Vector2Int cell)
            => _ground.Features.TryGetValue(cell, out var feature) ? _harvesting.GrowSecondsOf(feature) : 0;

        public TransplantRefusal Move(Vector2Int from, Vector2Int to, double now)
        {
            var refusal = PickUpRefusal(from);
            if (refusal != TransplantRefusal.None) return refusal;
            if (from == to) return TransplantRefusal.None;
            if (LandingProblem(from, to) != PlacementProblem.None) return TransplantRefusal.Placement;

            var feature = _harvesting.Lift(from);
            _harvesting.Plant(feature, to, now);
            Moved?.Invoke(from, to);
            return TransplantRefusal.None;
        }
    }
}
