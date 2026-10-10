using System;
using System.Linq;
using Codigames.Game.Fog;
using Codigames.Game.Harvest;
using Codigames.Game.Map;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Core;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.City
{
    // What a tap on the map means while no menu is open: on the fog, a share of its price; on a building, a
    // collect when its store is ready and its card otherwise; on a treasure, picking it up; on a lair, a landmark or a ruin, its card; on
    // the ground, a harvest.
    public class MapTaps : IStartable, IDisposable
    {
        private readonly MapGestures _gestures;
        private readonly UIManager _ui;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly HarvestInput _harvest;
        private readonly CollectInput _collect;
        private readonly FogOfWar _fog;
        private readonly FogInput _fogInput;
        private readonly Ruins _ruins;
        private readonly TreasureInput _treasures;
        private readonly Landmarks _landmarks;
        private readonly Lairs.LairsView _lairs;
        private readonly Codigames.Kingdom.Lairs.LairGround _lairGround;
        private readonly Relics.ShrinesView _shrines;

        public MapTaps(MapGestures gestures, UIManager ui, CityState city, ICatalog<IBuildingDefinition> buildings, HarvestInput harvest,
            CollectInput collect, FogOfWar fog, FogInput fogInput, Ruins ruins, TreasureInput treasures, Landmarks landmarks,
            Lairs.LairsView lairs, Codigames.Kingdom.Lairs.LairGround lairGround, Relics.ShrinesView shrines)
        {
            _shrines = shrines;
            _lairs = lairs;
            _lairGround = lairGround;
            _landmarks = landmarks;
            _treasures = treasures;
            _ruins = ruins;
            _fog = fog;
            _fogInput = fogInput;
            _collect = collect;
            _gestures = gestures;
            _ui = ui;
            _city = city;
            _buildings = buildings;
            _harvest = harvest;
        }

        public void Start() => _gestures.Tapped += OnTapped;

        public void Dispose() => _gestures.Tapped -= OnTapped;

        private string StandingLairOn(Vector2Int cell)
            => _lairGround.All.FirstOrDefault(l => _lairGround.Holds(l.Id) && Codigames.Modules.Grid.GridMath.Rect(l.Anchor, l.Size, l.Size).Contains(cell))?.Id;

        private void OnTapped(Vector2Int cell)
        {
            if (_ui.HasOverlayOpen) return;

            // A lair's bubble floats over other cells, and its model stands taller than its ground: either is the lair,
            // fog or not, and so is its own footprint.
            var lair = _lairs.LairAt(_gestures.LastTap) ?? StandingLairOn(cell);
            if (lair != null)
            {
                _ = _ui.ShowMenu<LairCardMenu, string>(lair);
                return;
            }

            // A sleeping Shrine's bubble floats over the cells above it: it is the Shrine, whose card holds Activate.
            var shrine = _shrines.ShrineAt(_gestures.LastTap);
            if (shrine != null)
            {
                _ = _ui.ShowMenu<DistrictCardMenu, string>(shrine);
                return;
            }

            if (!_fog.IsRevealed(cell))
            {
                _fogInput.Tap(cell);
                return;
            }

            // A treasure is picked up before anything under it is touched.
            if (_treasures.TryPickUp(cell)) return;

            var landmark = _landmarks.At(cell);
            if (landmark != null)
            {
                _ = _ui.ShowMenu<LandmarkCardMenu, string>(landmark.Id);
                return;
            }

            var ruin = _ruins.At(cell);
            if (ruin != null)
            {
                _ = _ui.ShowMenu<RuinCardMenu, string>(ruin.Id);
                return;
            }

            var district = CityQueries.At(_city, _buildings, cell);
            if (district == null) _harvest.Take(cell);
            else if (!_collect.TryCollect(district)) _ = _ui.ShowMenu<DistrictCardMenu, string>(district.Id);
        }
    }
}
