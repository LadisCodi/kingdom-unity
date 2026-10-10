using System;
using System.Collections.Generic;
using System.Linq;

namespace Codigames.Kingdom.Modifiers
{
    // The kingdom's modifier stack (the web's modifiers.ts resolve): every active term on a stat, its adds summed and its
    // multipliers multiplied — (base + Σ add) × Π mul — in id order, so the same stack always resolves to the same number.
    // A term is active while it has no expiry or the kingdom's last advance is before it: never the wall clock.
    public class ModifierStack : IModifiers
    {
        private readonly IReadOnlyList<IModifierSource> _sources;
        private readonly Func<double> _lastAdvance;

        public ModifierStack(IEnumerable<IModifierSource> sources, Func<double> lastAdvance)
        {
            _sources = sources.ToList();
            _lastAdvance = lastAdvance;
        }

        public double Resolve(string stat, double value, string scope = null)
        {
            var t = _lastAdvance();
            // Most numbers have no term on them, and one has at most one: those need no sort, nor the list it takes.
            Modifier only = null;
            var count = 0;
            for (var i = 0; i < _sources.Count; i++)
            foreach (var m in _sources[i].Modifiers)
            {
                if (!Active(m, stat, scope, t)) continue;
                only = m;
                count++;
            }

            if (count == 0) return value;
            if (count == 1) return only.Op == ModifierOp.Add ? value + only.Value : value * only.Value;

            double add = 0, mul = 1;
            foreach (var m in _sources.SelectMany(s => s.Modifiers)
                         .Where(m => Active(m, stat, scope, t))
                         .OrderBy(m => m.Id, StringComparer.Ordinal))
            {
                if (m.Op == ModifierOp.Add) add += m.Value;
                else mul *= m.Value;
            }

            return (value + add) * mul;
        }

        private static bool Active(Modifier m, string stat, string scope, double t)
            => m.Stat == stat && (m.Scope == null || m.Scope == scope) && (m.ExpiresAt == null || t < m.ExpiresAt);
    }
}
