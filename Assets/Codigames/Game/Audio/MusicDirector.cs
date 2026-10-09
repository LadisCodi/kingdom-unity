using System.Collections.Generic;
using System.Linq;
using Codigames.Modules.Audio;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Audio
{
    // A moment that takes over the music while its screen is up.
    public enum MusicMoment
    {
        // The deploy sheet: war drums under the mustering.
        Muster,
        // A fight's playback: its tune from the top.
        Battle,
        // The chest reveal.
        Feast,
    }

    // The town's music: a playlist from a random song, each in turn, crossfading — and the moments that take the floor
    // from it (the deploy sheet, a fight, a chest), the strongest of those on: a feast over a battle over a muster.
    public class MusicDirector : IStartable, ITickable
    {
        private readonly ISoundService _sounds;
        private readonly MusicSettings _settings;
        private readonly Playlist _town;
        private readonly HashSet<MusicMoment> _on = new();
        private MusicMoment? _playing;
        private ISoundLoop _loop;
        private bool _started;

        public MusicDirector(ISoundService sounds, MusicSettings settings)
        {
            _sounds = sounds;
            _settings = settings;
            var songs = settings.Town.Select(s => new Playlist.Song(s.SoundId, s.TurnSeconds)).ToList();
            _town = new Playlist(sounds, songs, settings.TownVolume, settings.Crossfade, settings.TownFadeIn,
                Random.Range(0, Mathf.Max(1, songs.Count)));
        }

        public void Start()
        {
            _started = true;
            Settle();
        }

        public void Tick() => _town.Tick(Time.unscaledDeltaTime);

        public void Set(MusicMoment moment, bool on)
        {
            if (on ? !_on.Add(moment) : !_on.Remove(moment)) return;
            if (_started) Settle();
        }

        private void Settle()
        {
            MusicMoment? want = _on.Contains(MusicMoment.Feast) ? MusicMoment.Feast
                : _on.Contains(MusicMoment.Battle) ? MusicMoment.Battle
                : _on.Contains(MusicMoment.Muster) ? MusicMoment.Muster : null;
            if (want == _playing && (want != null || _town.IsPlaying)) return;

            _loop?.Stop(_settings.MomentFadeOut);
            _loop = null;
            if (want == null)
            {
                _town.Play();
            }
            else
            {
                _town.Pause(_settings.MomentFadeOut);
                var (id, volume, fadeIn) = _settings.Moment(want.Value);
                _loop = _sounds.StartLoop(id, volume, fadeIn);
            }

            _playing = want;
        }
    }
}
