using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.City;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using Vector3 = UnityEngine.Vector3;

namespace Codigames.Game.Harvest
{
    // A FIELD BEING SOWN — a crop plot planted or repaired, for the seconds before it can be reaped — is worked like a
    // building (Docs/features/27-plantables.md §2): the builder's hammer over it and the construction's bar with the
    // time left, so repairing an old plot reads as the same few seconds as repairing the House.
    public class SownPlotsView : IStartable, ITickable, IDisposable
    {
        private const float BAR_HEIGHT = 0.2f;
        private const float HAMMER_CELLS = 1.2f;
        private const float HAMMER_TOP = 0.85f;
        private const float CHECK_SECONDS = 0.5f;

        private readonly GroundState _ground;
        private readonly Harvesting _harvesting;
        private readonly FogOfWar _fog;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ProvinceMap _map;
        private readonly SownPlot _prefab;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Dictionary<ModuleVector2Int, SownPlot> _shown = new();
        private readonly Stack<SownPlot> _pool = new();
        private HashSet<string> _sown;
        private Transform _parent;
        private float _nextCheck;

        public SownPlotsView(GroundState ground, Harvesting harvesting, FogOfWar fog, ICatalog<IBuildingDefinition> buildings, ProvinceMap map,
            SownPlot prefab, IClock clock, NumberFormat numbers)
        {
            _ground = ground;
            _harvesting = harvesting;
            _fog = fog;
            _buildings = buildings;
            _map = map;
            _prefab = prefab;
            _clock = clock;
            _numbers = numbers;
        }

        public void Start()
        {
            // What is sown: the features a plantable puts on the ground.
            _sown = new HashSet<string>(_buildings.Items.Select(b => b.Production.Plants).Where(p => p != null));
            _parent = new GameObject("SownPlots").transform;
            _harvesting.DepotChanged += OnDepotChanged;
        }

        public void Dispose() => _harvesting.DepotChanged -= OnDepotChanged;

        private void OnDepotChanged(ModuleVector2Int cell) => _nextCheck = 0;

        public void Tick()
        {
            var now = _clock.NowMs;
            if (Time.unscaledTime >= _nextCheck)
            {
                _nextCheck = Time.unscaledTime + CHECK_SECONDS;
                Sync(now);
            }

            foreach (var (cell, plot) in _shown)
            {
                var progress = _harvesting.Regrowth(cell, now) ?? 1;
                var remaining = Math.Max(0, (_harvesting.ExhaustedUntil(cell) ?? now) - now) / 1000;
                plot.Bar.SetFraction((float)progress);
                plot.Bar.SetLabel(_numbers.Duration(Math.Max(1, Math.Ceiling(remaining))));
            }
        }

        // A plot on every sown cell growing on the kingdom's own ground; none elsewhere.
        private void Sync(double now)
        {
            var wanted = _ground.Features.Where(f => _sown.Contains(f.Value) && _fog.IsRevealed(f.Key) && _harvesting.IsGrowing(f.Key, now))
                .Select(f => f.Key).ToHashSet();

            foreach (var cell in _shown.Keys.Where(c => !wanted.Contains(c)).ToList())
            {
                _shown[cell].gameObject.SetActive(false);
                _pool.Push(_shown[cell]);
                _shown.Remove(cell);
            }

            foreach (var cell in wanted.Where(c => !_shown.ContainsKey(c))) _shown[cell] = Show(cell);
        }

        private SownPlot Show(ModuleVector2Int cell)
        {
            var plot = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, _parent);
            plot.gameObject.SetActive(true);
            plot.transform.position = _map.CellCentre(cell);
            var cellSize = _map.Grid.cellSize;
            plot.Bar.SetSize(Mathf.Max(BAR_HEIGHT * 4f, cellSize.x * 0.6f), BAR_HEIGHT);
            var hammerWidth = Mathf.Min(cellSize.x, HAMMER_CELLS);
            plot.Hammer.Place(new Vector3(-hammerWidth / 2f, cellSize.y * HAMMER_TOP, 0f), hammerWidth, $"sow:{cell.X},{cell.Y}");
            return plot;
        }
    }
}
