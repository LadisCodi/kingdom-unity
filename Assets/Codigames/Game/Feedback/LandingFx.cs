using System.Collections.Generic;
using Codigames.Game.City;
using Codigames.Game.Map;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Feedback
{
    // A ghost planted (the web's ghostFx.ts land): what now stands on the plot falls the rest of the way from the
    // ghost's height — a little hop first, stretching as it goes up — then squashes into its plot, and a ring of dust
    // puffs rolls out from the front and sides of the plot as it hits, never the back, which is behind the building.
    // A building's art moves; a moved tree's tile does.
    public class LandingFx : ITickable
    {
        private const float HOP_SECONDS = 0.08f;
        private const float HOP = 0.1f;
        private const float FALL_SECONDS = 0.13f;
        private const float SQUASH_SECONDS = 0.32f;
        private const float DUST_SECONDS = 0.7f;
        private const int PUFFS = 12;
        // Over the buildings, as the web draws its markers.
        private const int DUST_ORDER = 560;
        private static readonly float[] VARY = { 1f, 0.75f, 1.15f, 0.85f, 1.25f, 0.8f, 1.1f, 0.7f, 1.2f, 0.9f, 1.05f, 0.8f };
        private static readonly Color DUST = new(0.788f, 0.706f, 0.545f, 1f);
        private static readonly Color DUST_LIGHT = new(0.965f, 0.933f, 0.867f, 1f);

        private sealed class Landing
        {
            public string District;
            public ModuleVector2Int Cell;
            public float At;
            public float From;
        }

        private sealed class Dust
        {
            public Vector3 Centre;
            public float Width;
            public float Height;
            public float At;
            public SpriteRenderer[] Dark;
            public SpriteRenderer[] Light;
        }

        private readonly ProvinceMap _map;
        private readonly CityView _city;
        private readonly List<Landing> _landings = new();
        private readonly List<Dust> _dusts = new();
        private readonly Stack<SpriteRenderer> _spare = new();
        private Transform _root;
        private Sprite _disc;

        public LandingFx(ProvinceMap map, CityView city)
        {
            _map = map;
            _city = city;
        }

        // A building planted on `anchor` (a build or a move), falling from `fromLift` plot widths.
        public void District(string districtId, ModuleVector2Int anchor, int width, int height, float fromLift)
        {
            _landings.Add(new Landing { District = districtId, At = Time.unscaledTime, From = Mathf.Max(fromLift, 0.07f) });
            AddDust(anchor, width, height);
        }

        // A tree or a crop plot set down on `cell`.
        public void Feature(ModuleVector2Int cell, float fromLift)
        {
            _landings.Add(new Landing { Cell = cell, At = Time.unscaledTime, From = Mathf.Max(fromLift, 0.07f) });
            AddDust(cell, 1, 1);
        }

        public void Tick()
        {
            var now = Time.unscaledTime;
            for (var i = _landings.Count - 1; i >= 0; i--)
            {
                var landing = _landings[i];
                var done = !Sample(now - landing.At, landing.From, out var lift, out var scale);
                Apply(landing, done ? 0f : lift, done ? Vector2.one : scale);
                if (done) _landings.RemoveAt(i);
            }

            for (var i = _dusts.Count - 1; i >= 0; i--)
            {
                var dust = _dusts[i];
                var k = (now - dust.At) / DUST_SECONDS;
                if (k >= 1f)
                {
                    foreach (var r in dust.Dark) Release(r);
                    foreach (var r in dust.Light) Release(r);
                    _dusts.RemoveAt(i);
                    continue;
                }

                DrawDust(dust, k);
            }
        }

        // The web's landing curve: hop, fall, squash. False once it has settled.
        private static bool Sample(float t, float from, out float lift, out Vector2 scale)
        {
            var top = from + HOP;
            if (t < HOP_SECONDS)
            {
                var k = t / HOP_SECONDS;
                var up = 1f - (1f - k) * (1f - k);
                lift = from + HOP * up;
                scale = new Vector2(1f - 0.06f * up, 1f + 0.08f * up);
                return true;
            }

            if (t < HOP_SECONDS + FALL_SECONDS)
            {
                var k = (t - HOP_SECONDS) / FALL_SECONDS;
                lift = top * (1f - k * k);
                scale = new Vector2(0.94f + 0.02f * k, 1.08f);
                return true;
            }

            var s = (t - HOP_SECONDS - FALL_SECONDS) / SQUASH_SECONDS;
            lift = 0f;
            if (s >= 1f)
            {
                scale = Vector2.one;
                return false;
            }

            var d = Mathf.Exp(-4.5f * s);
            scale = new Vector2(1f + 0.2f * d * Mathf.Cos(s * Mathf.PI * 3f), 1f - 0.24f * d * Mathf.Cos(s * Mathf.PI * 3.6f));
            return true;
        }

        private void Apply(Landing landing, float lift, Vector2 scale)
        {
            if (landing.District != null)
            {
                var view = _city.ViewOf(landing.District);
                if (view != null) view.SetLanding(lift, scale);
                return;
            }

            var position = ProvinceCoordinates.ToTilemap(landing.Cell);
            var width = _map.Grid.cellSize.x;
            var layer = _map.FeatureLayer(position);
            layer.SetTileFlags(position, TileFlags.None);
            layer.SetTransformMatrix(position,
                Matrix4x4.TRS(new Vector3(0f, lift * width, 0f), Quaternion.identity, new Vector3(scale.x, scale.y, 1f)));
        }

        // The ring starts as the building hits.
        private void AddDust(ModuleVector2Int anchor, int width, int height)
        {
            var corners = ProvinceGeometry.Corners(_map, anchor, width, height);
            var dust = new Dust
            {
                Centre = (corners[0] + corners[2]) / 2f,
                Width = Mathf.Abs(corners[1].x - corners[3].x),
                Height = Mathf.Abs(corners[0].y - corners[2].y),
                At = Time.unscaledTime + HOP_SECONDS + FALL_SECONDS,
                Dark = new SpriteRenderer[PUFFS],
                Light = new SpriteRenderer[PUFFS],
            };
            for (var i = 0; i < PUFFS; i++)
            {
                dust.Dark[i] = Rent(DUST_ORDER);
                dust.Light[i] = Rent(DUST_ORDER + 1);
                dust.Dark[i].enabled = dust.Light[i].enabled = false;
            }

            _dusts.Add(dust);
        }

        // Puffs from the left corner round the front to the right one, each a little bigger or smaller, nearer or
        // further out; growing, rising a little and thinning as they go. Two tones, lit from above.
        private void DrawDust(Dust dust, float k)
        {
            var started = k >= 0f;
            k = Mathf.Max(0f, k);
            var w = dust.Width;
            var h = dust.Height;
            var ease = 1f - (1f - k) * (1f - k);
            var fade = Mathf.Pow(1f - k, 1.5f);
            for (var i = 0; i < PUFFS; i++)
            {
                var a = Mathf.PI * (-0.12f + 1.24f * (i / (float)(PUFFS - 1)));
                var vary = VARY[i];
                var r = 0.92f + (0.3f + 0.12f * vary) * ease;
                // The web's screen y runs down: a positive sine is the front, so it is down here.
                var x = dust.Centre.x + Mathf.Cos(a) * w * 0.5f * r;
                var y = dust.Centre.y - Mathf.Sin(a) * h * 0.5f * r + ease * h * 0.18f * vary;
                var radius = w * (0.06f + 0.1f * ease) * vary;
                Puff(dust.Dark[i], started, new Vector3(x, y, 0f), radius, DUST, 0.75f * fade);
                Puff(dust.Light[i], started, new Vector3(x - radius * 0.15f, y + radius * 0.25f, 0f), radius * 0.62f, DUST_LIGHT, 0.8f * fade);
            }
        }

        private static void Puff(SpriteRenderer renderer, bool on, Vector3 at, float radius, Color colour, float alpha)
        {
            renderer.enabled = on;
            if (!on) return;
            renderer.transform.position = at;
            renderer.transform.localScale = Vector3.one * (radius * 2f);
            renderer.color = new Color(colour.r, colour.g, colour.b, alpha);
        }

        private SpriteRenderer Rent(int order)
        {
            if (_root == null) _root = new GameObject("LandingDust").transform;
            var renderer = _spare.Count > 0 ? _spare.Pop() : new GameObject("Puff").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(_root, false);
            renderer.sprite = Disc();
            renderer.sortingLayerID = _map.Features.GetComponent<TilemapRenderer>().sortingLayerID;
            renderer.sortingOrder = order;
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        private void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            _spare.Push(renderer);
        }

        // A soft-edged disc one unit across, drawn once.
        private Sprite Disc()
        {
            if (_disc != null) return _disc;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Dust disc", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                var alpha = Mathf.Clamp01((1f - d) * size / 2f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _disc = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _disc;
        }
    }
}
