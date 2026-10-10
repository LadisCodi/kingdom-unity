using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Store;
using Codigames.Game.Store;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Hud;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Kingdom.Heroes;
using Codigames.Kingdom.Store;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // An offer's splash (the web's offerSplash + Game.offerSale/buyFromSplash/doClaimNextDay): to buy — its price goes to
    // the confirmation; bought and waiting — the time until tomorrow's part; tomorrow come — Claim. A popup over the store
    // that opened it, or alone when the session raised it.
    public class OfferSplashMenuPresenter : AbstractDataMenuPresenter<OfferSplashMenu, OfferSplashOrder>, IPopupMenuPresenter, ITickable
    {
        private static readonly string[] ROMAN = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };
        private const string GEMS = "Gems";
        private const string HERO_XP = "HeroXp";
        // The picture on a gift's strip, by what it opens for good (the web's GIFT_ICON).
        private const string BUILD = "build";
        private const string EXPLORER = "compass";
        private const string HERO_SLOT = "helmet";

        private readonly UIManager _ui;
        private readonly Kingdom.Store.Store _store;
        private readonly Offers _offers;
        private readonly OfferValue _value;
        private readonly OfferTiles _tiles;
        private readonly ProductProse _prose;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;
        private readonly IQuickInfoMessageService _messages;
        private readonly OfferSplashGate _gate;
        private readonly OfferWidgets _widgets;
        private readonly UiIcons _icons;
        private readonly ICurrencyIcons _currencyIcons;
        private string _mode;
        // What is on screen — the mode, and the row of offers — so a change rebuilds it.
        private string _key;
        private List<WidgetOffer> _browse = new();
        private double _second = -1;

        public OfferSplashMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Store.Store store, Offers offers, OfferValue value,
            OfferTiles tiles, ProductProse prose, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds,
            RewardFlight flight, RewardFragments fragments, IQuickInfoMessageService messages, OfferSplashGate gate, OfferWidgets widgets, UiIcons icons, ICurrencyIcons currencyIcons) : base(views)
        {
            _ui = ui;
            _store = store;
            _offers = offers;
            _value = value;
            _tiles = tiles;
            _prose = prose;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
            _messages = messages;
            _gate = gate;
            _widgets = widgets;
            _icons = icons;
            _currencyIcons = currencyIcons;
        }

        public void RequestClose()
        {
            _gate.Closed(Data.Sku, _mode == "claim");
            _ = _ui.HideMenu<OfferSplashMenu>();
        }

        public void Tick()
        {
            if (View == null || !IsShown) return;
            var now = _clock.NowMs;
            View.Tick(now, s => _numbers.Countdown(s));
            // Tomorrow coming, a part claimed, an offer's window closing: what is on screen changes with them.
            var second = System.Math.Floor(now / 1000.0);
            if (second == _second) return;
            _second = second;
            if (Key(now) != _key) Refresh();
        }

        protected override void BindInternal(OfferSplashMenu view)
        {
            Refresh();
            if (Data.Auto) _sounds.Play(SoundIds.OFFER_SPLASH);
        }

        protected override void SubscribeToViewEventsInternal(OfferSplashMenu view)
        {
            view.CloseTapped += RequestClose;
            view.ActionTapped += OnAction;
            view.TabTapped += OnTab;
        }

        protected override void UnsubscribeFromViewEventsInternal(OfferSplashMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.ActionTapped -= OnAction;
            view.TabTapped -= OnTab;
        }

        private string Mode(string sku, double now) => _offers.NextDayReady(now).Any(d => d.Sku == sku) ? "claim"
            : _offers.NextDayWaiting(now).Any(d => d.Sku == sku) ? "waiting" : "buy";

        private List<WidgetOffer> Browse(double now) => Data.Browse ? _widgets.At(now) : new List<WidgetOffer>();

        private string Key(double now)
            => Mode(Data.Sku, now) + ":" + Data.Sku + ":" + string.Join(",", Browse(now).Select(w => w.Product.Id + "." + w.State));

        private void Refresh()
        {
            var now = _clock.NowMs;
            _key = Key(now);
            _browse = Browse(now);
            View.ShowTabs(_browse.Select(w => new OfferTabData
            {
                Icon = (w.Product as ProductAsset)?.Icon,
                Bust = _tiles.Hero(w.Product)?.Portrait,
                Kind = OfferWidgets.Kind(w.Product),
                Open = w.Product.Id == Data.Sku,
                Ready = w.State == OfferWidgetState.Ready,
            }).ToList());
            var product = _store.Get(Data.Sku);
            var asset = product as ProductAsset;
            var hero = _tiles.Hero(product);
            _mode = Mode(product.Id, now);
            var window = _offers.Window(product.Id);
            var left = product.Limit > 0 ? System.Math.Max(0, product.Limit - (window?.Bought ?? 0)) : (int?)null;
            var once = product.Limit == 1 && !(Repeats(product) && product.Hours > 0);
            var value = _value.ValuePercent(product);
            var chain = Chain(product);

            string aside, asideNote;
            OfferAsidePlaque plaque;
            if (hero != null)
            {
                aside = _localizer.Tr(hero.Name).Replace("The ", "");
                plaque = hero.Rarity switch
                {
                    HeroRarity.Legendary => OfferAsidePlaque.Legendary,
                    HeroRarity.Rare => OfferAsidePlaque.Rare,
                    _ => OfferAsidePlaque.Common,
                };
                asideNote = hero.Rarity switch
                {
                    HeroRarity.Legendary => _localizer.Tr("Legendary"),
                    HeroRarity.Rare => _localizer.Tr("Rare"),
                    _ => _localizer.Tr("Common"),
                };
            }
            else
            {
                aside = _localizer.Tr(product.Description);
                asideNote = chain.Count > 1 ? Roman(chain.IndexOf(product.Id) + 1) + " / " + Roman(chain.Count) : null;
                plaque = asideNote != null ? OfferAsidePlaque.Chain : OfferAsidePlaque.None;
            }

            var gifts = new List<OfferGiftData>();
            if (product.Builders > 0)
                gifts.Add(Gift(BUILD, _localizer.Trn(product.Builders, "A second builder", "{n} builders", ("n", _numbers.Exact(product.Builders))),
                    _localizer.Tr("Build two things at once")));
            if (product.Explorers > 0) gifts.Add(Gift(EXPLORER, _localizer.Tr("A second explorer"), _localizer.Tr("Explore the world with one more at once")));
            if (product.HeroSlots > 0) gifts.Add(Gift(HERO_SLOT, _localizer.Tr("A hero slot"), _localizer.Tr("One more hero in every party")));

            // More than four rewards with Gems among them: the Gems take a row of their own.
            var now_ = _tiles.Now(product);
            var many = now_.Count > 4 && product.Gems > 0;
            if (many) now_ = now_.Where(t => t.Icon != _currencyIcons.IconOf(GEMS)).ToList();

            var waitUntil = _mode == "waiting" ? _offers.NextDayWaiting(now).First(d => d.Sku == product.Id).ClaimableAt : (double?)null;
            View.Show(new OfferSplashData
            {
                Title = _localizer.Tr(product.DisplayName),
                Figure = asset != null && asset.Art != null ? asset.Art : hero != null && hero.Art != null ? hero.Art : asset != null ? asset.Icon : null,
                Aside = aside,
                AsideNote = asideNote,
                Plaque = plaque,
                NowTitle = _mode == "buy" ? _localizer.Tr("Yours now") : _localizer.Tr("Yours"),
                Now = now_,
                Gems = many ? _numbers.Exact(product.Gems) : null,
                Value = hero == null && value > 100 ? _numbers.Exact(value) + "%" : null,
                Gifts = gifts,
                TomorrowTitle = _localizer.Tr("Tomorrow"),
                Tomorrow = _tiles.NextDay(product),
                TomorrowLocked = _mode != "claim",
                Action = _mode == "buy" ? _prose.Price(product) : _mode == "claim" ? _localizer.Tr("Claim") : null,
                WaitUntil = waitUntil,
                WaitLabel = _localizer.Tr("Tomorrow in ").TrimEnd(),
                ClosesAt = _mode == "buy" ? window?.Closes : null,
                Note = _mode != "buy" ? null : once ? _localizer.Tr("Once per kingdom.") : left != null ? _localizer.Tr("Left: {n}", ("n", _numbers.Exact(left.Value))) : null,
            });
            View.Tick(now, s => _numbers.Countdown(s));
        }

        private OfferGiftData Gift(string icon, string title, string text) => new()
        {
            Icon = _icons.Get(icon),
            Title = _localizer.Tr("{title} — for good.", ("title", title)),
            Text = text + ".",
        };

        private static bool Repeats(IProductDefinition p) => p.OpensOn is "townhall" or "manaLow" or "manaOut" or "buildersBusy" or "explorersBusy" or "heroesBenched";

        // Its chain: back along `after` to the first step, then forward to the last.
        private List<string> Chain(IProductDefinition product)
        {
            var first = product;
            while (first.After != null) first = _store.Get(first.After);
            var chain = new List<string> { first.Id };
            while (_store.All.FirstOrDefault(p => p.Shelf == ProductShelf.Offer && p.After == chain[^1]) is { } next) chain.Add(next.Id);
            return chain;
        }

        private static string Roman(int n) => n >= 1 && n <= ROMAN.Length ? ROMAN[n - 1] : n.ToString();

        // Another offer in the row: the splash turns to it.
        private void OnTab(int index)
        {
            if (index >= _browse.Count || _browse[index].Product.Id == Data.Sku) return;
            Data.Sku = _browse[index].Product.Id;
            Refresh();
        }

        private async void OnAction()
        {
            var sku = Data.Sku;
            if (_mode == "buy")
            {
                _gate.Closed(sku, false);
                await _ui.HideMenu<OfferSplashMenu>();
                _ = _ui.ShowMenu<IapMenu, IapData>(new IapData { Sku = sku, From = "splash" });
                return;
            }

            if (_mode != "claim") return;
            var product = _store.Get(sku);
            if (_offers.ClaimNextDay(sku, _clock.NowMs) != ClaimNextDayResult.Claimed) return;
            _sounds.Play(SoundIds.QUEST_COMPLETE);
            _gate.Closed(sku, true);
            await _ui.HideMenu<OfferSplashMenu>();
            var haul = new Dictionary<string, double>();
            if (product.NextDayGems > 0) haul[GEMS] = product.NextDayGems;
            if (product.NextDayHeroXp > 0) haul[HERO_XP] = product.NextDayHeroXp;
            if (haul.Count > 0) _flight.Fly(haul, new Vector2(Screen.width / 2f, Screen.height / 2f), (c, a) => _fragments.For(c, a, false));
            if (_tiles.Hero(product) is { } hero && product.NextDayFragments > 0)
                _messages.Show(new QuickInfoMessageData(_localizer.Tr("{n} fragments of {hero}", ("n", _numbers.Exact(product.NextDayFragments)),
                    ("hero", _localizer.Tr(hero.Name)))));
        }
    }
}
