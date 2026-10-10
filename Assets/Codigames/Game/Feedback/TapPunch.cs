using System.Collections.Generic;
using Codigames.Game.City;
using Codigames.Game.Map;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Feedback
{
    // The squash and stretch a tap gives what it lands on, as the web draws it (tapFx.ts): squashed wide and short on
    // impact, then rebounding past its size at a different rate on each axis before settling, over 300 ms. A crew's
    // strike is the same gesture, weaker. What stands on a cell punches its tile; a building its art. The player's
    // own tap also flashes white — the sprite added onto itself, fading fast — and a strike never does: the flash is
    // what says "that was me". A tap on the fog flashes the cell.
    public class TapPunch : ITickable
    {
        private const float DURATION = 0.3f;
        // In front of what it flashes on the sort axis, by less than anything else stands apart.
        private const float IN_FRONT = 0.002f;

        private readonly ProvinceMap _map;
        private readonly CityView _city;
        private readonly TapFlashArt _art;
        private readonly Dictionary<ModuleVector2Int, (float Start, float Strength)> _cells = new();
        private readonly Dictionary<string, (float Start, float Strength)> _districts = new();
        private readonly Dictionary<ModuleVector2Int, float> _fog = new();
        private readonly Dictionary<ModuleVector2Int, SpriteRenderer> _cellFlashes = new();
        private readonly Dictionary<ModuleVector2Int, SpriteRenderer> _fogFlashes = new();
        private readonly Stack<SpriteRenderer> _spare = new();
        private readonly List<ModuleVector2Int> _doneCells = new();
        private readonly List<string> _doneDistricts = new();
        private Transform _root;

        public TapPunch(ProvinceMap map, CityView city, TapFlashArt art = null)
        {
            _map = map;
            _city = city;
            _art = art;
        }

        // `strength` 1 is the player's tap; a crew's strike is weaker.
        public void Cell(ModuleVector2Int cell, float strength = 1f) => _cells[cell] = (Time.unscaledTime, strength);

        public void District(string districtId, float strength = 1f) => _districts[districtId] = (Time.unscaledTime, strength);

        // A tap that paid into the fog, the last one too as it clears.
        public void Fog(ModuleVector2Int cell) => _fog[cell] = Time.unscaledTime;

        public void Tick()
        {
            var now = Time.unscaledTime;

            _doneCells.Clear();
            foreach (var (cell, punch) in _cells)
            {
                var position = ProvinceCoordinates.ToTilemap(cell);
                var elapsed = now - punch.Start;
                var scale = Sample(elapsed, punch.Strength, out var done);
                var layer = _map.FeatureLayer(position);
                layer.SetTileFlags(position, TileFlags.None);
                layer.SetTransformMatrix(position, Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1f)));
                FlashTile(cell, position, scale, done ? 0f : Flash(elapsed, punch.Strength));
                if (done) _doneCells.Add(cell);
            }
            foreach (var cell in _doneCells) _cells.Remove(cell);

            _doneDistricts.Clear();
            foreach (var (id, punch) in _districts)
            {
                var elapsed = now - punch.Start;
                var scale = Sample(elapsed, punch.Strength, out var done);
                var view = _city.ViewOf(id);
                if (view != null)
                {
                    view.SetPunch(scale);
                    if (_art != null) view.SetFlash(done ? 0f : Flash(elapsed, punch.Strength), _art.Flash);
                }

                if (done) _doneDistricts.Add(id);
            }
            foreach (var id in _doneDistricts) _districts.Remove(id);

            _doneCells.Clear();
            foreach (var (cell, start) in _fog)
            {
                var elapsed = now - start;
                var done = elapsed >= DURATION;
                FlashFog(cell, done ? 0f : Flash(elapsed, 1f) * (_art != null ? _art.FogAlpha : 0f));
                if (done) _doneCells.Add(cell);
            }
            foreach (var cell in _doneCells) _fog.Remove(cell);
        }

        // How much of the flash is left: only the player's tap flashes.
        private static float Flash(float elapsed, float strength)
            => strength >= 1f ? Mathf.Exp(-7f * elapsed / DURATION) : 0f;

        // The tile's own drawing, added onto it in front of it, punched with it.
        private void FlashTile(ModuleVector2Int cell, Vector3Int position, Vector2 scale, float flash)
        {
            var layer = _map.FeatureLayer(position);
            var sprite = flash > 0.004f ? layer.GetSprite(position) : null;
            if (_art == null || sprite == null)
            {
                Release(_cellFlashes, cell);
                return;
            }

            var renderer = Rent(_cellFlashes, cell, _art.Flash);
            var tint = layer.GetColor(position);
            renderer.sprite = sprite;
            renderer.color = new Color(tint.r, tint.g, tint.b, flash);
            renderer.transform.position = _map.Features.GetCellCenterWorld(position) + new Vector3(0f, -IN_FRONT, 0f);
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        // A diamond of the cloud's sunlit tone over the cell.
        private void FlashFog(ModuleVector2Int cell, float alpha)
        {
            if (_art == null || alpha <= 0.004f)
            {
                Release(_fogFlashes, cell);
                return;
            }

            var renderer = Rent(_fogFlashes, cell, null);
            renderer.sprite = _art.Cell;
            renderer.color = new Color(_art.FogFlash.r, _art.FogFlash.g, _art.FogFlash.b, alpha);
            // The cell's own middle: the features layer's anchor is its bottom corner, where their art stands.
            var centre = _map.CellCentre(cell);
            renderer.transform.position = new Vector3(centre.x, centre.y - IN_FRONT, 0f);
            renderer.transform.localScale = Vector3.one;
        }

        private SpriteRenderer Rent(Dictionary<ModuleVector2Int, SpriteRenderer> live, ModuleVector2Int cell, Material material)
        {
            if (live.TryGetValue(cell, out var renderer)) return renderer;
            if (_root == null) _root = new GameObject("TapFlashes").transform;
            renderer = _spare.Count > 0 ? _spare.Pop() : new GameObject("Flash").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(_root, false);
            renderer.sharedMaterial = material != null ? material : _map.Features.GetComponent<TilemapRenderer>().sharedMaterial;
            var source = _map.Features.GetComponent<TilemapRenderer>();
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.gameObject.SetActive(true);
            live[cell] = renderer;
            return renderer;
        }

        private void Release(Dictionary<ModuleVector2Int, SpriteRenderer> live, ModuleVector2Int cell)
        {
            if (!live.Remove(cell, out var renderer)) return;
            renderer.gameObject.SetActive(false);
            _spare.Push(renderer);
        }

        private static Vector2 Sample(float elapsed, float strength, out bool done)
        {
            done = elapsed >= DURATION;
            if (done) return Vector2.one;

            var k = elapsed / DURATION;
            var decay = Mathf.Exp(-4.5f * k) * strength;
            return new Vector2(1f + 0.22f * decay * Mathf.Cos(k * Mathf.PI * 3f), 1f - 0.28f * decay * Mathf.Cos(k * Mathf.PI * 3.8f));
        }
    }
}
