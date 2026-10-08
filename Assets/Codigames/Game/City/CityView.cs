using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.City;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Modules.Clock;
using UnityEngine;
using VContainer;

namespace Codigames.Game.City
{
    // The city on the map: one view per district, kept in step with what construction does, and the bars of
    // the jobs under way.
    public class CityView : MonoBehaviour
    {
        [SerializeField] private DistrictView _districtPrefab;
        [SerializeField] private Transform _districtsParent;

        private readonly Dictionary<string, DistrictView> _views = new();

        private CityState _city;
        private Construction _construction;
        private BuildingCollection _buildings;
        private ProvinceMap _map;
        private IClock _clock;

        [Inject]
        public void Construct(CityState city, Construction construction, BuildingCollection buildings, ProvinceMap map, IClock clock)
        {
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
        }

        private void OnDestroy()
        {
            if (_construction == null) return;

            _construction.DistrictPlaced -= Refresh;
            _construction.DistrictMoved -= Refresh;
            _construction.JobCompleted -= OnJobCompleted;
        }

        private void Update()
        {
            if (_city == null) return;

            var now = _clock.NowMs;
            foreach (var job in _city.Jobs)
            {
                if (_views.TryGetValue(job.DistrictId, out var view))
                    view.SetProgress((float)((now - job.StartedAt) / (job.Seconds * 1000)));
            }
        }

        public DistrictView ViewOf(string districtId) => _views.TryGetValue(districtId, out var view) ? view : null;

        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh(district);

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
        }
    }
}
