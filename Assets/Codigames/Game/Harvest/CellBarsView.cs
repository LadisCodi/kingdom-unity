using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Harvest.State;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using Vector3 = UnityEngine.Vector3;

namespace Codigames.Game.Harvest
{
    // Where the kingdom's ground stands, cell by cell, as the web marks it: a thin bar across a cell's middle — blue
    // filling while an emptied or planted cell grows back, green for what is left of one drawn on. A plot being sown
    // wears the construction's bar instead (SownPlotsView), and a full cell wears nothing.
    public class CellBarsView : IStartable, ITickable, IDisposable
    {
        private const float CHECK_SECONDS = 0.25f;
        private const float WIDTH = 0.7f;
        private const float HEIGHT = 0.045f;
        private const float BELOW_CENTRE = 0.2f;
        // Just over the fog's floor and a target's glow; what stands on the ground sorts above it.
        private const int ORDER = -48;
        private static readonly Color TROUGH = new(0f, 0f, 0f, 0.55f);
        private static readonly Color RECOVERY = new Color32(0x8a, 0xb4, 0xd8, 0xff);
        private static readonly Color STOCK = new Color32(0x7f, 0xd0, 0x7f, 0xff);

        private readonly HarvestState _state;
        private readonly Harvesting _harvesting;
        private readonly FogOfWar _fog;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ProvinceMap _map;
        private readonly SpriteArt _art;
        private readonly IClock _clock;
        private readonly Dictionary<ModuleVector2Int, (SpriteRenderer Trough, SpriteRenderer Fill)> _shown = new();
        private readonly Stack<(SpriteRenderer, SpriteRenderer)> _pool = new();
        private HashSet<string> _sown;
        private Transform _parent;
        private float _nextCheck;

        public CellBarsView(HarvestState state, Harvesting harvesting, FogOfWar fog, ICatalog<IBuildingDefinition> buildings, ProvinceMap map,
            SpriteArt art, IClock clock)
        {
            _state = state;
            _harvesting = harvesting;
            _fog = fog;
            _buildings = buildings;
            _map = map;
            _art = art;
            _clock = clock;
        }

        public void Start()
        {
            _sown = new HashSet<string>(_buildings.Items.Select(b => b.Production.Plants).Where(p => p != null));
            _parent = new GameObject("CellBars").transform;
            _harvesting.DepotChanged += OnDepotChanged;
        }

        public void Dispose() => _harvesting.DepotChanged -= OnDepotChanged;

        private void OnDepotChanged(ModuleVector2Int cell) => _nextCheck = 0;

        public void Tick()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CHECK_SECONDS;

            var now = _clock.NowMs;
            var wanted = new Dictionary<ModuleVector2Int, (double Fraction, Color Colour)>();
            foreach (var cell in _state.Depots.Keys)
            {
                if (!_fog.IsRevealed(cell) || _harvesting.SourceAt(cell) == null) continue;
                if (_harvesting.IsGrowing(cell, now) && _harvesting.Sown(cell, _sown)) continue;
                var growing = _harvesting.Regrowth(cell, now);
                if (growing.HasValue) wanted[cell] = (growing.Value, RECOVERY);
                else if (_harvesting.StockLeft(cell) < 1) wanted[cell] = (_harvesting.StockLeft(cell), STOCK);
            }

            foreach (var cell in _shown.Keys.Where(c => !wanted.ContainsKey(c)).ToList())
            {
                var bar = _shown[cell];
                bar.Trough.transform.parent.gameObject.SetActive(false);
                _pool.Push(bar);
                _shown.Remove(cell);
            }

            foreach (var (cell, (fraction, colour)) in wanted)
            {
                if (!_shown.TryGetValue(cell, out var bar)) _shown[cell] = bar = Show(cell);
                Fill(bar.Fill, (float)fraction, colour);
            }
        }

        private (SpriteRenderer Trough, SpriteRenderer Fill) Show(ModuleVector2Int cell)
        {
            var bar = _pool.Count > 0 ? _pool.Pop() : Make();
            var root = bar.Item1.transform.parent;
            root.gameObject.SetActive(true);
            var cellSize = _map.Grid.cellSize;
            root.position = _map.CellCentre(cell) - new Vector3(0f, cellSize.y * BELOW_CENTRE, 0f);
            Size(bar.Item1, WIDTH * cellSize.x, 0f);
            return bar;
        }

        // The fill grows from the trough's left end.
        private void Fill(SpriteRenderer fill, float fraction, Color colour)
        {
            var full = WIDTH * _map.Grid.cellSize.x;
            var width = full * Mathf.Clamp01(fraction);
            fill.color = colour;
            Size(fill, width, (width - full) / 2f);
        }

        private (SpriteRenderer, SpriteRenderer) Make()
        {
            var root = new GameObject("Bar").transform;
            root.SetParent(_parent, false);
            return (Renderer("Trough", root, TROUGH, ORDER), Renderer("Fill", root, Color.white, ORDER + 1));
        }

        private SpriteRenderer Renderer(string name, Transform parent, Color colour, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = _art.Pixel;
            renderer.color = colour;
            renderer.sortingOrder = order;
            return renderer;
        }

        // A piece `width` world units long and the bar's height, its middle `x` from the bar's.
        private void Size(SpriteRenderer renderer, float width, float x)
        {
            var native = _art.Pixel.rect.width / _art.Pixel.pixelsPerUnit;
            renderer.transform.localScale = new Vector3(width / native, HEIGHT * _map.Grid.cellSize.x / native, 1f);
            renderer.transform.localPosition = new Vector3(x, 0f, 0f);
        }
    }
}
