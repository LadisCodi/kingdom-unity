using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic.State;
using Codigames.Kingdom.Research;

namespace Codigames.Kingdom.Magic
{
    // The Mana every tap on the ground is paid from: a pool with a cap that gains one unit at a time while it is
    // below it, and stops while it is full.
    public class ManaPool : Drip
    {
        public const string MANA = "Mana";

        private readonly IManaSettings _settings;
        private readonly IBonuses _bonuses;

        public ManaPool(ManaState state, ITreasury treasury, IManaSettings settings, IBonuses bonuses = null)
            : base(state, treasury, MANA)
        {
            _settings = settings;
            _bonuses = bonuses;
        }

        public override double Cap => System.Math.Round(_settings.BaseCap * _bonuses.Multiplier(TechStats.MANA_CAP), System.MidpointRounding.AwayFromZero);

        public override double PerHour => _settings.BasePerHour * _bonuses.Multiplier(TechStats.MANA_REGEN);
    }
}
