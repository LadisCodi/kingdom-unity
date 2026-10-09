using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.City;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Game.Map;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Cameras;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using ModuleVector2 = Codigames.Modules.Core.Vector2;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.UI.Presenters
{
    // Placing a building picked in the build menu: the ghost starts on the legal plot nearest the Townhall, and a
    // tap on the map or a drag of the ghost moves it; the panel reprices the wait for the plot and says what is
    // wrong with it. Build pays and starts the work; the close goes back to the build menu under it.
    public class PlacementMenuPresenter : AbstractDataMenuPresenter<PlacementMenu, string>, IClosableMenuPresenter, IMapDragHandler
    {
        // Where the ghost sits on screen while placing: above the panel.
        private static readonly ModuleVector2 GHOST_ON_SCREEN = new(0.5f, 0.58f);

        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly Placement _placement;
        private readonly ITreasury _treasury;
        private readonly BuildingCollection _buildings;
        private readonly ICurrencyIcons _icons;
        private readonly ProvinceMap _map;
        private readonly MapGestures _gestures;
        private readonly GhostView _ghost;
        private readonly CameraController _camera;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;

        private ModuleVector2Int? _anchor;
        private ModuleVector2Int _grab;

        public PlacementMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, Placement placement,
            ITreasury treasury, BuildingCollection buildings, ICurrencyIcons icons, ProvinceMap map, MapGestures gestures,
            GhostView ghost, CameraController camera, IClock clock, NumberFormat numbers, Localizer localizer, ISoundService sounds)
            : base(views)
        {
            _sounds = sounds;
            _ui = ui;
            _construction = construction;
            _placement = placement;
            _treasury = treasury;
            _buildings = buildings;
            _icons = icons;
            _map = map;
            _gestures = gestures;
            _ghost = ghost;
            _camera = camera;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        private BuildingAsset Building => _buildings.Get<BuildingAsset>(Data);

        public void RequestClose() => _ = _ui.HideMenu<PlacementMenu>();

        protected override void BindInternal(PlacementMenu view)
        {
            _anchor = _placement.Nearest(Data);
            _sounds.Play(SoundIds.GHOST_LIFT);
            Refresh();
            CentreOnGhost();

            _gestures.SetDragHandler(this);
            _gestures.Tapped += OnMapTapped;
            _treasury.Changed += OnTreasuryChanged;
        }

        protected override void UnbindInternal(PlacementMenu view)
        {
            _gestures.ClearDragHandler(this);
            _gestures.Tapped -= OnMapTapped;
            _treasury.Changed -= OnTreasuryChanged;
            _ghost.Hide();
        }

        protected override void SubscribeToViewEventsInternal(PlacementMenu view)
        {
            view.CloseTapped += RequestClose;
            view.BuildTapped += OnBuild;
        }

        protected override void UnsubscribeFromViewEventsInternal(PlacementMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.BuildTapped -= OnBuild;
        }

        public bool BeginDrag(ModuleVector2Int cell)
        {
            if (!_anchor.HasValue || !OnGhost(cell)) return false;

            _grab = new ModuleVector2Int(cell.X - _anchor.Value.X, cell.Y - _anchor.Value.Y);
            _sounds.Play(SoundIds.GHOST_LIFT);
            return true;
        }

        public void Drag(ModuleVector2Int cell) => MoveTo(new ModuleVector2Int(cell.X - _grab.X, cell.Y - _grab.Y));

        public void EndDrag() => _sounds.Play(SoundIds.GHOST_PLANT);

        private void OnMapTapped(ModuleVector2Int cell)
        {
            if (_anchor.HasValue && OnGhost(cell)) return;
            MoveTo(cell);
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();

        private void MoveTo(ModuleVector2Int anchor)
        {
            if (!_map.Contains(anchor) || anchor == _anchor) return;
            _anchor = anchor;
            _sounds.Play(SoundIds.GHOST_STEP);
            Refresh();
        }

        private bool OnGhost(ModuleVector2Int cell)
        {
            var building = Building;
            var anchor = _anchor.Value;
            return cell.X >= anchor.X && cell.X < anchor.X + building.Width && cell.Y >= anchor.Y && cell.Y < anchor.Y + building.Height;
        }

        private void OnBuild()
        {
            if (!_anchor.HasValue) return;

            var refusal = _construction.Build(Data, _anchor.Value, _clock.NowMs);
            if (refusal == ConstructionRefusal.None)
            {
                _sounds.Play(SoundIds.BUILD_PLACED);
                _ = _ui.CloseAll();
            }
            else
            {
                _sounds.Play(SoundIds.ERROR);
                Refresh();
            }
        }

        private void CentreOnGhost()
        {
            if (!_anchor.HasValue) return;

            var building = Building;
            var corners = ProvinceGeometry.Corners(_map, _anchor.Value, building.Width, building.Height);
            var centre = (corners[0] + corners[2]) / 2f;
            _camera.CenterOn(new ModuleVector2(centre.x, centre.y), GHOST_ON_SCREEN);
        }

        private void Refresh()
        {
            var building = Building;
            var offer = _construction.Offer(Data, _anchor);
            var problem = _anchor.HasValue ? _placement.Check(Data, _anchor.Value) : PlacementProblem.OutsideProvince;

            if (_anchor.HasValue)
            {
                var (basePosition, width) = ProvinceGeometry.Footprint(_map, _anchor.Value, building.Width, building.Height);
                _ghost.Show(building.ArtFor(1), basePosition, width,
                    ProvinceGeometry.Corners(_map, _anchor.Value, building.Width, building.Height), problem == PlacementProblem.None);
            }
            else
            {
                _ghost.Hide();
            }

            var price = offer.Price
                .Select(p => new CostChipData(_icons.IconOf(p.Key), _numbers.Exact(p.Value), _treasury.Get(p.Key) < p.Value))
                .ToList();

            View.Show(new PlacementPanelData(
                _localizer.Capitalized(_localizer.Tr(building.DisplayName)),
                offer.Numbered ? "#" + _numbers.Number(offer.Ordinal) : string.Empty,
                building.ArtFor(1), _localizer.Tr(building.Promise), _numbers.Duration(offer.Seconds), price,
                problem == PlacementProblem.None && offer.Refusal == ConstructionRefusal.None,
                Reason(problem, offer.Refusal)));
        }

        private string Reason(PlacementProblem problem, ConstructionRefusal refusal)
        {
            if (!_anchor.HasValue) return _localizer.Tr("Nowhere legal to build it");

            switch (problem)
            {
                case PlacementProblem.Occupied: return _localizer.Tr("Something stands there");
                case PlacementProblem.InFog: return _localizer.Tr("Clear the fog off it first");
                case PlacementProblem.NeedsLand: return _localizer.Tr("It needs dry land");
                case PlacementProblem.OutsideProvince: return _localizer.Tr("Outside the province");
                case PlacementProblem.CountLimit: return _localizer.Tr("No more of these at this Townhall");
            }

            return refusal == ConstructionRefusal.NoFreeBuilder ? _localizer.Tr("Every builder is busy") : string.Empty;
        }
    }
}
