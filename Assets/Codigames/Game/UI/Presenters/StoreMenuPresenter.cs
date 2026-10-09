using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Ads;
using Codigames.Game.Audio;
using Codigames.Game.Data.Heroes;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The store's logic (the web's storeSheet and storeHeroes, and Game.doPull/doPullMany/startFreePullWatch): the
    // tabs; on Heroes, the two calls — the ×1 one button with whichever face is true (free, an ad that pays for it, or a
    // key), the ×10 always a price — the free calls line ticking, the odds; and every call handed to the reveal. The
    // golden call stands a different Legendary each visit, every one before any comes round again.
    public class StoreMenuPresenter : AbstractDataMenuPresenter<StoreMenu, string>, IClosableMenuPresenter, IPurseMenu, ITickable
    {
        public const string HEROES = "heroes";
        private static readonly string[] TABS = { HEROES };

        private readonly UIManager _ui;
        private readonly Gacha _gacha;
        private readonly Reveals _reveals;
        private readonly HeroCollection _heroes;
        private readonly IItemHoldings _items;
        private readonly Doors _doors;
        private readonly RewardedAd _ads;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly List<string> _legendBag = new();

        private string _tab = HEROES;
        private string _legend;
        private double _shownSecond = -1;

        public StoreMenuPresenter(IMenuViewFactory views, UIManager ui, Gacha gacha, Reveals reveals, HeroCollection heroes, IItemHoldings items,
            Doors doors, RewardedAd ads, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _gacha = gacha;
            _reveals = reveals;
            _heroes = heroes;
            _items = items;
            _doors = doors;
            _ads = ads;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }

        // The keys, while the Heroes tab is open.
        public IReadOnlyList<string> Purse => _gacha.Banners.Select(b => b.Key).ToList();

        public void RequestClose() => _ = _ui.HideMenu<StoreMenu>();

        public void Tick()
        {
            if (View == null || !IsShown) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _shownSecond) return;
            _shownSecond = second;
            Refresh();
        }

        protected override void BindInternal(StoreMenu view)
        {
            _tab = string.IsNullOrEmpty(Data) ? HEROES : Data;
            NextLegend();
            view.HideOdds();
            view.SetCarousel(_heroes.Entries.OfType<HeroAsset>().Where(h => h.Art != null).Select(h => h.Art).ToList());
            Refresh();
            _gacha.Changed += Refresh;
        }

        protected override void UnbindInternal(StoreMenu view) => _gacha.Changed -= Refresh;

        protected override void SubscribeToViewEventsInternal(StoreMenu view)
        {
            view.CloseTapped += RequestClose;
            view.TabTapped += OnTab;
            view.OneTapped += OnOne;
            view.TenTapped += OnTen;
        }

        protected override void UnsubscribeFromViewEventsInternal(StoreMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.TabTapped -= OnTab;
            view.OneTapped -= OnOne;
            view.TenTapped -= OnTen;
        }

        private void OnTab(int index)
        {
            _tab = TABS[Math.Min(index, TABS.Length - 1)];
            Refresh();
        }

        // ---- the calls

        private void OnOne(int index)
        {
            var banner = _gacha.Banners[index].Id;
            var price = _gacha.PullPrice(banner);
            if (price.Amount > 0 && _gacha.FreePullAvailable(banner, _clock.NowMs))
            {
                // The ad pays for it; the call is made when its reward is claimed.
                _ads.Watch(() =>
                {
                    if (_gacha.ClaimFreePull(banner, _clock.NowMs, out var free) == FreePullOutcome.Pulled) Reveal(banner, new[] { free });
                });
                return;
            }

            var pull = _gacha.Pull(banner);
            if (pull.Outcome != PullOutcome.Pulled)
            {
                _sounds.Play(SoundIds.ERROR);
                return;
            }

            Reveal(banner, new[] { pull });
        }

        private void OnTen(int index)
        {
            var banner = _gacha.Banners[index].Id;
            if (_gacha.PullMany(banner, 10, out var pulls) != PullOutcome.Pulled)
            {
                _sounds.Play(SoundIds.ERROR);
                return;
            }

            Reveal(banner, pulls);
        }

        private void Reveal(string banner, IReadOnlyList<PullResult> pulls)
        {
            var reveal = _reveals.Open(banner, pulls);
            if (reveal.Prizes.Count > 0) _ = _ui.ShowMenu<RevealScreen, Kingdom.Heroes.Reveal>(reveal);
        }

        // ---- the screen

        private void Refresh()
        {
            if (View == null) return;
            View.ShowTabs(TABS.Select(t => (_localizer.Tr("Heroes"), t == _tab, News())).ToList());
            if (!_doors.IsOpen(DoorId.Banner))
            {
                View.ShowLocked(_localizer.Tr("Build a Tavern to call heroes."));
                return;
            }

            var now = _clock.NowMs;
            View.ShowHeroes(_localizer.Tr("Call for aid"), _localizer.Tr("Odds"), Odds(), _gacha.Banners.Select(b => Banner(b, now)).ToList());
        }

        // A dot on the Heroes tab: a free call to take.
        private bool News() => _doors.IsOpen(DoorId.Banner)
            && _gacha.Banners.Any(b => _gacha.PullPrice(b.Id).Amount == 0 || _gacha.FreePullAvailable(b.Id, _clock.NowMs));

        private BannerPanelData Banner(IBannerDefinition banner, double now)
        {
            var price = _gacha.PullPrice(banner.Id);
            var held = _items.Count(price.Key);
            CallButtonData one;
            if (price.Amount == 0) one = new CallButtonData { Note = _localizer.Tr("Free"), Label = _localizer.Tr("Call") };
            else if (_gacha.FreePullAvailable(banner.Id, now))
                one = new CallButtonData { Note = _localizer.Tr("Free"), Label = "<sprite name=\"video\"> " + _localizer.Tr("Call") };
            else
                one = new CallButtonData { Price = new[] { Key(price.Key, price.Amount, held) }, Label = _localizer.Tr("Call"), Enabled = held >= price.Amount };

            // A free first call is free once, so a ten over it costs nine.
            var ten = price.Amount == 0 ? 9 : price.Amount * 10;
            return new BannerPanelData
            {
                Name = banner.Id == Gacha.STANDARD ? _localizer.Tr("The common call") : _localizer.Tr("The golden call"),
                Golden = banner.ShowsHero,
                Hero = banner.ShowsHero && _legend != null ? _heroes.Get<HeroAsset>(_legend).Art : null,
                Free = FreeLine(banner, now),
                One = one,
                Ten = new CallButtonData { Price = new[] { Key(price.Key, ten, held) }, Label = _localizer.Tr("Call ×10"), Enabled = held >= ten },
            };
        }

        private PriceTerm Key(string key, int amount, int held) => new(key, _numbers.Count(amount), held < amount);

        // How many free calls today, or when the next one is.
        private string FreeLine(IBannerDefinition banner, double now)
        {
            if (banner.FreePerDay == 0) return string.Empty;
            var left = _gacha.FreePullsLeft(banner.Id, now);
            if (left == 0) return _localizer.Tr("Free calls tomorrow");
            if (_gacha.FreePullAvailable(banner.Id, now)) return _localizer.Tr("Free calls today: {n}", ("n", _numbers.Count(left)));
            return _localizer.Tr("Free calls today: {n} · next in {time}", ("n", _numbers.Count(left)),
                ("time", "<b>" + _numbers.Countdown(Math.Max(0, Math.Ceiling((_gacha.FreePullReadyAt(banner.Id) - now) / 1000))) + "</b>"));
        }

        // How soon a hero, and a Legendary, comes on each call.
        private string Odds()
            => string.Join(". ", _gacha.Banners.Select(b =>
            {
                var name = b.Id == Gacha.STANDARD ? _localizer.Tr("The common call") : _localizer.Tr("The golden call");
                var chance = _numbers.Count(Math.Round(_gacha.HeroChanceAt(b.Id) * 100, MidpointRounding.AwayFromZero));
                var calls = _numbers.Count(_gacha.PullsToGuarantee(b.Id));
                var legend = _gacha.PullsToLegendary(b.Id);
                return legend == null
                    ? _localizer.Tr("{banner}: {chance}% a hero now, one within {calls}", ("banner", name), ("chance", chance), ("calls", calls))
                    : _localizer.Tr("{banner}: {chance}% a hero now, one within {calls}, a legend within {legend}", ("banner", name), ("chance", chance),
                        ("calls", calls), ("legend", _numbers.Count(legend.Value)));
            })) + ".";

        // What the golden call stands this visit: the next Legendary in a shuffled bag.
        private void NextLegend()
        {
            if (_legendBag.Count == 0)
            {
                _legendBag.AddRange(_heroes.Items.Where(h => h.Rarity == HeroRarity.Legendary).Select(h => h.Id));
                for (var i = _legendBag.Count - 1; i > 0; i--)
                {
                    var j = UnityEngine.Random.Range(0, i + 1);
                    (_legendBag[i], _legendBag[j]) = (_legendBag[j], _legendBag[i]);
                }

                if (_legendBag.Count > 1 && _legendBag[0] == _legend) (_legendBag[0], _legendBag[1]) = (_legendBag[1], _legendBag[0]);
            }

            if (_legendBag.Count == 0) return;
            _legend = _legendBag[0];
            _legendBag.RemoveAt(0);
        }
    }
}
