using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Magic;

namespace Codigames.Kingdom.Research
{
    // What a technology completing does to the kingdom beyond its own record. The stores count what they made
    // at the old rates up to that moment, so a raise never reprices time already worked; afterwards every
    // building looks again with the sight it now has, and a pool or a store a raise left below its new cap
    // starts filling.
    public class ResearchEffects
    {
        private readonly Stores _stores;
        private readonly FogOfWar _fog;
        private readonly ManaPool _mana;
        private double _at;

        public ResearchEffects(Researching research, Stores stores, FogOfWar fog, ManaPool mana)
        {
            _stores = stores;
            _fog = fog;
            _mana = mana;

            research.Completing += OnCompleting;
            research.Researched += OnResearched;
        }

        private void OnCompleting(string id, double now)
        {
            _at = now;
            _stores.SettleAll(now);
        }

        private void OnResearched(string id)
        {
            _fog.RevealAroundAll();
            _mana.Wake(_at);
            _stores.WakeAll(_at);
        }
    }
}
