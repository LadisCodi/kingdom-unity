using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Store;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Hud;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Kingdom.Store;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.Feedback;
using Codigames.Modules.UI;
using UnityEngine;

namespace Codigames.Game.UI.Presenters
{
    // The purchase confirmation over the screen that raised it (the web's iapSheet + Game.confirmIap). Buy spends the
    // month's budget: bought, the Gems fly to the purse and what went to the Bag is said; refused, the Gems shake and
    // the sheet stays; an offer that closed under it charges nothing. The store never grants anything itself.
    public class IapMenuPresenter : AbstractDataMenuPresenter<IapMenu, IapData>, IPopupMenuPresenter
    {
        private const string GEMS = "Gems";

        private readonly UIManager _ui;
        private readonly Kingdom.Store.Store _store;
        private readonly ProductProse _prose;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;
        private readonly IQuickInfoMessageService _messages;
        private readonly Kingdom.Heroes.Heroes _heroes;

        public IapMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Store.Store store, ProductProse prose, NumberFormat numbers,
            Localizer localizer, IClock clock, ISoundService sounds, RewardFlight flight, RewardFragments fragments,
            IQuickInfoMessageService messages, Kingdom.Heroes.Heroes heroes = null) : base(views)
        {
            _heroes = heroes;
            _ui = ui;
            _store = store;
            _prose = prose;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
            _messages = messages;
        }

        public void RequestClose() => _ = _ui.HideMenu<IapMenu>();

        protected override void BindInternal(IapMenu view) => Refresh();

        protected override void SubscribeToViewEventsInternal(IapMenu view)
        {
            view.CloseTapped += RequestClose;
            view.BuyTapped += OnBuy;
        }

        protected override void UnsubscribeFromViewEventsInternal(IapMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.BuyTapped -= OnBuy;
        }

        private void Refresh()
        {
            var product = _store.Get(Data.Sku);
            var asset = product as ProductAsset;
            var now = _clock.NowMs;
            var price = Kingdom.Store.Store.PriceCents(product);
            var left = _store.BudgetRemainingCents(now);
            var affordable = left is { } l && l >= price;
            var rows = new List<(string, string)>
            {
                (_localizer.Tr("Price"), _numbers.Usd(price)),
                (_localizer.Tr("Left this month"), left == null ? "—" : _numbers.Usd(left.Value)),
                affordable
                    ? (_localizer.Tr("After"), _numbers.Usd(left.Value - price))
                    : (_localizer.Tr("Short by"), left == null ? "—" : _numbers.Usd(price - left.Value)),
            };

            string note = null;
            if (!affordable)
            {
                if (_store.Profile == null) note = _localizer.Tr("Pick a payer profile first.");
                else if (_store.MonthlyBudgetCents(_store.Profile.Value) == 0) note = _localizer.Tr("You are playing as someone who never spends.");
                else note = _localizer.Tr("Your budget refills {when}.", ("when", _prose.Wait(Kingdom.Store.Store.MonthResetsAt(now) - now)));
            }

            View.Show(_localizer.Tr("Confirm purchase"), asset != null ? asset.Icon : null, _localizer.Tr(product.DisplayName),
                product.Gems > 0 ? "<sprite name=\"Gems\"> " + _localizer.Tr("{n} Gems", ("n", _numbers.Exact(product.Gems))) : _localizer.Tr(product.Description),
                string.Join("\n", _prose.Lines(product)), rows.ToArray(), affordable, note,
                _localizer.Tr("SIMULADO — no real money changes hands."), _localizer.Tr("Not now"),
                _localizer.Tr("Buy for {price}", ("price", _numbers.Usd(price))), affordable);
        }

        private async void OnBuy()
        {
            var product = _store.Get(Data.Sku);
            var held = product.Hero != null && _heroes != null && _heroes.Owns(product.Hero);
            var fragmentsBefore = product.Hero != null && _heroes != null ? _heroes.Fragments(product.Hero) : 0;
            var result = _store.Buy(product.Id, _clock.NowMs);
            switch (result)
            {
                case BuyResult.NotOnSale:
                    _messages.Show(new QuickInfoMessageData(_localizer.Tr("That offer has ended")));
                    await _ui.HideMenu<IapMenu>();
                    return;
                case BuyResult.Purchased:
                    _sounds.Play(SoundIds.GEM_SPEND);
                    await _ui.HideMenu<IapMenu>();
                    if (product.Items.Count > 0)
                        _messages.Show(new QuickInfoMessageData(_localizer.Tr("{name} — it is in the Bag", ("name", _localizer.Tr(product.DisplayName)))));
                    if (product.Gems > 0)
                        _flight.Fly(new Dictionary<string, double> { [GEMS] = product.Gems },
                            new Vector2(Screen.width / 2f, Screen.height / 2f), (c, a) => _fragments.For(c, a, false));
                    if (product.Hero != null && _heroes != null)
                    {
                        var prize = held
                            ? new Kingdom.Heroes.Prize { Kind = Kingdom.Heroes.PrizeKind.Fragments, Id = product.Hero, Amount = _heroes.Fragments(product.Hero) - fragmentsBefore }
                            : new Kingdom.Heroes.Prize { Kind = Kingdom.Heroes.PrizeKind.Hero, Id = product.Hero, Amount = 1 };
                        _ = _ui.ShowMenu<RevealScreen, Kingdom.Heroes.Reveal>(new Kingdom.Heroes.Reveal
                        {
                            Chest = Kingdom.Heroes.RevealChest.Golden, Calls = 1, Prizes = new[] { prize },
                        });
                    }

                    return;
                default:
                    // A refusal is data, and a denial: the sheet stays, saying why.
                    _sounds.Play(SoundIds.ERROR);
                    Refresh();
                    return;
            }
        }
    }
}
