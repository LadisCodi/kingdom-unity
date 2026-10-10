using Codigames.Kingdom.Magic;
using Codigames.Modules.Clock;
using VContainer.Unity;

namespace Codigames.Game.Store
{
    // The video's offer latched on the live tick, once a second — never from a replayed absence.
    public class RefillLatch : ITickable
    {
        private readonly ManaRefills _refills;
        private readonly IClock _clock;
        private double _second = -1;

        public RefillLatch(ManaRefills refills, IClock clock)
        {
            _refills = refills;
            _clock = clock;
        }

        public void Tick()
        {
            var second = System.Math.Floor(_clock.NowMs / 1000.0);
            if (second == _second) return;
            _second = second;
            _refills.Refresh(_clock.NowMs);
        }
    }
}
