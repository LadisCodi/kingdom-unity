using System;
using System.Collections.Generic;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Economy
{
    // A currency that fills by itself: one unit at a time while it is below its cap, nothing while it is at or
    // over it. Each unit lands at an absolute moment, so an absence replays exactly. Spending through it starts
    // the clock again; anything else may land over the cap (a reward does), and only the drip stops there.
    public abstract class Drip : ITimedSystem
    {
        private const double MS_PER_HOUR = 3_600_000;

        private readonly DripState _state;
        private readonly ITreasury _treasury;
        private readonly string _currency;

        protected Drip(DripState state, ITreasury treasury, string currency)
        {
            _state = state;
            _treasury = treasury;
            _currency = currency;
        }

        public double Amount => _treasury.Get(_currency);

        public abstract double Cap { get; }

        public abstract double PerHour { get; }

        // When the next unit lands; null while it is full.
        public double? NextUnitAt => _state.AccruingSince + UnitMs;

        // When it is full at this pace; `now` when it already is.
        public double FullAt(double now)
        {
            var missing = Math.Ceiling(Cap - Amount);
            if (missing <= 0 || NextUnitAt == null) return now;
            return NextUnitAt.Value + (missing - 1) * UnitMs;
        }

        protected double UnitMs => MS_PER_HOUR / PerHour;

        public bool TrySpend(double amount, double now)
        {
            if (Amount < amount) return false;

            _treasury.TryPay(new Dictionary<string, double> { [_currency] = amount });
            Wake(now);
            return true;
        }

        // Starts it filling from `now` if it is below its cap and was not already.
        public void Wake(double now)
        {
            if (_state.AccruingSince == null && Amount < Cap) _state.AccruingSince = now;
        }

        public double? NextBoundary(double after)
        {
            var next = NextUnitAt;
            return next > after ? next : null;
        }

        public void ApplyDue(double time)
        {
            var next = NextUnitAt;
            if (next == null || next > time) return;

            var room = Math.Max(0, Cap - Amount);
            if (room > 0) _treasury.Add(_currency, Math.Min(1, room));

            _state.AccruingSince = Amount < Cap ? next : null;
        }

        public void RunUntil(double time) { }
    }
}
