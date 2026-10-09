using System;
using Codigames.Game.Harvest;
using Codigames.Game.Map;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Modules.Core;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.City
{
    // What a tap on the map means while no menu is open: on a building, its card; on the ground, a harvest.
    public class MapTaps : IStartable, IDisposable
    {
        private readonly MapGestures _gestures;
        private readonly UIManager _ui;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly HarvestInput _harvest;

        public MapTaps(MapGestures gestures, UIManager ui, CityState city, ICatalog<IBuildingDefinition> buildings, HarvestInput harvest)
        {
            _gestures = gestures;
            _ui = ui;
            _city = city;
            _buildings = buildings;
            _harvest = harvest;
        }

        public void Start() => _gestures.Tapped += OnTapped;

        public void Dispose() => _gestures.Tapped -= OnTapped;

        private void OnTapped(Vector2Int cell)
        {
            if (_ui.HasOverlayOpen) return;

            var district = CityQueries.At(_city, _buildings, cell);
            if (district != null) _ = _ui.ShowMenu<DistrictCardMenu, string>(district.Id);
            else _harvest.Take(cell);
        }
    }
}
