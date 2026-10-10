using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.City;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Map;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Audio;
using Codigames.Modules.Cameras;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;
using ModuleVector2 = Codigames.Modules.Core.Vector2;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.UI.Presenters
{
    // The ghost on the map and its panel (Docs/features/05-city-and-districts.md §4): placing a building picked in
    // the build menu, starting on the legal plot nearest the Townhall; or moving one, or a tree or a crop plot,
    // starting where it stands, faint at its old address while its ghost is out. A tap on the map or a drag of the
    // ghost carries it; the panel says what the plot costs and what is wrong with it. Build pays and starts the
    // work; Move puts it down — free and at once for a building, which reopens its card; growing from nothing for a
    // tree or a plot, a wait the panel says before the button. A move back where it started is a cancel.
    public class PlacementMenuPresenter : AbstractDataMenuPresenter<PlacementMenu, PlacementOrder>, IClosableMenuPresenter, IMapDragHandler
    {
        // Where the ghost sits on screen while placing: above the panel.
        private static readonly ModuleVector2 GHOST_ON_SCREEN = new(0.5f, 0.58f);

        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly Adjacency _adjacency;
        private readonly WorkAreaView _workArea;
        private readonly Kingdom.Crews.Workforce _crews;
        private readonly Harvesting _harvesting;
        private readonly Kingdom.Map.IProvinceMap _province;
        private readonly PriceTerms _prices;
        // A pill's height, in tile widths: the web's labels, a fifth of a tile with their padding.
        private const float PILL_HEIGHT = 0.29f;
        private readonly Placement _placement;
        private readonly Transplanting _transplanting;
        private readonly ITreasury _treasury;
        private readonly BuildingCollection _buildings;
        private readonly FeatureCollection _features;
        private readonly CityState _city;
        private readonly GroundState _ground;
        private readonly ProvinceMap _map;
        private readonly MapGestures _gestures;
        private readonly GhostView _ghost;
        private readonly CityView _cityView;
        private readonly GroundView _groundView;
        private readonly CameraController _camera;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;

        private ModuleVector2Int? _anchor;
        private ModuleVector2Int _grab;
        private string _feature;
        // The building faint at its old address, remembered: the order is gone by the time the menu has closed.
        private string _liftedDistrict;
        private readonly Feedback.LandingFx _landing;
        // The ways a ghost can step: one grid axis each, wherever the map goes on.
        private static readonly Vector2Int[] STEPS = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        private readonly List<Vector2Int> _steps = new();

        public PlacementMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, Placement placement,
            Transplanting transplanting, ITreasury treasury, BuildingCollection buildings, FeatureCollection features, CityState city,
            GroundState ground, ProvinceMap map, MapGestures gestures, GhostView ghost, CityView cityView,
            GroundView groundView, CameraController camera, IClock clock, NumberFormat numbers, Localizer localizer, ISoundService sounds,
            Adjacency adjacency, WorkAreaView workArea, Kingdom.Crews.Workforce crews, Harvesting harvesting, Kingdom.Map.IProvinceMap province, PriceTerms prices,
            Feedback.LandingFx landing = null)
            : base(views)
        {
            _landing = landing;
            _prices = prices;
            _province = province;
            _workArea = workArea;
            _crews = crews;
            _harvesting = harvesting;
            _adjacency = adjacency;
            _sounds = sounds;
            _ui = ui;
            _construction = construction;
            _placement = placement;
            _transplanting = transplanting;
            _treasury = treasury;
            _buildings = buildings;
            _features = features;
            _city = city;
            _ground = ground;
            _map = map;
            _gestures = gestures;
            _ghost = ghost;
            _cityView = cityView;
            _groundView = groundView;
            _camera = camera;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
        }

        // What is being placed, while the ghost is out: a building's id when it is built.
        public string PlacingId => IsShown && Data is { IsMove: false } ? Data.DefinitionId : null;

        // Where the ghost stands now, while it is out.
        public ModuleVector2Int? GhostAnchor => IsShown ? _anchor : null;

        // The building being moved, by its district id; null while placing or moving a feature.
        public string MovingDistrictId => IsShown && Data is { IsMove: true, MovesDistrict: true } ? Data.DistrictId : null;

        // What is moving: a building's id, or the feature's.
        public string MovingId => !IsShown || Data is not { IsMove: true } ? null : Data.MovesDistrict ? Data.DefinitionId : _feature;

        private BuildingAsset Building => _buildings.Get<BuildingAsset>(Data.DefinitionId);

        private DistrictState District => _city.Districts.FirstOrDefault(d => d.Id == Data.DistrictId);

        // Where the thing moving starts.
        private ModuleVector2Int? Origin => Data.MovesDistrict ? District?.Anchor : Data.FeatureCell;

        private int Width => Data.MovesFeature ? 1 : Building.Width;

        private int Height => Data.MovesFeature ? 1 : Building.Height;

        public void RequestClose() => _ = _ui.HideMenu<PlacementMenu>();

        protected override void BindInternal(PlacementMenu view)
        {
            _feature = Data.MovesFeature && _ground.Features.TryGetValue(Data.FeatureCell.Value, out var feature) ? feature : null;
            _anchor = Data.IsMove ? Origin : _placement.Nearest(Data.DefinitionId);
            Lift(Data.DistrictId, Data.FeatureCell);
            _sounds.Play(SoundIds.GHOST_LIFT);
            _ghost.Begin(!Data.IsMove);
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
            _workArea.Hide();
            Lift(null, null);
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
            _ghost.Grab();
            _ghost.SetHeld(true);
            return true;
        }

        public void Drag(ModuleVector2Int cell) => MoveTo(new ModuleVector2Int(cell.X - _grab.X, cell.Y - _grab.Y));

        public void EndDrag()
        {
            _sounds.Play(SoundIds.GHOST_PLANT);
            _ghost.SetHeld(false);
        }

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
            var anchor = _anchor.Value;
            return cell.X >= anchor.X && cell.X < anchor.X + Width && cell.Y >= anchor.Y && cell.Y < anchor.Y + Height;
        }

        // Faint at its old address while its ghost is out; null puts back whatever was lifted.
        private void Lift(string district, ModuleVector2Int? cell)
        {
            if (_liftedDistrict != null) _cityView.ViewOf(_liftedDistrict)?.SetLifted(false);
            _liftedDistrict = district;
            if (district != null) _cityView.ViewOf(district)?.SetLifted(true);
            _groundView.SetLifted(cell);
        }

        private void OnBuild()
        {
            if (!_anchor.HasValue) return;

            if (Data.MovesDistrict) PutDown(_construction.Move(Data.DistrictId, _anchor.Value, _clock.NowMs) == ConstructionRefusal.None, true);
            else if (Data.MovesFeature)
                PutDown(_transplanting.Move(Data.FeatureCell.Value, _anchor.Value, _clock.NowMs) == TransplantRefusal.None, false);
            else
            {
                var refusal = _construction.Build(Data.DefinitionId, _anchor.Value, _clock.NowMs);
                if (refusal == ConstructionRefusal.NoFreeBuilder)
                {
                    // Every builder busy: the builder sheet, its free row offering this very build.
                    _sounds.Play(SoundIds.ERROR);
                    var offer = _construction.Offer(Data.DefinitionId, _anchor);
                    _ = _ui.ShowMenu<BuilderMenu, BuilderOrder>(new BuilderOrder
                    {
                        Verb = _localizer.Tr("Build"),
                        What = _localizer.Tr("Ready to build the {name}", ("name", _localizer.Tr(Building.DisplayName))),
                        Price = _prices.Of(offer.Price, offer.Goods),
                        Start = OnBuild,
                    });
                    return;
                }

                PutDown(refusal == ConstructionRefusal.None, false);
            }
        }

        // Down: the thud, and back to the map — or to the card the move was started from.
        private async void PutDown(bool done, bool reopenCard)
        {
            if (!done)
            {
                _sounds.Play(SoundIds.ERROR);
                _ghost.Shake();
                Refresh();
                return;
            }

            _sounds.Play(SoundIds.BUILD_PLACED);
            Land();
            var district = Data.DistrictId;
            await _ui.CloseAll();
            if (reopenCard) _ = _ui.ShowMenu<DistrictCardMenu, string>(district);
        }

        // What now stands where the ghost was falls from its height onto the plot.
        private void Land()
        {
            if (!_anchor.HasValue || _landing == null) return;
            var anchor = _anchor.Value;
            var lift = _ghost.Lift;
            // Planted: the ghost is gone at once, not when the menu has faded.
            _ghost.Hide();
            _workArea.Hide();
            Lift(null, null);
            if (Data.MovesFeature)
            {
                _landing.Feature(anchor, lift);
                return;
            }

            var district = Data.MovesDistrict ? District : _city.Districts.LastOrDefault(d => d.Anchor == anchor);
            if (district != null) _landing.District(district.Id, anchor, Width, Height, lift);
        }

        private void CentreOnGhost()
        {
            if (!_anchor.HasValue) return;

            var corners = ProvinceGeometry.Corners(_map, _anchor.Value, Width, Height);
            var centre = (corners[0] + corners[2]) / 2f;
            _camera.CenterOn(new ModuleVector2(centre.x, centre.y), GHOST_ON_SCREEN);
        }

        private void Refresh()
        {
            if (Data.MovesFeature) RefreshFeature();
            else if (Data.MovesDistrict) RefreshDistrictMove();
            else RefreshBuild();
        }

        private void RefreshBuild()
        {
            var building = Building;
            var offer = _construction.Offer(Data.DefinitionId, _anchor);
            var problem = _anchor.HasValue ? _placement.Check(Data.DefinitionId, _anchor.Value) : PlacementProblem.OutsideProvince;
            ShowGhost(building.ArtFor(1), null, problem);
            ShowNeighbours(null);

            var price = _prices.Of(offer.Price, offer.Goods);

            View.Show(new PlacementPanelData(
                _localizer.Capitalized(_localizer.Tr(building.DisplayName)),
                offer.Numbered ? "#" + _numbers.Number(offer.Ordinal) : string.Empty,
                building.ArtFor(1), _localizer.Tr(building.Promise), _numbers.Duration(offer.Seconds), price,
                problem == PlacementProblem.None && offer.Refusal is ConstructionRefusal.None or ConstructionRefusal.NoFreeBuilder,
                Reason(problem, offer.Refusal == ConstructionRefusal.NoFreeBuilder ? ConstructionRefusal.None : offer.Refusal, _localizer.Tr("Nowhere legal to build it")), _localizer.Tr("Build")));
        }

        // A move is free and at once: neither a wait nor a price.
        private void RefreshDistrictMove()
        {
            var building = Building;
            var district = District;
            var art = building.ArtFor(district?.Level ?? 1);
            var problem = _anchor.HasValue && district != null ? _placement.Check(Data.DefinitionId, _anchor.Value, district.Id) : PlacementProblem.OutsideProvince;
            ShowGhost(art, null, problem);
            ShowNeighbours(district?.Id);

            View.Show(new PlacementPanelData(
                _localizer.Capitalized(_localizer.Tr(building.DisplayName)),
                district != null && _construction.MaxCount(building) != 1 ? "#" + _numbers.Number(district.Ordinal) : string.Empty,
                art, Unmoved ? _localizer.Tr("Drag it, or tap where it should go") : _localizer.Tr(building.Promise), string.Empty,
                System.Array.Empty<PriceTerm>(), problem == PlacementProblem.None,
                Reason(problem, ConstructionRefusal.None, _localizer.Tr("Nowhere legal to put it")), _localizer.Tr("Move")));
        }

        // The price of moving a tree or a crop plot is the wait, so the wait is said before the button.
        private void RefreshFeature()
        {
            var feature = _feature != null && _features.TryGet(_feature, out var definition) ? definition as FeatureAsset : null;
            var art = feature != null && feature.TileFor(1) is VariantTile tile ? tile.SpriteAt(Data.FeatureCell.Value) : null;
            var problem = _anchor.HasValue ? _transplanting.LandingProblem(Data.FeatureCell.Value, _anchor.Value) : PlacementProblem.OutsideProvince;
            ShowGhost(art, art != null ? art.rect.width / art.pixelsPerUnit : (float?)null, problem);

            View.Show(new PlacementPanelData(
                _localizer.Capitalized(_localizer.Tr(feature?.DisplayName ?? _feature ?? string.Empty)), string.Empty, art,
                Unmoved ? _localizer.Tr("Drag it, or tap where it should go") : _localizer.Tr("It grows again where it lands"),
                _numbers.Duration(_transplanting.GrowSeconds(Data.FeatureCell.Value)), System.Array.Empty<PriceTerm>(),
                problem == PlacementProblem.None, Reason(problem, ConstructionRefusal.None, _localizer.Tr("Nowhere legal to put it")),
                _localizer.Tr("Move")));
        }

        // Not carried anywhere yet: still on the cell it started from.
        private bool Unmoved => _anchor.HasValue && Origin.HasValue && _anchor.Value == Origin.Value;

        // What the plot would do: on each standing neighbour what it would gain, rule by rule, and on the plot what it
        // would receive. Gold as a signed figure; a time as a percentage, good when it falls.
        // A producer shows its work area instead: the ground its crew would reach, the features it would work rimmed,
        // and what each of them holds. A crop plot is the resource, so what it would hold goes on the ghost.
        private void ShowNeighbours(string exclude)
        {
            if (!_anchor.HasValue)
            {
                _ghost.Pills.Hide();
                _workArea.Hide();
                return;
            }

            var preview = _adjacency.Preview(Data.DefinitionId, _anchor.Value, exclude);
            var labels = new List<(Vector3, string, MapPills.Tone)>();
            foreach (var (district, stat, magnitude) in preview.Given) labels.Add(Pill(district.Anchor, stat, magnitude));
            foreach (var (stat, total) in preview.Received) labels.Add(Pill(_anchor.Value, stat, total));

            var building = Building;
            if (building.Production.Plants != null && Held(_anchor.Value, building.Production.Plants) is { } provided) labels.Add(provided);

            var reaching = building.Production.HarvestSources.Count > 0 && building.Production.InfluenceRadiusPerLevel.Count > 0;
            if (reaching)
            {
                var plot = new DistrictState { Id = exclude, DefinitionId = Data.DefinitionId, Anchor = _anchor.Value, Level = District?.Level ?? 1 };
                var worked = _crews.Workable(plot);
                _workArea.Show(_crews.Reach(plot).Where(_province.Contains).ToList(), worked);
                labels = worked.Select(Holding).Where(l => l.HasValue).Select(l => l.Value).ToList();
            }
            else
            {
                _workArea.Hide();
            }

            _ghost.Pills.Show(labels, _map.Grid.cellSize.x * PILL_HEIGHT);
        }

        // What a cell holds when full, toned against the authored stock: richer ground green, poorer red.
        private (Vector3, string, MapPills.Tone)? Holding(ModuleVector2Int cell)
        {
            var source = _harvesting.SourceAt(cell);
            if (source == null) return null;
            return Label(cell, source, _harvesting.FullStock(cell, source));
        }

        private (Vector3, string, MapPills.Tone)? Held(ModuleVector2Int cell, string feature)
        {
            if (!_features.TryGet(feature, out var definition) || definition.Source == null) return null;
            var source = _harvesting.Source(definition.Source);
            return source == null ? null : Label(cell, source, _harvesting.FullStock(cell, source));
        }

        private (Vector3, string, MapPills.Tone) Label(ModuleVector2Int cell, Kingdom.Harvest.IHarvestSource source, double held)
        {
            var at = _map.CellCentre(cell) - new Vector3(0, _map.Grid.cellSize.y * 0.4f, 0);
            var tone = held > source.Stock ? MapPills.Tone.Good : held < source.Stock ? MapPills.Tone.Bad : MapPills.Tone.Plain;
            return (at, "<sprite name=\"" + source.Currency + "\"> " + _numbers.Count(held), tone);
        }

        private (Vector3, string, MapPills.Tone) Pill(ModuleVector2Int cell, AdjacencyStat stat, double value)
        {
            var at = _map.CellCentre(cell) - new Vector3(0, _map.Grid.cellSize.y * 0.4f, 0);
            if (stat == AdjacencyStat.GoldPerMinute)
                return (at, "<sprite name=\"Gold\"> " + Signed(value), value < 0 ? MapPills.Tone.Bad : MapPills.Tone.Good);
            return (at, "<sprite name=\"hourglass\"> " + Signed(Math.Round(value * 100)) + "%", value > 0 ? MapPills.Tone.Bad : MapPills.Tone.Good);
        }

        private string Signed(double value) => (value < 0 ? "\u2212" : "+") + _numbers.Exact(Math.Abs(value));

        // The ghost on its plot, white or red; a feature's drawing at its own size.
        private void ShowGhost(Sprite art, float? artWidth, PlacementProblem problem)
        {
            if (!_anchor.HasValue)
            {
                _ghost.Hide();
                return;
            }

            var (basePosition, width) = ProvinceGeometry.Footprint(_map, _anchor.Value, Width, Height);
            _steps.Clear();
            foreach (var d in STEPS)
                if (_map.Contains(new ModuleVector2Int(_anchor.Value.X + d.x, _anchor.Value.Y + d.y))) _steps.Add(d);
            var origin = _map.CellCentre(_anchor.Value);
            _ghost.SetSteps(_steps, _map.CellCentre(new ModuleVector2Int(_anchor.Value.X + 1, _anchor.Value.Y)) - origin,
                _map.CellCentre(new ModuleVector2Int(_anchor.Value.X, _anchor.Value.Y + 1)) - origin, new Vector2Int(Width, Height));
            _ghost.Show(art, basePosition, artWidth ?? width, ProvinceGeometry.Corners(_map, _anchor.Value, Width, Height),
                problem == PlacementProblem.None);
        }

        private string Reason(PlacementProblem problem, ConstructionRefusal refusal, string nowhere)
        {
            if (!_anchor.HasValue) return nowhere;

            switch (problem)
            {
                case PlacementProblem.Occupied: return _localizer.Tr("Something stands there");
                case PlacementProblem.InFog: return _localizer.Tr("Clear the fog off it first");
                case PlacementProblem.NeedsLand: return _localizer.Tr("It needs dry land");
                case PlacementProblem.OutsideProvince: return _localizer.Tr("Outside the province");
                case PlacementProblem.CountLimit: return _localizer.Tr("No more of these at this Townhall");
                case PlacementProblem.LairZone: return _localizer.Tr("A lair holds this ground");
            }

            return refusal switch
            {
                ConstructionRefusal.NoFreeBuilder => _localizer.Tr("Every builder is busy"),
                ConstructionRefusal.NeedsHarmony => _localizer.Tr("Needs more Harmony"),
                ConstructionRefusal.NotEnoughGoods => _localizer.Tr("Not enough refined goods — queue some at a workshop"),
                _ => string.Empty,
            };
        }
    }
}
