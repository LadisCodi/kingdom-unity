using System;
using System.Linq;
using Codigames.Kingdom.Bag.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Bag
{
    // The boosts running (Docs/proposals/inventory.md §3.3): one of a kind at a time — a second extends the first and a
    // stronger one lifts it, never stacking. Each ends at its own moment, a boundary like any other, so an absence
    // replays its end where it fell. Before any change, Changing says when, so what the boost raises is settled at
    // the old rate up to that instant.
    public class Boosts : ITimedSystem, IBoosts
    {
        private readonly BagState _state;

        public Boosts(BagState state) => _state = state;

        // A boost is about to start, change or end at this instant: settle what it raises.
        public event Action<double> Changing;

        public double Multiplier(BoostKind kind) => Running(kind)?.Multiplier ?? 1;

        public RunningBoost Running(BoostKind kind)
        {
            foreach (var boost in _state.Boosts)
                if (boost.Kind == kind) return boost;
            return null;
        }

        // Starts one at `now`, or extends the one running by its length and lifts it to the stronger multiplier.
        public void Start(BoostKind kind, double multiplier, double seconds, double now)
        {
            Changing?.Invoke(now);
            var running = Running(kind);
            if (running != null)
            {
                running.EndsAt = Math.Max(running.EndsAt, now) + seconds * 1000;
                running.Multiplier = Math.Max(running.Multiplier, multiplier);
                return;
            }

            _state.Boosts.Add(new RunningBoost { Kind = kind, Multiplier = multiplier, EndsAt = now + seconds * 1000 });
        }

        public double? NextBoundary(double after)
        {
            double? next = null;
            foreach (var boost in _state.Boosts)
                if (boost.EndsAt > after && (next == null || boost.EndsAt < next)) next = boost.EndsAt;
            return next;
        }

        public void ApplyDue(double time)
        {
            foreach (var boost in _state.Boosts.Where(b => b.EndsAt <= time).OrderBy(b => b.EndsAt).ToList())
            {
                Changing?.Invoke(boost.EndsAt);
                _state.Boosts.Remove(boost);
            }
        }

        public void RunUntil(double time) { }
    }
}
