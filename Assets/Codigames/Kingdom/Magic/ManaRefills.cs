using System;
using System.Collections.Generic;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic.State;
using Codigames.Modules.Randomness;

namespace Codigames.Kingdom.Magic
{
    public enum RefillResult
    {
        Refilled,
        AlreadyFull,
        NotEnoughGems,
        NoneLeft,
    }

    public enum ClaimAdResult
    {
        Claimed,
        NoOffer,
        NoneLeftToday,
    }

    // THE REFILL (the web's manaRefill.ts and adOffers.ts): a whole pool, on top of whatever is banked, two ways — a
    // video, a few a day, offered once the pool is below a share and a cooldown has passed; or Gems, on a ladder whose
    // rung rises with every pool bought today. The two counts are separate and roll at UTC midnight, lazily: every reader
    // rolls the ledger onto today. Not a boundary of the timeline — nothing in the sim reads them, and the offer is
    // latched by the live tick only. The cooldown is drawn from the claims counter, the event, never the clock.
    public class ManaRefills
    {
        private const double DAY_MS = 86_400_000;
        private const string GEMS = "Gems";

        private readonly AdsState _state;
        private readonly ManaPool _mana;
        private readonly IRefillSettings _settings;
        private readonly ITreasury _treasury;
        private readonly uint _seed;

        public ManaRefills(AdsState state, ManaPool mana, IRefillSettings settings, ITreasury treasury, uint seed)
        {
            _state = state;
            _mana = mana;
            _settings = settings;
            _treasury = treasury;
            _seed = seed;
        }

        public event Action Changed;

        private static long Day(double now) => (long)Math.Floor(now / DAY_MS);

        private (int Watched, int Bought) Counts(double now) => _state.RefillDay == Day(now) ? (_state.Watched, _state.Bought) : (0, 0);

        private void Roll(double now)
        {
            var today = Day(now);
            if (_state.RefillDay == today) return;
            _state.RefillDay = today;
            _state.Watched = 0;
            _state.Bought = 0;
        }

        // A whole pool, on top of what is banked.
        public double Reward => _mana.Cap;

        public int WatchedLeft(double now) => Math.Max(0, _settings.RefillsPerDay - Counts(now).Watched);
        public int BoughtLeft(double now) => Math.Max(0, _settings.GemRefillCosts.Count - Counts(now).Bought);
        public int NextRung(double now) => Counts(now).Bought + 1;

        // The next pool's Gems today, or null when the ladder is spent.
        public double? GemCost(double now)
        {
            var i = Counts(now).Bought;
            return i < _settings.GemRefillCosts.Count ? _settings.GemRefillCosts[i] : null;
        }

        public RefillResult RefillWithGems(double now)
        {
            if (GemCost(now) is not { } cost) return RefillResult.NoneLeft;
            if (_mana.Amount >= _mana.Cap) return RefillResult.AlreadyFull;
            if (!_treasury.TryPay(new Dictionary<string, double> { [GEMS] = cost })) return RefillResult.NotEnoughGems;
            Grant();
            Roll(now);
            _state.Bought++;
            Changed?.Invoke();
            return RefillResult.Refilled;
        }

        // ---- the video's offer

        public bool Pending => _state.Pending;

        // Short enough for an offer to be a kindness rather than an interruption.
        public bool Eligible => _mana.Amount < _mana.Cap * _settings.EligibleBelowFraction;

        public double ReadyAt => _state.ReadyAt;

        // The latch, from the live tick. An offer up stays up until claimed.
        public void Refresh(double now)
        {
            if (_state.Pending || WatchedLeft(now) <= 0 || now < _state.ReadyAt || !Eligible) return;
            _state.Pending = true;
            Changed?.Invoke();
        }

        // Take the reward and start the next cooldown, drawn on the claims counter.
        public ClaimAdResult ClaimAd(double now)
        {
            if (!_state.Pending) return ClaimAdResult.NoOffer;
            if (WatchedLeft(now) <= 0) return ClaimAdResult.NoneLeftToday;
            Grant();
            _state.Pending = false;
            _state.Claims++;
            Roll(now);
            _state.Watched++;
            var roll = Rand.Value(_seed, "adOffer", _state.Claims);
            _state.ReadyAt = now + Math.Round((_settings.CooldownMinSeconds + roll * (_settings.CooldownMaxSeconds - _settings.CooldownMinSeconds)) * 1000);
            Changed?.Invoke();
            return ClaimAdResult.Claimed;
        }

        private void Grant() => _treasury.Add(ManaPool.MANA, _mana.Cap);
    }
}
