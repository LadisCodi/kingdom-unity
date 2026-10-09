using System.Collections.Generic;

namespace Codigames.Kingdom.Tutorial
{
    // Every reader asked in turn; the first that answers decides. `tap` is the stage's own event and never holds,
    // `always` always does, and a kind nobody answers never holds.
    public class Conditions : IConditions
    {
        private readonly IReadOnlyList<IConditionReader> _readers;

        public Conditions(IReadOnlyList<IConditionReader> readers) => _readers = readers;

        public bool Holds(Condition condition, int tapsAtStart = 0)
        {
            switch (condition.Kind)
            {
                case ConditionKind.Tap:
                case ConditionKind.Unknown: return false;
                case ConditionKind.Always: return true;
            }

            foreach (var reader in _readers)
                if (reader.TryHolds(condition, tapsAtStart, out var holds)) return holds;
            return false;
        }
    }
}
