using System.Collections.Generic;
using NUnit.Framework;

namespace Codigames.Modules.Audio.Tests
{
    public class PlaylistTests
    {
        private sealed class Loop : ISoundLoop
        {
            public Loop(string id) => Id = id;
            public string Id { get; }
            public float? StoppedOver { get; private set; }
            public void FadeTo(float volume, float seconds) { }
            public void Stop(float fadeSeconds) => StoppedOver = fadeSeconds;
        }

        private sealed class Sounds : ISoundService
        {
            public readonly List<(Loop Loop, float Fade)> Started = new();
            public bool Play(string soundId, float volume = 1) => true;
            public void SetVolume(SoundTrack track, float volume) { }

            public ISoundLoop StartLoop(string soundId, float volume, float fadeSeconds)
            {
                var loop = new Loop(soundId);
                Started.Add((loop, fadeSeconds));
                return loop;
            }
        }

        private static Playlist Make(Sounds sounds, int first = 0) => new(sounds,
            new[] { new Playlist.Song("harp", 60), new Playlist.Song("anthem", 120) }, 0.35f, 5f, 1.5f, first);

        [Test]
        public void Play_ShouldStartTheFirstSongFadingIn()
        {
            var sounds = new Sounds();
            Make(sounds, first: 1).Play();

            Assert.That(sounds.Started[0].Loop.Id, Is.EqualTo("anthem"));
            Assert.That(sounds.Started[0].Fade, Is.EqualTo(1.5f));
        }

        [Test]
        public void Tick_ShouldCrossfadeIntoTheNextSongBeforeTheTurnEnds()
        {
            var sounds = new Sounds();
            var playlist = Make(sounds);
            playlist.Play();

            playlist.Tick(54);
            Assert.That(sounds.Started, Has.Count.EqualTo(1));

            playlist.Tick(1);
            Assert.That(sounds.Started, Has.Count.EqualTo(2));
            Assert.That(sounds.Started[1].Loop.Id, Is.EqualTo("anthem"));
            Assert.That(sounds.Started[0].Loop.StoppedOver, Is.EqualTo(5f));
        }

        [Test]
        public void APausedSong_ShouldResumeItsTurnWhereItWas()
        {
            var sounds = new Sounds();
            var playlist = Make(sounds);
            playlist.Play();
            playlist.Tick(30);

            playlist.Pause(1);
            playlist.Tick(100);
            playlist.Play();
            playlist.Tick(24);

            Assert.That(playlist.CurrentSongId, Is.EqualTo("harp"));
            playlist.Tick(1);
            Assert.That(playlist.CurrentSongId, Is.EqualTo("anthem"));
        }
    }
}
