using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using UnityEngine;
using VContainer;

namespace Codigames.Game.City
{
    // The city on the map: one view per district, kept in step with what construction does, the bars of the
    // jobs under way, and the collect bubbles over the stores that are ready.
    public class CityView : MonoBehaviour
    {
        [SerializeField] private DistrictView _districtPrefab;
        [SerializeField] private Transform _districtsParent;

        private const float STORE_CHECK_SECONDS = 0.25f;

        private readonly Dictionary<string, DistrictView> _views = new();

        private CityState _city;
        private Construction _construction;
        private Kingdom.Army.Army _army;
        private BuildingCollection _buildings;
        private ProvinceMap _map;
        private IClock _clock;
        private NumberFormat _numbers;
        private Stores _stores;
        private ICurrencyIcons _icons;
        private float _nextStoreCheck;

        private Kingdom.Doors.Doors _doors;

        [Inject]
        public void Construct(CityState city, Construction construction, BuildingCollection buildings, ProvinceMap map, IClock clock,
            NumberFormat numbers, Stores stores, ICurrencyIcons icons, Kingdom.Doors.Doors doors, Kingdom.Army.Army army)
        {
            _army = army;
            _doors = doors;
            _numbers = numbers;
            _stores = stores;
            _icons = icons;
            _city = city;
            _construction = construction;
            _buildings = buildings;
            _map = map;
            _clock = clock;
        }

        private void Start()
        {
            foreach (var district in _city.Districts) Refresh(district);

            _construction.DistrictPlaced += Refresh;
            _construction.DistrictMoved += Refresh;
            _construction.JobCompleted += OnJobCompleted;
            _construction.JobStarted += OnJobStarted;
        }

        private void OnDestroy()
        {
            if (_construction == null) return;

            _construction.DistrictPlaced -= Refresh;
            _construction.DistrictMoved -= Refresh;
            _construction.JobCompleted -= OnJobCompleted;
            _construction.JobStarted -= OnJobStarted;
        }

        private void Update()
        {
            if (_city == null) return;

            var now = _clock.NowMs;
            foreach (var job in _city.Jobs)
            {
                if (!_views.TryGetValue(job.DistrictId, out var view)) continue;

                var remaining = System.Math.Max(0, job.StartedAt + job.Seconds * 1000 - now) / 1000;
                view.SetProgress((float)((now - job.StartedAt) / (job.Seconds * 1000)), _numbers.Duration(System.Math.Ceiling(remaining)));
            }

            // A hall's line over it while it trains: its head, and the whole line's time.
            foreach (var district in _city.Districts)
            {
                if (!district.Built || !_views.TryGetValue(district.Id, out var hall)) continue;
                var left = _army.RemainingSeconds(district.Id, now);
                if (left == null) hall.SetTraining(null, null);
                else hall.SetTraining((float)_army.HeadProgress(district.Id, now), _numbers.Duration(System.Math.Ceiling(left.Value)));
            }
        }

        // The collect bubbles, a few times a second: a store's readiness changes with time alone.
        private void LateUpdate()
        {
            if (_city == null || Time.unscaledTime < _nextStoreCheck) return;
            _nextStoreCheck = Time.unscaledTime + STORE_CHECK_SECONDS;

            var now = _clock.NowMs;
            foreach (var district in _city.Districts)
            {
                if (!_views.TryGetValue(district.Id, out var view)) continue;

                // Through the First Morning, the Townhall's own Gold shows no bubble.
                var ready = _stores.IsReady(district, now) && !(_doors.FirstMorningOn && _stores.MakesItsOwn(district));
                view.SetStore(ready, ready ? _icons.IconOf(Stores.GOLD) : null, ready && _stores.IsFull(district, now));
            }
        }

        public DistrictView ViewOf(string districtId) => _views.TryGetValue(districtId, out var view) ? view : null;

        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh(district);

        private void OnJobStarted(ConstructionJob job)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == job.DistrictId);
            if (district != null) Refresh(district);
        }

        private void Refresh(DistrictState district)
        {
            if (!_views.TryGetValue(district.Id, out var view))
            {
                view = Instantiate(_districtPrefab, _districtsParent);
                view.name = district.Id;
                _views[district.Id] = view;
            }

            var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
            var (basePosition, width) = ProvinceGeometry.Footprint(_map, district.Anchor, building.Width, building.Height);
            view.Show(building.ArtFor(district.Level), basePosition, width, district.Built && !_city.Jobs.Any(j => j.DistrictId == district.Id && j.TargetLevel == 1));
            view.SetWorking(_city.Jobs.Any(j => j.DistrictId == district.Id));
        }
    }
}
