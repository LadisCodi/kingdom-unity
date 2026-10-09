using System;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Crews.State;
using Codigames.Modules.Audio;
using Codigames.Modules.Cameras;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Audio
{
    // What the kingdom does by itself makes its noise: a building finished, a villager moving in, a crew's swing
    // (the tap's own foley, at half volume, only on screen and never more than a few at once).
    public class SoundCues : IStartable, IDisposable
    {
        private const float STRIKE_VOLUME = 0.5f;
        private const int STRIKE_VOICES = 3;
        private const float STRIKE_VOICE_SECONDS = 0.35f;

        private readonly ISoundService _sounds;
        private readonly Construction _construction;
        private readonly VillagerTraining _training;
        private readonly Workforce _crews;
        private readonly Kingdom.Harvest.Harvesting _harvesting;
        private readonly ICameraRig _camera;
        private readonly ProvinceMap _map;
        private readonly float[] _voices = new float[STRIKE_VOICES];

        public SoundCues(ISoundService sounds, Construction construction, VillagerTraining training, Workforce crews,
            Kingdom.Harvest.Harvesting harvesting, ICameraRig camera, ProvinceMap map)
        {
            _sounds = sounds;
            _construction = construction;
            _training = training;
            _crews = crews;
            _harvesting = harvesting;
            _camera = camera;
            _map = map;
        }

        public void Start()
        {
            _construction.JobCompleted += OnJobCompleted;
            _training.Arrived += OnArrived;
            _crews.Struck += OnStruck;
        }

        public void Dispose()
        {
            _construction.JobCompleted -= OnJobCompleted;
            _training.Arrived -= OnArrived;
            _crews.Struck -= OnStruck;
        }

        private void OnJobCompleted(ConstructionJob job, DistrictState district) => _sounds.Play(SoundIds.CONSTRUCTION_COMPLETE);

        private void OnArrived() => _sounds.Play(SoundIds.VILLAGER_TRAINED);

        private void OnStruck(WorkerState worker, ModuleVector2Int cell)
        {
            if (!OnScreen(cell) || !TakeVoice()) return;

            var source = _harvesting.SourceAt(cell);
            _sounds.Play(SoundIds.TapOn(source?.Id), STRIKE_VOLUME);
        }

        private bool OnScreen(ModuleVector2Int cell)
        {
            var at = _map.CellCentre(cell);
            var halfHeight = _camera.OrthographicSize;
            var halfWidth = halfHeight * _camera.Aspect;
            return Mathf.Abs(at.x - _camera.Position.X) <= halfWidth && Mathf.Abs(at.y - _camera.Position.Y) <= halfHeight;
        }

        // At most a few strikes sound at once; the rest are dropped.
        private bool TakeVoice()
        {
            var now = Time.unscaledTime;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voices[i] > now) continue;
                _voices[i] = now + STRIKE_VOICE_SECONDS;
                return true;
            }

            return false;
        }
    }
}
