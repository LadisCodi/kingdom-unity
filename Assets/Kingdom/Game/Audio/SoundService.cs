using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace Kingdom.Game.Audio
{
    // A thin layer over Feel's MMSoundManager, the game's one sound channel: feedbacks play through it too,
    // so a track's volume reaches every sound.
    public class SoundService : ISoundService
    {
        private readonly MMSoundManager _manager;
        private readonly SoundCatalog _catalog;
        private readonly HashSet<string> _warned = new();

        public SoundService(MMSoundManager manager, SoundCatalog catalog)
        {
            _manager = manager;
            _catalog = catalog;
        }

        public void Play(string soundId, float volume = 1f)
        {
            if (!_catalog.TryGet(soundId, out var clip, out var track))
            {
                if (_warned.Add(soundId)) Debug.LogWarning($"#Audio# No clip for sound '{soundId}' in the SoundCatalog.");
                return;
            }

            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = ToManagerTrack(track);
            options.Volume = volume;
            _manager.PlaySound(clip, options);
        }

        public void SetVolume(SoundTrack track, float volume) => _manager.SetTrackVolume(ToManagerTrack(track), volume);

        private static MMSoundManager.MMSoundManagerTracks ToManagerTrack(SoundTrack track) => track switch
        {
            SoundTrack.Ui => MMSoundManager.MMSoundManagerTracks.UI,
            SoundTrack.Music => MMSoundManager.MMSoundManagerTracks.Music,
            SoundTrack.Ambience => MMSoundManager.MMSoundManagerTracks.Other,
            _ => MMSoundManager.MMSoundManagerTracks.Sfx,
        };
    }
}
