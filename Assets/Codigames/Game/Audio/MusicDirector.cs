using System.Linq;
using Codigames.Modules.Audio;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Audio
{
    // The town's music: a playlist from a random song, each in turn, crossfading. The moments that take over the
    // music (a fight, a chest) arrive with their screens.
    public class MusicDirector : IStartable, ITickable
    {
        private readonly Playlist _town;

        public MusicDirector(ISoundService sounds, MusicSettings settings)
        {
            var songs = settings.Town.Select(s => new Playlist.Song(s.SoundId, s.TurnSeconds)).ToList();
            _town = new Playlist(sounds, songs, settings.TownVolume, settings.Crossfade, settings.TownFadeIn,
                Random.Range(0, Mathf.Max(1, songs.Count)));
        }

        public void Start() => _town.Play();

        public void Tick() => _town.Tick(Time.unscaledDeltaTime);
    }
}
