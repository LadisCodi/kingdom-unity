using Codigames.Modules.Clock;
using Codigames.Modules.Timeline;
using VContainer.Unity;

namespace Codigames.Game.Session
{
    // The one tick driver: reads the clock and advances the kingdom to now. Nothing else advances it.
    public class KingdomTicker : ITickable
    {
        private readonly Timeline _timeline;
        private readonly IClock _clock;

        public KingdomTicker(Timeline timeline, IClock clock)
        {
            _timeline = timeline;
            _clock = clock;
        }

        public void Tick() => _timeline.Advance(_clock.NowMs);
    }
}
