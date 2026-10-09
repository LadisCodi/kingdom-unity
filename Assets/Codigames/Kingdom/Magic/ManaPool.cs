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
        private readonly IManaSources _sources;

        public ManaPool(ManaState state, ITreasury treasury, IManaSettings settings, IBonuses bonuses = null, IManaSources sources = null)
            : base(state, treasury, MANA)
        {
            _settings = settings;
            _bonuses = bonuses;
            _sources = sources;
        }

        // The base and everything that raises it, then the tree's share of the whole.
        public override double Cap
            => System.Math.Round((_settings.BaseCap + (_sources?.ExtraCap ?? 0)) * _bonuses.Multiplier(TechStats.MANA_CAP),
                System.MidpointRounding.AwayFromZero);

        public override double PerHour => (_settings.BasePerHour + (_sources?.ExtraPerHour ?? 0)) * _bonuses.Multiplier(TechStats.MANA_REGEN);
    }
}
