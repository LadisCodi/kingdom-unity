using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic;

namespace Codigames.Kingdom.Bag
{
    // What a boost changing does to the rest of the kingdom: the stores and the Mana pool are brought up to that
    // instant at the old rate, so the new one starts there — live or in a replayed absence.
    public class BoostEffects
    {
        private readonly Stores _stores;
        private readonly ManaPool _mana;

        public BoostEffects(Boosts boosts, Stores stores, ManaPool mana)
        {
            _stores = stores;
            _mana = mana;
            boosts.Changing += OnChanging;
        }

        private void OnChanging(double at)
        {
            _stores.SettleAll(at);
            _mana.ApplyDue(at);
        }
    }
}
