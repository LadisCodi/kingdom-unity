using System.Collections.Generic;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Modules.Audio.Tests
{
    public class SoundServiceTests
    {
        private sealed class FakeSound : ISound
        {
            public FakeSound(string id) => Id = id;
            public string Id { get; }
            public SoundTrack Track => SoundTrack.Ui;
        }

        private sealed class FakeCatalog : Catalog<ISound>, ISoundCatalog
        {
            public FakeCatalog(params ISound[] sounds) : base(sounds) { }
        }

        private sealed class FakePlayer : ISoundPlayer
        {
            public readonly List<(ISound Sound, float Volume)> Played = new();
            public readonly Dictionary<SoundTrack, float> Volumes = new();

            public void Play(ISound sound, float volume) => Played.Add((sound, volume));

            public void SetVolume(SoundTrack track, float volume) => Volumes[track] = volume;

            public ISoundLoop StartLoop(ISound sound, float volume, float fadeSeconds) => null;
        }

        private readonly FakeSound _click = new("click");
        private FakePlayer _player;
        private SoundService _service;

        [SetUp]
        public void SetUp()
        {
            _player = new FakePlayer();
            _service = new SoundService(new FakeCatalog(_click), _player);
        }

        [Test]
        public void Play_ShouldPlayTheCataloguedSound()
        {
            Assert.That(_service.Play("click", 0.5f), Is.True);
            Assert.That(_player.Played, Is.EqualTo(new[] { ((ISound)_click, 0.5f) }));
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
