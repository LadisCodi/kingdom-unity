using System;
using Codigames.Kingdom.Battles;

namespace Codigames.Game.Battles
{
    public enum PlaybackPhase
    {
        Playing,
        // The fight is over; the verdict comes down.
        Result,
        // The way out is offered.
        Done,
    }

    // A fight that has already happened, replayed (Docs/features/combat.md §13): the log, what it is called, and the
    // screen's clock over it — real time at a speed, held for a heavy blow, slowed for the last. The clock is the
    // screen's alone: the log never moves, only when it is shown.
    public sealed class BattlePlayback
    {
        // How long, in ms of the fight's clock, the verdict stands before the way out is offered.
        public const double RESULT_DELAY_MS = 2000;

        private double _clockAt;
        private double _clockMs;
        private (double From, double Until, double Factor)? _slow;

        public BattlePlayback(BattleLog log, string title, string subtitle, int tickMs, double now, double speed)
        {
            Log = log;
            Title = title;
            Subtitle = subtitle;
            TickMs = tickMs;
            Speed = speed;
            _clockAt = now;
        }

        // The enemy's squads wear a lair's creatures rather than the player's own soldiers.
        public bool Creatures { get; set; }

        public BattleLog Log { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public int TickMs { get; }
        public double Speed { get; private set; }
        public PlaybackPhase Phase { get; private set; } = PlaybackPhase.Playing;
        public double EndMs => Log.Ticks * (double)TickMs;

        // Milliseconds of the fight shown at `now`; past the end it stays at the end.
        public double Ms(double now) => Math.Min(EndMs, Raw(now));

        public void SetSpeed(double speed, double now)
        {
            _clockMs = Raw(now);
            _clockAt = now;
            Speed = speed;
        }

        // Straight to the end.
        public void Skip(double now)
        {
            if (Phase != PlaybackPhase.Playing) return;
            _clockMs = EndMs;
            _clockAt = now;
            _slow = null;
            Advance(now);
        }

        // Freezes the replay for `ms` of real time: the weight of a heavy blow.
        public void Hold(double ms, double now)
        {
            if (Phase != PlaybackPhase.Playing) return;
            _clockMs = Raw(now);
            _clockAt = now + ms;
        }

        // Runs it `factor` times slower for the next `ms` of real time: the last blow, in slow motion.
        public void Slow(double factor, double ms, double now)
        {
            if (Phase != PlaybackPhase.Playing) return;
            _clockMs = Raw(now);
            _clockAt = Math.Max(now, _clockAt);
            _slow = (_clockAt, _clockAt + ms, factor);
        }

        // Moves the phase on to what the clock says; true when it moved.
        public bool Advance(double now)
        {
            var elapsed = Raw(now);
            var moved = false;
            if (Phase == PlaybackPhase.Playing && elapsed >= EndMs)
            {
                Phase = PlaybackPhase.Result;
                moved = true;
            }

            if (Phase == PlaybackPhase.Result && elapsed >= EndMs + RESULT_DELAY_MS)
            {
                Phase = PlaybackPhase.Done;
                moved = true;
            }

            return moved;
        }

        // The clock where it was last set, plus the real time since at its speed, less what a slow window held back.
        private double Raw(double now)
        {
            var run = Math.Max(0, now - _clockAt);
            var slowed = _slow is { } slow ? Math.Max(0, Math.Min(now, slow.Until) - Math.Max(slow.From, _clockAt)) * (1 - slow.Factor) : 0;
            return _clockMs + (run - slowed) * Speed;
        }
    }
}
