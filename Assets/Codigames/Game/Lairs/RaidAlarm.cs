using System.Collections.Generic;
using Codigames.Game.Audio;
using Codigames.Modules.Audio;
using VContainer.Unity;

namespace Codigames.Game.Lairs
{
    // A raid sounds the alarm: once a frame however many landed in it, an absence's raids included.
    public class RaidAlarm : IStartable, ITickable, System.IDisposable
    {
        private readonly Kingdom.Lairs.Lairs _lairs;
        private readonly ISoundService _sounds;
        private bool _raided;

        public RaidAlarm(Kingdom.Lairs.Lairs lairs, ISoundService sounds)
        {
            _lairs = lairs;
            _sounds = sounds;
        }

        public void Start() => _lairs.Raided += OnRaided;

        public void Dispose() => _lairs.Raided -= OnRaided;

        public void Tick()
        {
            if (!_raided) return;
            _raided = false;
            _sounds.Play(SoundIds.RAID_ALARM);
        }

        private void OnRaided(string lair, double at, IReadOnlyDictionary<string, double> took) => _raided = true;
    }
}
