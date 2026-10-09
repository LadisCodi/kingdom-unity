using System;
using System.Collections.Generic;

namespace Codigames.Modules.Audio
{
    // Songs played in turn, each for its turn's length, the next crossfading in over the last seconds of the one
    // playing. Time a song has played counts from its own clock, so a turn that something else interrupted
    // resumes where it was and still ends on time.
    public class Playlist
    {
        private readonly ISoundService _sounds;
        private readonly IReadOnlyList<Song> _songs;
        private readonly float _volume;
        private readonly float _crossfadeSeconds;
        private readonly float _fadeInSeconds;
        private readonly double[] _heard;

        private int _current;
        private ISoundLoop _playing;

        public Playlist(ISoundService sounds, IReadOnlyList<Song> songs, float volume, float crossfadeSeconds, float fadeInSeconds, int first)
        {
            _sounds = sounds;
            _songs = songs;
            _volume = volume;
            _crossfadeSeconds = crossfadeSeconds;
            _fadeInSeconds = fadeInSeconds;
            _heard = new double[songs.Count];
            _current = songs.Count == 0 ? 0 : ((first % songs.Count) + songs.Count) % songs.Count;
        }

        public bool IsPlaying => _playing != null;

        public string CurrentSongId => _songs.Count == 0 ? null : _songs[_current].SoundId;

        public void Play()
        {
            if (_playing != null || _songs.Count == 0) return;
            _playing = _sounds.StartLoop(_songs[_current].SoundId, _volume, _fadeInSeconds);
        }

        // Steps aside (another moment takes over the music); the song's clock stops with it.
        public void Pause(float fadeSeconds)
        {
            _playing?.Stop(fadeSeconds);
            _playing = null;
        }

        public void Tick(float deltaSeconds)
        {
            if (_playing == null || _songs.Count == 0) return;

            _heard[_current] += deltaSeconds;
            if (_heard[_current] < _songs[_current].TurnSeconds - _crossfadeSeconds) return;

            _playing.Stop(_crossfadeSeconds);
            _heard[_current] = 0;
            _current = (_current + 1) % _songs.Count;
            _playing = _sounds.StartLoop(_songs[_current].SoundId, _volume, _crossfadeSeconds);
        }

        public readonly struct Song
        {
            public Song(string soundId, double turnSeconds)
            {
                SoundId = soundId;
                TurnSeconds = Math.Max(1, turnSeconds);
            }

            public string SoundId { get; }
            public double TurnSeconds { get; }
        }
    }
}
