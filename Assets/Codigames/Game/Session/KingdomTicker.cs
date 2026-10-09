using System;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Clock;
using Codigames.Modules.Timeline;
using VContainer.Unity;

namespace Codigames.Game.Session
{
    // The one tick driver: reads the clock and advances the kingdom to now. Nothing else advances it. It also hands
    // the kingdom the device's offset from UTC — the rules have no clock of their own, and a lair raids inside the
    // player's local day — after the advance, so a raid due before the move lands where it was due.
    public class KingdomTicker : ITickable
    {
        private readonly Timeline _timeline;
        private readonly IClock _clock;
        private readonly Codigames.Kingdom.Lairs.Lairs _lairs;

        public KingdomTicker(Timeline timeline, IClock clock, Codigames.Kingdom.Lairs.Lairs lairs)
        {
            _timeline = timeline;
            _clock = clock;
            _lairs = lairs;
        }

        public void Tick()
        {
            var now = _clock.NowMs;
            _timeline.Advance(now);
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds((long)now));
            _lairs.SetUtcOffset((int)Math.Round(offset.TotalMinutes), now);
        }
    }
}
