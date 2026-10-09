using System;
using System.Collections.Generic;
using Codigames.Modules.Saves;
using NUnit.Framework;

namespace Codigames.Modules.Saves.Tests
{
    public class SaveSlotTests
    {
        private const string SLOT = "kingdom";

        // A save as "version|name"; the raw document is the two parts.
        private sealed class Codec : ISaveCodec<string, string[]>
        {
            public string Encode(string state, int version) => version + "|" + state;

            public string[] Parse(string text)
            {
                var parts = text.Split('|');
                if (parts.Length != 2) throw new FormatException("not a save");
                return parts;
            }

            public int VersionOf(string[] raw) => int.Parse(raw[0]);
            public string Decode(string[] raw) => raw[1];
        }

        // Version 1 called the town "town"; version 2 calls it "city".
        private sealed class RenameTown : ISaveMigration<string[]>
        {
            public int FromVersion => 1;
            public string[] Apply(string[] raw) => new[] { "2", raw[1].Replace("town", "city") };
        }

        private sealed class Storage : ISaveStorage
        {
            public readonly Dictionary<string, string> Slots = new();
            public string Read(string slot) => Slots.TryGetValue(slot, out var text) ? text : null;
            public void Write(string slot, string text) => Slots[slot] = text;
            public void Keep(string slot, string asSlot) => Slots[asSlot] = Slots[slot];
            public void Delete(string slot) => Slots.Remove(slot);
        }

        private Storage _storage;

        [SetUp]
        public void SetUp() => _storage = new Storage();

        private SaveSlot<string, string[]> Slot(int version = 2)
            => new(_storage, new Codec(), SLOT, version, new ISaveMigration<string[]>[] { new RenameTown() });

        [Test]
        public void Load_ShouldFindNothingOnAFreshDevice()
        {
            Assert.That(Slot().Load().Status, Is.EqualTo(SaveLoadStatus.Missing));
        }

        [Test]
        public void Save_ShouldReadBackWhatItWrote()
        {
            Slot().Save("my city");

            var load = Slot().Load();

            Assert.That(load.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(load.State, Is.EqualTo("my city"));
        }

        [Test]
        public void Load_ShouldMigrateAnOlderSave()
        {
            _storage.Slots[SLOT] = "1|my town";

            var load = Slot().Load();

            Assert.That(load.State, Is.EqualTo("my city"));
            Assert.That(load.Version, Is.EqualTo(1));
        }

        [Test]
        public void Load_ShouldRefuseANewerSaveAndNeverWriteOverIt()
        {
            _storage.Slots[SLOT] = "3|from the future";
            var slot = Slot();

            Assert.That(slot.Load().Status, Is.EqualTo(SaveLoadStatus.TooNew));
            Assert.That(slot.Save("today"), Is.False);
            Assert.That(_storage.Slots[SLOT], Is.EqualTo("3|from the future"));
        }

        [Test]
        public void Load_ShouldSetAsideWhatItCannotRead()
        {
            _storage.Slots[SLOT] = "garbage";

            Assert.That(Slot().Load().Status, Is.EqualTo(SaveLoadStatus.Unreadable));
            Assert.That(_storage.Slots.ContainsKey(SLOT), Is.False);
            Assert.That(_storage.Slots[SLOT + ".unreadable"], Is.EqualTo("garbage"));
        }

        [Test]
        public void Constructor_ShouldRefuseAGapInTheMigrations()
        {
            Assert.Throws<ArgumentException>(() => Slot(version: 3));
        }
    }
}
