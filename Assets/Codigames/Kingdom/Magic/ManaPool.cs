using System;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic.State;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Magic
{
    // The Mana every tap on the ground is paid from: a pool with a cap that gains one unit at a time while it is
    // below it, and stops while it is full. Each unit lands at an absolute moment, so an absence replays exactly.
    public class ManaPool : ITimedSystem
    {
        public const string MANA = "Mana";

        private const double MS_PER_HOUR = 3_600_000;

        private readonly ManaState _state;
        private readonly ITreasury _treasury;
        private readonly IManaSettings _settings;

        public ManaPool(ManaState state, ITreasury treasury, IManaSettings settings)
        {
            _state = state;
            _treasury = treasury;
            _settings = settings;
        }

        public double Amount => _treasury.Get(MANA);

        public double Cap => _settings.BaseCap;

        public double PerHour => _settings.BasePerHour;

        // When the next unit lands; null while the pool is full.
        public double? NextUnitAt => _state.AccruingSince + UnitMs;

        private double UnitMs => MS_PER_HOUR / PerHour;

        public bool TrySpend(double amount, double now)
        {
            if (Amount < amount) return false;

            _treasury.TryPay(new System.Collections.Generic.Dictionary<string, double> { [MANA] = amount });
            Wake(now);
            return true;
        }

        // Starts the pool gaining from `now` if it is below its cap and was not already.
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
            if (room > 0) _treasury.Add(MANA, Math.Min(1, room));

            _state.AccruingSince = Amount < Cap ? next : null;
        }

        public void RunUntil(double time) { }
    }
}
