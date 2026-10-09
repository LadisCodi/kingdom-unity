using System.Collections.Generic;
using Codigames.Game.Fog;
using Codigames.Game.Map;
using Codigames.Kingdom.Tutorial;
using UnityEngine;

namespace Codigames.Game.UI.Stage
{
    // A map target's glow: the plot's own diamond drawn on the ground in the pointer's blue magic, over the fog's
    // floor and under what stands on it (its trees, its clouds), breathing slowly.
    public class PlotGlow
    {
        // Just over the fog's floor (-50); the clouds and what stands on the ground sort above, by depth.
        private const int ON_THE_GROUND = -49;
        private const float INSET = 0.94f;
        private const float BEAT_SECONDS = 1.4f;
        private static readonly Color OUTER = new Color32(0x3c, 0x9d, 0xff, 0xff);
        private static readonly Color INNER = new Color32(0xc8, 0xf0, 0xff, 0xff);

        private readonly ProvinceMap _map;
        private readonly SightArt _art;
        private readonly List<(Vector3, Vector3)> _edges = new();
        private readonly List<(float, Color)> _layers = new();
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private MapTarget? _shown;

        public PlotGlow(ProvinceMap map, SightArt art)
        {
            _map = map;
            _art = art;
        }

        // The glow on a plot, this frame; null takes it off.
        public void Show(MapTarget? target)
        {
            if (target == null)
            {
                if (_renderer != null) _renderer.enabled = false;
                _shown = null;
                return;
            }

            Ensure();
            if (!_shown.Equals(target)) Outline(target.Value);
            _shown = target;
            _renderer.enabled = true;

            var beat = 0.4f + 0.6f * (0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / BEAT_SECONDS * Mathf.PI * 2f));
            var cell = _map.Grid.cellSize.x;
            _layers.Clear();
            _layers.Add((Mathf.Max(0.3f * cell, 0.08f), Faded(OUTER, 0.35f * beat)));
            _layers.Add((0.17f * cell, Faded(OUTER, 0.6f * beat)));
            _layers.Add((0.05f * cell, INNER));
            GlowLines.Build(_mesh, _edges, _layers);
        }

        private void Outline(MapTarget target)
        {
            var corners = ProvinceGeometry.Corners(_map, target.Anchor, target.Width, target.Height);
            var centre = (corners[0] + corners[2]) / 2f;
            _edges.Clear();
            for (var i = 0; i < 4; i++)
            {
                var a = centre + (corners[i] - centre) * INSET;
                var b = centre + (corners[(i + 1) % 4] - centre) * INSET;
                _edges.Add((a, b));
            }
        }

        private void Ensure()
        {
            if (_renderer != null) return;
            var go = new GameObject("PlotGlow");
            go.AddComponent<MeshFilter>().sharedMesh = _mesh = new Mesh { name = "PlotGlow" };
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _art.Border;
            _renderer.sortingOrder = ON_THE_GROUND;
        }

        private static Color Faded(Color colour, float alpha) => new(colour.r, colour.g, colour.b, alpha);
    }
}
