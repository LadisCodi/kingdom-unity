using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic.State;

namespace Codigames.Kingdom.Magic
{
    // The Mana every tap on the ground is paid from: a pool with a cap that gains one unit at a time while it is
    // below it, and stops while it is full.
    public class ManaPool : Drip
    {
        public const string MANA = "Mana";

        private readonly IManaSettings _settings;

        public ManaPool(ManaState state, ITreasury treasury, IManaSettings settings)
            : base(state, treasury, MANA)
        {
            _settings = settings;
        }

        public override double Cap => _settings.BaseCap;

        public override double PerHour => _settings.BasePerHour;
    }
}
