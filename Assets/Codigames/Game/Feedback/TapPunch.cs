using System.Collections.Generic;
using Codigames.Game.City;
using Codigames.Game.Map;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Feedback
{
    // The squash and stretch a tap gives what it lands on, as the web draws it: squashed wide and short on impact,
    // then rebounding past its size at a different rate on each axis before settling, over 300 ms. A crew's
    // strike is the same gesture, weaker. What stands on a cell punches its tile; a building its art.
    public class TapPunch : ITickable
    {
        private const float DURATION = 0.3f;

        private readonly ProvinceMap _map;
        private readonly CityView _city;
        private readonly Dictionary<ModuleVector2Int, (float Start, float Strength)> _cells = new();
        private readonly Dictionary<string, (float Start, float Strength)> _districts = new();
        private readonly List<ModuleVector2Int> _doneCells = new();
        private readonly List<string> _doneDistricts = new();

        public TapPunch(ProvinceMap map, CityView city)
        {
            _map = map;
            _city = city;
        }

        // `strength` 1 is the player's tap; a crew's strike is weaker.
        public void Cell(ModuleVector2Int cell, float strength = 1f) => _cells[cell] = (Time.unscaledTime, strength);

        public void District(string districtId, float strength = 1f) => _districts[districtId] = (Time.unscaledTime, strength);

        public void Tick()
        {
            var now = Time.unscaledTime;

            _doneCells.Clear();
            foreach (var (cell, punch) in _cells)
            {
                var position = ProvinceCoordinates.ToTilemap(cell);
                var scale = Sample(now - punch.Start, punch.Strength, out var done);
                _map.Features.SetTileFlags(position, TileFlags.None);
                _map.Features.SetTransformMatrix(position, Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1f)));
                if (done) _doneCells.Add(cell);
            }
            foreach (var cell in _doneCells) _cells.Remove(cell);

            _doneDistricts.Clear();
            foreach (var (id, punch) in _districts)
            {
                var scale = Sample(now - punch.Start, punch.Strength, out var done);
                _city.ViewOf(id)?.SetPunch(scale);
                if (done) _doneDistricts.Add(id);
            }
            foreach (var id in _doneDistricts) _districts.Remove(id);
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
