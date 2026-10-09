using System;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // One district's card: its name, number and level, what it does, and either the builder's work with its
    // countdown or the next level's price and wait; Upgrade pays and starts the work.
    public class DistrictCardMenuPresenter : AbstractDataMenuPresenter<DistrictCardMenu, string>, IClosableMenuPresenter, ITickable
    {
        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly CityState _city;
        private readonly BuildingCollection _buildings;
        private readonly IConstructionSettings _settings;
        private readonly ITreasury _treasury;
        private readonly ICurrencyIcons _icons;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly Stores _stores;
        private readonly VillagerTraining _training;
        private readonly Workforce _crews;
        private readonly ISoundService _sounds;

        // The second the card was last drawn at: it is redrawn once a second.
        private double _shownSeconds = -1;

        public DistrictCardMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, CityState city,
            BuildingCollection buildings, IConstructionSettings settings, ITreasury treasury, ICurrencyIcons icons, IClock clock,
            NumberFormat numbers, Localizer localizer, Stores stores, VillagerTraining training, Workforce crews,
            ISoundService sounds) : base(views)
        {
            _sounds = sounds;
            _crews = crews;
            _training = training;
            _stores = stores;
            _ui = ui;
            _construction = construction;
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _treasury = treasury;
            _icons = icons;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        private DistrictState District => _city.Districts.FirstOrDefault(d => d.Id == Data);

        public void RequestClose() => _ = _ui.HideMenu<DistrictCardMenu>();

        public void Tick()
        {
            if (!IsShown) return;

            // Once a second: the countdown, and the store filling.
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second != _shownSeconds) Refresh();
        }

        protected override void BindInternal(DistrictCardMenu view)
        {
            Refresh();
            _treasury.Changed += OnTreasuryChanged;
            _construction.JobStarted += OnJob;
            _construction.JobCompleted += OnJobCompleted;
            _training.Arrived += Refresh;
        }

        protected override void UnbindInternal(DistrictCardMenu view)
        {
            _treasury.Changed -= OnTreasuryChanged;
            _construction.JobStarted -= OnJob;
            _construction.JobCompleted -= OnJobCompleted;
            _training.Arrived -= Refresh;
        }

        protected override void SubscribeToViewEventsInternal(DistrictCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.UpgradeTapped += OnUpgrade;
            view.TrainTapped += OnTrain;
            view.CrewMinusTapped += OnCrewMinus;
            view.CrewPlusTapped += OnCrewPlus;
        }

        protected override void UnsubscribeFromViewEventsInternal(DistrictCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.UpgradeTapped -= OnUpgrade;
            view.TrainTapped -= OnTrain;
            view.CrewMinusTapped -= OnCrewMinus;
            view.CrewPlusTapped -= OnCrewPlus;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();
        private void OnJob(ConstructionJob job) => Refresh();
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh();

        private void OnUpgrade()
        {
            var refusal = _construction.Upgrade(Data, _clock.NowMs);
            _sounds.Play(refusal == ConstructionRefusal.None ? SoundIds.UPGRADE_BOUGHT : SoundIds.ERROR);
            Refresh();
        }

        private void OnTrain()
        {
            _training.Train(_clock.NowMs);
            Refresh();
        }

        private void OnCrewMinus()
        {
            _crews.Unassign(Data);
            Refresh();
        }

        private void OnCrewPlus()
        {
            _crews.Assign(Data, _clock.NowMs);
            Refresh();
        }

        // The crew line, on a building that works the ground.
        private CrewStripData Crew(DistrictState district)
        {
            if (!_crews.HasCrew(district)) return null;

            var assigned = _crews.Assigned(district.Id);
            var limit = _crews.Limit(district);
            var note = _crews.FreeVillagers > 0 || assigned >= limit
                ? _localizer.Tr("{n} free villagers", ("n", _numbers.Number(_crews.FreeVillagers)))
                : _localizer.Tr("Train villagers at the Townhall");

            return new CrewStripData(_numbers.Number(assigned) + " / " + _numbers.Number(limit), assigned > 0,
                _crews.CanAssign(district), note);
        }

        // The villager line, on the Townhall only.
        private TrainingStripData Training(DistrictState district, double now)
        {
            if (district.DefinitionId != _settings.Townhall.Id) return null;

            var villagers = _localizer.Tr("Villagers {n}/{cap}", ("n", _numbers.Number(_city.Population)), ("cap", _numbers.Number(_stores.Housing)));
            var arrivesAt = _training.Current?.ArrivesAt;
            var onTheWay = arrivesAt == null
                ? string.Empty
                : _localizer.Tr("{n} on the way · next in {time}", ("n", _numbers.Number(_city.Trainees.Count)),
                    ("time", _numbers.Duration(Math.Ceiling(Math.Max(0, arrivesAt.Value - now) / 1000))));
            var cost = _training.NextCost;
            var price = new[] { new CostChipData(_icons.IconOf(VillagerTraining.FOOD), _numbers.Exact(cost), _treasury.Get(VillagerTraining.FOOD) < cost) };
            var refusal = _training.Refusal;
            var reason = refusal == TrainRefusal.NoRoom ? _localizer.Tr("Build houses for more villagers") : string.Empty;

            return new TrainingStripData(villagers, onTheWay, price, refusal == TrainRefusal.None, reason);
        }

        private ConstructionJob JobOf(string districtId) => _city.Jobs.FirstOrDefault(j => j.DistrictId == districtId);

        private void Refresh()
        {
            var district = District;
            if (district == null) return;
            _shownSeconds = Math.Floor(_clock.NowMs / 1000.0);

            var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
            var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
            var numbered = building.Buildable && (!cap.HasValue || cap.Value > 1);
            var job = JobOf(district.Id);
            var name = _localizer.Capitalized(_localizer.Tr(building.DisplayName));
            var ordinal = numbered ? "#" + _numbers.Number(district.Ordinal) : string.Empty;
            var level = _localizer.Tr("Lv {n}", ("n", _numbers.Number(district.Level)));
            var promise = _localizer.Tr(building.Promise);
            var now = _clock.NowMs;
            var capacity = _stores.Capacity(district);
            var store = capacity > 0
                ? _localizer.Tr("Storage {held}/{cap}", ("held", _numbers.Short(_stores.Held(district, now))), ("cap", _numbers.Short(capacity)))
                : string.Empty;
            var storeFull = _stores.IsFull(district, now);

            if (job != null)
            {
                var remaining = Math.Max(0, job.CompletesAt - now) / 1000;
                var doing = job.TargetLevel == 1
                    ? _localizer.Tr("Building")
                    : _localizer.Tr("Upgrading to Lv {n}", ("n", _numbers.Number(job.TargetLevel)));
                var progress = (float)Math.Min(1, (_clock.NowMs - job.StartedAt) / (job.Seconds * 1000));

                View.Show(new DistrictCardData(name, ordinal, level, building.ArtFor(district.Level), promise, true,
                    doing + " · " + _numbers.Duration(Math.Ceiling(remaining)), progress, string.Empty,
                    Array.Empty<CostChipData>(), false, string.Empty, store, storeFull, Training(district, now), Crew(district)));
                return;
            }

            var offer = _construction.UpgradeOffer(district.Id);
            var atTop = offer.Refusal == ConstructionRefusal.MaxLevel;
            var price = offer.Price
                .Select(p => new CostChipData(_icons.IconOf(p.Key), _numbers.Exact(p.Value), _treasury.Get(p.Key) < p.Value))
                .ToList();
            var next = atTop
                ? string.Empty
                : _localizer.Tr("Lv {n}", ("n", _numbers.Number(offer.TargetLevel))) + " · " + _numbers.Duration(offer.Seconds);

            View.Show(new DistrictCardData(name, ordinal, level, building.ArtFor(district.Level), promise, false,
                string.Empty, 0, next, price, offer.Refusal == ConstructionRefusal.None, Reason(offer), store, storeFull, Training(district, now), Crew(district)));
        }

        private string Reason(UpgradeOffer offer) => offer.Refusal switch
        {
            ConstructionRefusal.MaxLevel => _localizer.Tr("Highest level"),
            ConstructionRefusal.NeedsTownhallLevel => _localizer.Tr("Needs Townhall level {n}", ("n", _numbers.Number(offer.RequiredTownhallLevel))),
            ConstructionRefusal.NoFreeBuilder => _localizer.Tr("Every builder is busy"),
            ConstructionRefusal.NeedsPopulation => _localizer.Tr("Needs {n} villagers", ("n", _numbers.Number(offer.RequiredPopulation))),
            _ => string.Empty,
        };
    }
}
