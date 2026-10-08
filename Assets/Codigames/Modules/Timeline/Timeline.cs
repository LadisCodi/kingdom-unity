using System;
using System.Collections.Generic;

namespace Codigames.Modules.Timeline
{
    // Advances every registered system to a moment, walking from boundary to boundary: discrete work is
    // applied exactly at the moment it falls due, continuous change runs between. Boundaries are absolute
    // times, so one long advance (an absence replayed) and many short ones (live play) end in the same state.
    public class Timeline
    {
        // A seatbelt, not a design limit: a window with this many real boundaries is a content bug.
        public const int MAX_STEPS = 10_000;

        private readonly List<ITimedSystem> _systems = new();

        public Timeline(double lastAdvance)
        {
            LastAdvance = lastAdvance;
        }

        // Where the last advance left off.
        public double LastAdvance { get; private set; }

        // Systems run in registration order at every step.
        public void Register(ITimedSystem system) => _systems.Add(system);

        // Returns the number of boundaries crossed.
        public int Advance(double toTime)
        {
            var cursor = Math.Min(LastAdvance, toTime);
            var steps = 0;

            for (; steps < MAX_STEPS; steps++)
            {
                foreach (var system in _systems) system.ApplyDue(cursor);

                var next = NextBoundary(cursor);
                if (!next.HasValue || next.Value > toTime) break;

                foreach (var system in _systems) system.RunUntil(next.Value);
                cursor = next.Value;
            }

            foreach (var system in _systems) system.RunUntil(toTime);
            LastAdvance = toTime;
            return steps;
        }

        private double? NextBoundary(double after)
        {
            double? earliest = null;

            foreach (var system in _systems)
            {
                var at = system.NextBoundary(after);
                if (at.HasValue && at.Value > after && (!earliest.HasValue || at.Value < earliest.Value)) earliest = at;
            }

            return earliest;
        }
    }
}
