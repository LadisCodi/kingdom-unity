using System;
using Codigames.Game.Audio;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Store;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The payer profile, asked once (the web's payerSheet + Game.doChoosePayerProfile). Nothing closes it but a choice.
    public class PayerMenuPresenter : AbstractMenuPresenter<PayerMenu>, IClosableMenuPresenter
    {
        private static readonly PayerProfile[] PROFILES = (PayerProfile[])Enum.GetValues(typeof(PayerProfile));

        private readonly UIManager _ui;
        private readonly Kingdom.Store.Store _store;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;

        public PayerMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Store.Store store, NumberFormat numbers, Localizer localizer,
            IClock clock, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _store = store;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }


        // A window like any other (the nav steps aside for it), that nothing closes but a choice.
        public void RequestClose() { }

        protected override void BindInternal(PayerMenu view)
        {
            var options = new (string, string, string, string)[PROFILES.Length];
            for (var i = 0; i < PROFILES.Length; i++)
            {
                var cents = _store.MonthlyBudgetCents(PROFILES[i]);
                options[i] = (Label(PROFILES[i]), cents == 0 ? _localizer.Tr("No purchases") : _localizer.Tr("{price} a month", ("price", _numbers.Usd(cents))),
                    Blurb(PROFILES[i]), _localizer.Tr("Play as this"));
            }

            view.Show(_localizer.Tr("Who are you playing as?"),
                _localizer.Tr("This prototype has a store, and nothing in it charges real money.") + "\n\n"
                + _localizer.Tr("Pick how much you would spend a month and the game will hold you to it. Prices come out of that budget, and it refills on the first of the month."),
                _localizer.Tr("The choice is final for this kingdom. To play as someone else, start over from Settings."), options);
        }

        protected override void SubscribeToViewEventsInternal(PayerMenu view) => view.Chosen += OnChosen;

        protected override void UnsubscribeFromViewEventsInternal(PayerMenu view) => view.Chosen -= OnChosen;

        private async void OnChosen(int index)
        {
            _store.ChooseProfile(PROFILES[index], _clock.NowMs);
            _sounds.Play(SoundIds.BUTTON_PRESS);
            await _ui.HideMenu<PayerMenu>();
        }

        public string Label(PayerProfile profile) => profile switch
        {
            PayerProfile.F2P => "F2P",
            PayerProfile.Minnow => _localizer.Tr("Minnow"),
            PayerProfile.Dolphin => _localizer.Tr("Dolphin"),
            PayerProfile.Whale => _localizer.Tr("Whale"),
            _ => _localizer.Tr("Super Whale"),
        };

        private string Blurb(PayerProfile profile) => profile switch
        {
            PayerProfile.F2P => _localizer.Tr("Never spends. The store is open to look at, and every price is refused."),
            PayerProfile.Minnow => _localizer.Tr("A pack or two a month, when something is worth it."),
            PayerProfile.Dolphin => _localizer.Tr("Buys what saves time. A chest of Gems most weeks."),
            PayerProfile.Whale => _localizer.Tr("Buys what they want, when they want it."),
            _ => _localizer.Tr("The store is not a constraint. Buys everything, and then buys it again."),
        };
    }
}
