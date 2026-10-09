using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // An abandoned building's card: what it is and what repairing it costs; Repair starts the work where it stands.
    public class RuinCardMenuPresenter : AbstractDataMenuPresenter<RuinCardMenu, string>, IClosableMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly Ruins _ruins;
        private readonly BuildingCollection _buildings;
        private readonly ITreasury _treasury;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;

        public RuinCardMenuPresenter(IMenuViewFactory views, UIManager ui, Ruins ruins, BuildingCollection buildings, ITreasury treasury,
            ICurrencyIcons icons, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds,
            IQuickInfoMessageService messages) : base(views)
        {
            _ui = ui;
            _ruins = ruins;
            _buildings = buildings;
            _treasury = treasury;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
        }

        public void RequestClose() => _ = _ui.HideMenu<RuinCardMenu>();

        protected override void BindInternal(RuinCardMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
        }

        protected override void UnbindInternal(RuinCardMenu view) => _treasury.Changed -= OnTreasuryChanged;

        protected override void SubscribeToViewEventsInternal(RuinCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.RepairTapped += OnRepair;
        }

        protected override void UnsubscribeFromViewEventsInternal(RuinCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.RepairTapped -= OnRepair;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();

        private void OnRepair()
        {
            var site = _ruins.Get(Data);
            if (site == null) return;

            var refusal = _ruins.Repair(site.Id, _clock.NowMs);
            if (refusal == RepairRefusal.None)
            {
                _sounds.Play(SoundIds.BUILD_PLACED);
                RequestClose();
                return;
            }

            _sounds.Play(SoundIds.ERROR);
            var message = Refusal(refusal, site);
            if (!string.IsNullOrEmpty(message)) _messages.Show(new QuickInfoMessageData(message));
        }

        private string Refusal(RepairRefusal refusal, IAbandonedSite site) => refusal switch
        {
            RepairRefusal.NotRevealed => _localizer.Tr("Clear the fog off it first"),
            RepairRefusal.NoFreeBuilder => _localizer.Tr("Every builder is busy"),
            RepairRefusal.AtCap => _localizer.Tr("The Townhall can hold no more {building} — raise it first",
                ("building", _localizer.Tr(_buildings.Get<BuildingAsset>(site.District).DisplayName))),
            RepairRefusal.MissingItem => _localizer.Tr("It needs {item} — find it first", ("item", _localizer.Tr("a missing piece"))),
            RepairRefusal.CannotAfford => _localizer.Tr("Not enough Gold"),
            _ => string.Empty,
        };

        private void Refresh()
        {
            var site = _ruins.Get(Data);
            if (site == null) return;

            var building = _buildings.Get<BuildingAsset>(site.District);
            var price = _ruins.Price(site)
                .Select(p => new CostChipData(_icons.IconOf(p.Key), _numbers.Exact(p.Value), _treasury.Get(p.Key) < p.Value))
                .ToList();
            var need = _ruins.MissingItem(site) == null
                ? string.Empty
                : _localizer.Tr("Needs {item} — not found yet", ("item", _localizer.Tr("a missing piece")));

            View.Show(_localizer.Tr(site.Name), building.RuinArt, _localizer.Tr(building.Promise), need, price);
        }
    }
}
