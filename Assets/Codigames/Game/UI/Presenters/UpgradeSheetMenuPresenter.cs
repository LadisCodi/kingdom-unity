using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.UI.Buildings;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Research;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The upgrade sheet for one district: the level it becomes, what the level gains (the stats model's pairs), every
    // requirement met or not, and the price with its wait. The sim's own check decides the button, so the sheet never
    // offers a press the command would refuse; an unmet requirement locks it and says so, a short purse only reddens
    // its figure.
    public class UpgradeSheetMenuPresenter : AbstractDataMenuPresenter<UpgradeSheetMenu, string>, IClosableMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly CityState _city;
        private readonly BuildingCollection _buildings;
        private readonly ITreasury _treasury;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly BuildingStats _stats;
        private readonly BuildingStatProse _prose;
        private readonly TechProse _techs;
        private readonly UiIcons _icons;
        private readonly ISoundService _sounds;

        public UpgradeSheetMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, CityState city,
            BuildingCollection buildings, ITreasury treasury, IClock clock, NumberFormat numbers, Localizer localizer,
            BuildingStats stats, BuildingStatProse prose, TechProse techs, UiIcons icons, ISoundService sounds) : base(views)
        {
            _ui = ui;
            _construction = construction;
            _city = city;
            _buildings = buildings;
            _treasury = treasury;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
            _stats = stats;
            _prose = prose;
            _techs = techs;
            _icons = icons;
            _sounds = sounds;
        }

        private DistrictState District => _city.Districts.FirstOrDefault(d => d.Id == Data);

        // Closed — or its level bought — the sheet gives the building's card back its place.
        public async void RequestClose()
        {
            var district = Data;
            await _ui.HideMenu<UpgradeSheetMenu>();
            _ = _ui.ShowMenu<DistrictCardMenu, string>(district);
        }

        protected override void BindInternal(UpgradeSheetMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
            _construction.JobCompleted += OnJobCompleted;
        }

        protected override void UnbindInternal(UpgradeSheetMenu view)
        {
            _treasury.Changed -= OnTreasuryChanged;
            _construction.JobCompleted -= OnJobCompleted;
        }

        protected override void SubscribeToViewEventsInternal(UpgradeSheetMenu view)
        {
            view.CloseTapped += RequestClose;
            view.UpgradeTapped += OnUpgrade;
        }

        protected override void UnsubscribeFromViewEventsInternal(UpgradeSheetMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.UpgradeTapped -= OnUpgrade;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh();

        private void OnUpgrade()
        {
            var refusal = _construction.Upgrade(Data, _clock.NowMs);
            _sounds.Play(refusal == ConstructionRefusal.None ? SoundIds.UPGRADE_BOUGHT : SoundIds.ERROR);
            if (refusal == ConstructionRefusal.None) RequestClose();
        }

        private void Refresh()
        {
            var district = District;
            if (district == null) return;

            var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
            var offer = _construction.UpgradeOffer(district.Id);
            var next = district.Level + 1;
            var gates = _construction.UpgradeRequirements(district.Id);
            var locked = gates.Any(g => !g.Met);
            var note = locked ? _localizer.Tr("Complete all requirements to upgrade")
                : offer.Refusal == ConstructionRefusal.NoFreeBuilder ? _localizer.Tr("Every builder is busy")
                : offer.Refusal == ConstructionRefusal.CannotAfford ? _localizer.Tr("Not enough to pay for it")
                : string.Empty;
            var word = _localizer.Tr("Upgrade");

            View.Show(new UpgradeSheetData
            {
                Title = _localizer.Tr("Upgrade to Level {n}", ("n", _numbers.Exact(next))),
                From = building.ArtFor(district.Level),
                To = building.ArtFor(next),
                FromLevel = _localizer.Tr("Level {n}", ("n", _numbers.Exact(district.Level))),
                ToLevel = _localizer.Tr("Level {n}", ("n", _numbers.Exact(next))),
                Gains = _stats.Changes(district)
                    .Select(c => new UpgradeRowData(_prose.Icon(c.Now), _prose.Label(c.Now.Kind), _prose.Value(c.Now), _prose.Delta(c), !c.Better))
                    .ToList(),
                Gates = gates.Select(Gate).ToList(),
                Price = offer.Price
                    .Select(p => new PriceTerm(p.Key, _numbers.Exact(p.Value), _treasury.Get(p.Key) < p.Value))
                    .ToList(),
                Time = "<color=#7A5C3E><font-weight=700><sprite name=\"hourglass\">" + _numbers.Duration(offer.Seconds) + "</font-weight></color>",
                Locked = locked,
                Button = locked ? "<size=60%><sprite name=\"padlock\"></size> " + word : word,
                CanUpgrade = offer.Refusal == ConstructionRefusal.None,
                Note = note,
            });
        }

        private GateRowData Gate(UpgradeRequirement gate) => gate.Kind switch
        {
            RequirementKind.TownhallLevel => new GateRowData(_icons.Get("Townhall"),
                _localizer.Tr("Townhall level {n}", ("n", _numbers.Exact(gate.Amount))), gate.Met),
            RequirementKind.Research => new GateRowData(_icons.Get("research"),
                _localizer.Tr("Research {tech}", ("tech", _techs.Name(gate.Tech))), gate.Met),
            _ => new GateRowData(_icons.Get("population"),
                _localizer.Tr("Reach {n} population", ("n", _numbers.Exact(gate.Amount))), gate.Met),
        };
    }
}
