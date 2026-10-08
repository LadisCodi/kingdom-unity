using System.Collections.Generic;
using NUnit.Framework;

namespace Codigames.Modules.Audio.Tests
{
    public class SoundServiceTests
    {
        private sealed class FakeSound : ISound
        {
            public SoundTrack Track => SoundTrack.Ui;
        }

        private sealed class FakeCatalog : ISoundCatalog
        {
            public readonly Dictionary<string, ISound> Sounds = new();

            public bool TryGet(string soundId, out ISound sound) => Sounds.TryGetValue(soundId, out sound);
        }

        private sealed class FakePlayer : ISoundPlayer
        {
            public readonly List<(ISound Sound, float Volume)> Played = new();
            public readonly Dictionary<SoundTrack, float> Volumes = new();

            public void Play(ISound sound, float volume) => Played.Add((sound, volume));

            public void SetVolume(SoundTrack track, float volume) => Volumes[track] = volume;
        }

        private FakeCatalog _catalog;
        private FakePlayer _player;
        private SoundService _service;

        [SetUp]
        public void SetUp()
        {
            _catalog = new FakeCatalog();
            _player = new FakePlayer();
            _service = new SoundService(_catalog, _player);
        }

        [Test]
        public void Play_ShouldPlayTheCataloguedSound()
        {
            var click = new FakeSound();
            _catalog.Sounds["click"] = click;

            Assert.That(_service.Play("click", 0.5f), Is.True);
            Assert.That(_player.Played, Is.EqualTo(new[] { ((ISound)click, 0.5f) }));
        }

        [Test]
        public void Play_ShouldRefuseAnUnknownId()
        {
            Assert.That(_service.Play("nothing"), Is.False);
            Assert.That(_service.Play(null), Is.False);
            Assert.That(_player.Played, Is.Empty);
        }

        [Test]
        public void SetVolume_ShouldReachThePlayer()
        {
            _service.SetVolume(SoundTrack.Music, 0.3f);

            Assert.That(_player.Volumes[SoundTrack.Music], Is.EqualTo(0.3f));
        }
    }
}
