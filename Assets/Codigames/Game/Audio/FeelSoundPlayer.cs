using Codigames.Modules.Audio;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace Codigames.Game.Audio
{
    // Plays the game's sounds through Feel's MMSoundManager, the one sound channel: feedbacks play through it
    // too, so a track's volume reaches every sound. A take is picked at random and pitched by its wobble.
    public class FeelSoundPlayer : ISoundPlayer
    {
        private readonly MMSoundManager _manager;

        public FeelSoundPlayer(MMSoundManager manager)
        {
            _manager = manager;
        }

        public void Play(ISound sound, float volume)
        {
            if (sound is not UnitySound unitySound || unitySound.Takes == 0) return;

            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = ToManagerTrack(sound.Track);
            options.Volume = volume * unitySound.Volume;
            options.Pitch = unitySound.Rate * (1f + Random.Range(-unitySound.PitchJitter, unitySound.PitchJitter));
            _manager.PlaySound(unitySound.Take(Random.Range(0, unitySound.Takes)), options);
        }

        public ISoundLoop StartLoop(ISound sound, float volume, float fadeSeconds)
        {
            if (sound is not UnitySound unitySound || unitySound.Takes == 0) return null;

            var level = volume * unitySound.Volume;
            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = ToManagerTrack(sound.Track);
            options.Loop = true;
            options.Volume = level;
            options.Pitch = unitySound.Rate;
            options.Persistent = true;
            options.Fade = fadeSeconds > 0;
            options.FadeInitialVolume = 0f;
            options.FadeDuration = fadeSeconds;
            options.FadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuadratic);
            var source = _manager.PlaySound(unitySound.Take(0), options);
            return source == null ? null : new Loop(_manager, source, unitySound.Volume);
        }

        public void SetVolume(SoundTrack track, float volume) => _manager.SetTrackVolume(ToManagerTrack(track), volume);

        private static MMSoundManager.MMSoundManagerTracks ToManagerTrack(SoundTrack track) => track switch
        {
            SoundTrack.Ui => MMSoundManager.MMSoundManagerTracks.UI,
            SoundTrack.Music => MMSoundManager.MMSoundManagerTracks.Music,
            SoundTrack.Ambience => MMSoundManager.MMSoundManagerTracks.Other,
            _ => MMSoundManager.MMSoundManagerTracks.Sfx,
        };

        private sealed class Loop : ISoundLoop
        {
            private readonly MMSoundManager _manager;
            private readonly AudioSource _source;
            private readonly float _level;

            public Loop(MMSoundManager manager, AudioSource source, float level)
            {
                _manager = manager;
                _source = source;
                _level = level;
            }

            public void FadeTo(float volume, float seconds)
            {
                if (_source == null) return;
                _manager.FadeSound(_source, Mathf.Max(0.01f, seconds), _source.volume, volume * _level,
                    new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuadratic));
            }

            public void Stop(float fadeSeconds)
            {
                if (_source == null) return;
                _manager.FadeSound(_source, Mathf.Max(0.01f, fadeSeconds), _source.volume, 0f,
                    new MMTweenType(MMTween.MMTweenCurve.EaseInOutQuadratic), true);
            }
        }
    }
}
