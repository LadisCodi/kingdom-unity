using Codigames.Kingdom.Store;
using Codigames.Modules.Clock;
using VContainer.Unity;

namespace Codigames.Game.Store
{
    // The offers' latch on the live tick, once a second: an offer is an opportunity shown to a player, so only the live
    // game opens one — never a replayed absence.
    public class OfferLatch : IStartable, ITickable
    {
        private readonly Offers _offers;
        private readonly Kingdom.Store.Store _store;
        private readonly IClock _clock;
        private double _second = -1;

        public OfferLatch(Offers offers, Kingdom.Store.Store store, IClock clock)
        {
            _offers = offers;
            _store = store;
            _clock = clock;
        }

        public void Start() => _store.UseOffers(_offers);

        public void Tick()
        {
            var second = System.Math.Floor(_clock.NowMs / 1000.0);
            if (second == _second) return;
            _second = second;
            _offers.Refresh(_clock.NowMs);
        }
    }
}
