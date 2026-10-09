using System;
using Codigames.Game.Map;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Stage;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Core;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.City
{
    // A long press on the map picks up what may move (Docs/features/05-city-and-districts.md §4.3, 27-plantables.md
    // §4): a building, a crop plot, or a tree once Transplanting is researched — a tree before then says what to
    // research. Never with a menu open, nor while a tutorial line holds a lock.
    public class MoveStarter : IMapHoldHandler, IStartable, IDisposable
    {
        private readonly MapGestures _gestures;
        private readonly UIManager _ui;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly Transplanting _transplanting;
        private readonly StagePresenter _stage;
        private readonly IQuickInfoMessageService _messages;
        private readonly Localizer _localizer;

        public MoveStarter(MapGestures gestures, UIManager ui, CityState city, ICatalog<IBuildingDefinition> buildings, Transplanting transplanting,
            StagePresenter stage, IQuickInfoMessageService messages, Localizer localizer)
        {
            _gestures = gestures;
            _ui = ui;
            _city = city;
            _buildings = buildings;
            _transplanting = transplanting;
            _stage = stage;
            _messages = messages;
            _localizer = localizer;
        }

        public void Start() => _gestures.SetHoldHandler(this);

        public void Dispose() => _gestures.SetHoldHandler(null);

        public bool CanHold(ModuleVector2Int cell)
        {
            if (_ui.HasOverlayOpen || !_stage.AllowsHold) return false;
            var district = CityQueries.At(_city, _buildings, cell);
            if (district != null) return _buildings.Get(district.DefinitionId).Buildable;
            var refusal = _transplanting.PickUpRefusal(cell);
            return refusal == TransplantRefusal.None || refusal == TransplantRefusal.NeedsResearch;
        }

        public bool Hold(ModuleVector2Int cell)
        {
            if (!CanHold(cell)) return false;

            var district = CityQueries.At(_city, _buildings, cell);
            if (district != null)
            {
                _ = _ui.ShowMenu<PlacementMenu, PlacementOrder>(PlacementOrder.MoveDistrict(district.Id, district.DefinitionId));
                return true;
            }

            if (_transplanting.PickUpRefusal(cell) == TransplantRefusal.NeedsResearch)
            {
                _messages.Show(new QuickInfoMessageData(_localizer.Tr("Research {tech} before you can move trees",
                    ("tech", _localizer.Tr(Transplanting.TRANSPLANTING)))));
                return false;
            }

            _ = _ui.ShowMenu<PlacementMenu, PlacementOrder>(PlacementOrder.MoveFeature(cell));
            return true;
        }
    }
}
