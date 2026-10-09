using Codigames.Game.Map;
using Codigames.Modules.Audio;
using Codigames.Modules.Cameras;
using Codigames.Modules.Grid;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Audio
{
    // One looping bed under the music, chosen from the ground at the camera's centre: waves near water, cold wind
    // over snow, birdsong elsewhere. Beds crossfade as the view pans between them.
    public class AmbienceDirector : ITickable
    {
        private const float CHECK_SECONDS = 1f;
        private const string WATER = "Water";
        private const string SNOW = "Snow";

        private readonly ISoundService _sounds;
        private readonly MusicSettings _settings;
        private readonly ICameraRig _camera;
        private readonly ProvinceMap _map;

        private string _bed;
        private ISoundLoop _playing;
        private float _nextCheck;

        public AmbienceDirector(ISoundService sounds, MusicSettings settings, ICameraRig camera, ProvinceMap map)
        {
            _sounds = sounds;
            _settings = settings;
            _camera = camera;
            _map = map;
        }

        public void Tick()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CHECK_SECONDS;

            var centre = _camera.Position;
            var bed = BedAt(_map.CellAt(new Vector3(centre.X, centre.Y, 0f)));
            if (bed == _bed) return;

            _playing?.Stop(_settings.AmbienceFade);
            _bed = bed;
            _playing = _sounds.StartLoop(bed, _settings.AmbienceVolume, _settings.AmbienceFade);
        }

        private string BedAt(ModuleVector2Int cell)
        {
            if (_map.TerrainAt(cell) == SNOW) return _settings.Snow;

            foreach (var near in GridMath.AroundRect(cell, 1, 1, _settings.CoastReach))
            {
                if (_map.TerrainAt(near) == WATER) return _settings.Coast;
            }

            return _settings.Meadow;
        }
    }
}
