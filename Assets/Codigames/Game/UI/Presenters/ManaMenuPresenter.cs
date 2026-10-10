using System;
using System.Collections.Generic;
using Codigames.Game.Ads;
using Codigames.Game.Audio;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The Mana sheet's logic (the web's manaSheet + Game.manaRefills/doRefillMana/startAdWatch): every refusal says which
    // one it is — a spent allowance by the count over its own button, the rest under the pair. Closing never consumes a
    // standing offer; only claiming does.
    public class ManaMenuPresenter : AbstractMenuPresenter<ManaMenu>, IPopupMenuPresenter, ITickable
    {
        private const string GEMS = "Gems";

        private readonly UIManager _ui;
        private readonly ManaPool _mana;
        private readonly ManaRefills _refills;
        private readonly IRefillSettings _settings;
        private readonly ITreasury _treasury;
        private readonly RewardedAd _ads;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private double _second = -1;

        public ManaMenuPresenter(IMenuViewFactory views, UIManager ui, ManaPool mana, ManaRefills refills, IRefillSettings settings, ITreasury treasury,
            RewardedAd ads, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _mana = mana;
            _refills = refills;
            _settings = settings;
            _treasury = treasury;
            _ads = ads;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<ManaMenu>();

        public void Tick()
        {
            if (View == null || !IsShown) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _second) return;
            _second = second;
            Refresh();
        }

        protected override void BindInternal(ManaMenu view) => Refresh();

        protected override void SubscribeToViewEventsInternal(ManaMenu view)
        {
            view.CloseTapped += RequestClose;
            view.GemsTapped += OnGems;
            view.AdTapped += OnAd;
        }

        protected override void UnsubscribeFromViewEventsInternal(ManaMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.GemsTapped -= OnGems;
            view.AdTapped -= OnAd;
        }

        private void Refresh()
        {
            var now = _clock.NowMs;
            var value = _mana.Amount;
            var cap = _mana.Cap;
            var status = value > cap ? _localizer.Tr("Overcharged — {n} past the ceiling", ("n", _numbers.Exact(value - cap)))
                : value >= cap ? _localizer.Tr("Full — anything more is spilling")
                : _localizer.Tr("Full in about {time}", ("time", _numbers.Duration((cap - value) / Math.Max(1, _mana.PerHour) * 3600)));
            var cost = _refills.GemCost(now);
            var full = value >= cap;
            var gemsWhy = _refills.BoughtLeft(now) <= 0 ? null : full ? _localizer.Tr("Your pool is already full") : null;
            View.Show(new ManaSheetData
            {
                Title = _localizer.Tr("Mana"),
                Name = _localizer.Tr("Mana"),
                Status = status,
                Fill = (float)(cap > 0 ? value / cap : 0),
                Gauge = _numbers.Exact(Math.Floor(value)) + " / " + _numbers.Exact(cap),
                Regen = _localizer.Tr("Drawn from the land") + " · <b>+" + _numbers.Exact(_mana.PerHour) + "/h</b>",
                Note = _localizer.Tr("Every tap is paid from the pool."),
                Prize = _localizer.Tr("Refill now — a whole pool, on top of what you have"),
                Amount = "<sprite name=\"Mana\"> <b>+" + _numbers.Exact(_refills.Reward) + "</b>",
                GemsLeft = _localizer.Tr("Left today: {left}/{max}", ("left", _numbers.Exact(_refills.BoughtLeft(now))),
                    ("max", _numbers.Exact(_settings.GemRefillCosts.Count))),
                GemsNote = cost == null ? string.Empty : _localizer.Tr("the {nth} today", ("nth", Ordinal(_refills.NextRung(now)))),
                GemsLabel = _localizer.Tr("Refill"),
                GemsPrice = cost == null ? Array.Empty<PriceTerm>() : new[] { new PriceTerm(GEMS, _numbers.Count(cost.Value), _treasury.Get(GEMS) < cost.Value) },
                GemsEnabled = cost != null && !full,
                GemsWhy = gemsWhy,
                AdLeft = _localizer.Tr("Left today: {left}/{max}", ("left", _numbers.Exact(_refills.WatchedLeft(now))), ("max", _numbers.Exact(_settings.RefillsPerDay))),
                AdNote = _localizer.Tr("a short ad"),
                AdLabel = _localizer.Tr("Watch"),
                AdWhy = VideoWhy(now, full),
            });
        }

        // Why the video cannot be watched now, or null when it can. A spent allowance is said by its count.
        private string VideoWhy(double now, bool full)
        {
            if (_refills.WatchedLeft(now) <= 0) return _localizer.Tr("All {n} taken today — back at midnight", ("n", _numbers.Exact(_settings.RefillsPerDay)));
            if (_refills.Pending) return null;
            if (full) return _localizer.Tr("Your pool is already full");
            if (!_refills.Eligible) return _localizer.Tr("The video is offered once you are below half a pool");
            return _localizer.Tr("The next video is on its way");
        }

        private string Ordinal(int n)
        {
            if (n % 10 == 1 && n % 100 != 11) return _localizer.Tr("{n}st", ("n", n));
            if (n % 10 == 2 && n % 100 != 12) return _localizer.Tr("{n}nd", ("n", n));
            if (n % 10 == 3 && n % 100 != 13) return _localizer.Tr("{n}rd", ("n", n));
            return _localizer.Tr("{n}th", ("n", n));
        }

        private void OnGems()
        {
            var result = _refills.RefillWithGems(_clock.NowMs);
            _sounds.Play(result == RefillResult.Refilled ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            Refresh();
        }

        private async void OnAd()
        {
            if (!_refills.Pending) return;
            await _ui.HideMenu<ManaMenu>();
            WatchFor(_ads, _refills, _clock, _sounds);
        }

        // The video, and its reward: a whole pool.
        public static void WatchFor(RewardedAd ads, ManaRefills refills, IClock clock, ISoundService sounds)
            => ads.Watch(() =>
            {
                if (refills.ClaimAd(clock.NowMs) == ClaimAdResult.Claimed) sounds.Play(SoundIds.QUEST_COMPLETE);
            });
    }
}
