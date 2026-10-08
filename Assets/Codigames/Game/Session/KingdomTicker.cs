using Codigames.Kingdom;
using Codigames.Kingdom.Economy;
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
        private readonly KingdomState _state;
        private readonly ITreasury _treasury;

        public KingdomTicker(Timeline timeline, IClock clock, KingdomState state, ITreasury treasury)
        {
            _timeline = timeline;
            _clock = clock;
            _state = state;
            _treasury = treasury;
        }

        public void Tick()
        {
            _timeline.Advance(_clock.NowMs);
            _state.LastAdvance = _timeline.LastAdvance;
            _state.Balances = new System.Collections.Generic.Dictionary<string, double>(_treasury.Balances);
        }
    }
}
