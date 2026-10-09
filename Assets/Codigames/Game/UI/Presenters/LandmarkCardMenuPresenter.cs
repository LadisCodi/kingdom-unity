using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Sites;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // A landmark's card: the reward made legible before anything is spent — what it gives, what it costs — and Claim.
    public class LandmarkCardMenuPresenter : AbstractDataMenuPresenter<LandmarkCardMenu, string>, IClosableMenuPresenter
    {
        private const string GOLD = "Gold";

        private readonly UIManager _ui;
        private readonly Landmarks _landmarks;
        private readonly ProvinceSitesAsset _sites;
        private readonly IManaSettings _manaSettings;
        private readonly ManaPool _mana;
        private readonly ITreasury _treasury;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;
        private readonly CardFraming _framing;

        public LandmarkCardMenuPresenter(IMenuViewFactory views, UIManager ui, Landmarks landmarks, ProvinceSitesAsset sites,
            IManaSettings manaSettings, ManaPool mana, ITreasury treasury, NumberFormat numbers, Localizer localizer, IClock clock,
            ISoundService sounds, IQuickInfoMessageService messages, CardFraming framing) : base(views)
        {
            _framing = framing;
            _ui = ui;
            _landmarks = landmarks;
            _sites = sites;
            _manaSettings = manaSettings;
            _mana = mana;
            _treasury = treasury;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
        }

        public void RequestClose() => _ = _ui.HideMenu<LandmarkCardMenu>();

        protected override void BindInternal(LandmarkCardMenu view)
        {
            Refresh();
            if (_landmarks.All.FirstOrDefault(l => l.Id == Data) is { } site)
                _framing.Frame(site.Anchor, site.Size, site.Size, View.CardTop);

            _treasury.Changed += OnTreasuryChanged;
        }

        protected override void UnbindInternal(LandmarkCardMenu view) => _treasury.Changed -= OnTreasuryChanged;

        protected override void SubscribeToViewEventsInternal(LandmarkCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.ClaimTapped += OnClaim;
        }

        protected override void UnsubscribeFromViewEventsInternal(LandmarkCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.ClaimTapped -= OnClaim;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();

        private void OnClaim()
        {
            var result = _landmarks.Claim(Data, _clock.NowMs);
            if (result == ClaimResult.Claimed)
            {
                _sounds.Play(SoundIds.UPGRADE_BOUGHT);
                Refresh();
                return;
            }

            _sounds.Play(SoundIds.ERROR);
            var message = result switch
            {
                ClaimResult.NotRevealed => _localizer.Tr("Clear the fog off it first"),
                ClaimResult.CannotAfford => _localizer.Tr("Not enough Gold"),
                _ => string.Empty,
            };
            if (message != string.Empty) _messages.Show(new QuickInfoMessageData(message));
        }

        private void Refresh()
        {
            var site = _landmarks.All.FirstOrDefault(l => l.Id == Data);
            if (site == null) return;

            var kind = _sites.KindOf(site.Kind);
            var claimed = _landmarks.IsClaimed(site.Id);
            var side = _landmarks.DiscoverRadius * 2 + 1;
            var cost = _landmarks.ClaimCost(site);
            var note = claimed
                ? _localizer.Tr("Holding {n} more Mana. Your pool: {pool}.", ("n", _numbers.Exact(_manaSettings.LandmarkCap)),
                    ("pool", _numbers.Exact(_mana.Cap)))
                : _localizer.Tr("A longer run of taps, and more from every refill. The fog lifts for {n} cells around: you will see what is out there, though clearing it is still yours to pay for.",
                    ("n", _numbers.Exact(_landmarks.DiscoverRadius)));

            View.Show(_localizer.Tr(kind?.Name ?? site.Kind), kind?.Art, claimed ? _localizer.Tr("Claimed") : _localizer.Tr("Unclaimed"),
                claimed ? _localizer.Tr("Gives") : _localizer.Tr("Claim it for"), "+" + _numbers.Exact(_manaSettings.LandmarkCap),
                $"{_numbers.Exact(side)}×{_numbers.Exact(side)}", note, !claimed, _numbers.Exact(cost), _treasury.Get(GOLD) >= cost);
        }
    }
}
