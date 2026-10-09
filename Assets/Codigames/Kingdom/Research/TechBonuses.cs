using System.Collections.Generic;
using Codigames.Kingdom.Research.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Research
{
    // Every researched technology's effects, folded by number and aim. Two rules keep the sums identical on
    // every device: a percent is divided per effect before it is added, and the fold walks the tree's own order,
    // never the order things were researched in (float addition is not associative).
    public class TechBonuses : IBonuses
    {
        private readonly ResearchState _state;
        private readonly ICatalog<ITechnology> _technologies;
        private readonly Dictionary<(string, TargetKind, string), BonusTotals> _byKey = new();

        // Research only ever adds, so the count identifies a fold.
        private int _foldedCount = -1;

        public TechBonuses(ResearchState state, ICatalog<ITechnology> technologies)
        {
            _state = state;
            _technologies = technologies;
        }

        public BonusTotals Totals(string stat, TargetKind target = TargetKind.Global, string targetId = null)
        {
            Fold();
            var global = _byKey.TryGetValue((stat, TargetKind.Global, null), out var all) ? all : BonusTotals.None;
            if (target == TargetKind.Global) return global;

            return _byKey.TryGetValue((stat, target, targetId), out var aimed) ? global + aimed : global;
        }

        private void Fold()
        {
            if (_foldedCount == _state.Completed.Count) return;

            _byKey.Clear();
            var done = new HashSet<string>(_state.Completed);
            foreach (var tech in _technologies.Items)
            {
                if (!done.Contains(tech.Id)) continue;

                foreach (var effect in tech.Effects)
                {
                    var key = (effect.Stat, effect.Target, effect.Target == TargetKind.Global ? null : effect.TargetId);
                    var totals = _byKey.TryGetValue(key, out var sum) ? sum : BonusTotals.None;
                    _byKey[key] = totals + (effect.Op == EffectOp.Flat
                        ? new BonusTotals(effect.Value, 0)
                        : new BonusTotals(0, effect.Value / 100));
                }
            }

            _foldedCount = _state.Completed.Count;
        }
    }
}
