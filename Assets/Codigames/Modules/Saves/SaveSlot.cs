using System;
using System.Collections.Generic;
using System.Linq;

namespace Codigames.Modules.Saves
{
    // One save: written whole at the current version, read back through every migration from the version it was
    // written at. Migrations are ordered, gapless and append-only: one per version that renamed, reshaped or
    // changed the meaning of something. A save from a newer build is refused, and this slot then refuses to be
    // written, so the newer save survives.
    public class SaveSlot<TState, TRaw>
    {
        private const string UNREADABLE_SUFFIX = ".unreadable";

        private readonly ISaveStorage _storage;
        private readonly ISaveCodec<TState, TRaw> _codec;
        private readonly string _slot;
        private readonly int _version;
        private readonly Dictionary<int, ISaveMigration<TRaw>> _migrations;

        private bool _refusesWrites;

        public SaveSlot(ISaveStorage storage, ISaveCodec<TState, TRaw> codec, string slot, int version,
            IEnumerable<ISaveMigration<TRaw>> migrations)
        {
            _storage = storage;
            _codec = codec;
            _slot = slot;
            _version = version;
            _migrations = migrations.ToDictionary(m => m.FromVersion);

            for (var from = 1; from < version; from++)
            {
                if (!_migrations.ContainsKey(from)) throw new ArgumentException($"No migration from version {from} to {from + 1}.");
            }
        }

        public SaveLoad<TState> Load()
        {
            var text = _storage.Read(_slot);
            if (text == null) return new SaveLoad<TState>(SaveLoadStatus.Missing, default, 0);

            TRaw raw;
            int version;
            try
            {
                raw = _codec.Parse(text);
                version = _codec.VersionOf(raw);
            }
            catch (Exception)
            {
                return SetAside();
            }

            if (version > _version)
            {
                _refusesWrites = true;
                return new SaveLoad<TState>(SaveLoadStatus.TooNew, default, version);
            }

            try
            {
                for (var from = version; from < _version; from++) raw = _migrations[from].Apply(raw);
                return new SaveLoad<TState>(SaveLoadStatus.Loaded, _codec.Decode(raw), version);
            }
            catch (Exception)
            {
                return SetAside();
            }
        }

        // False when this slot holds a newer build's save, which is never written over.
        public bool Save(TState state)
        {
            if (_refusesWrites) return false;

            _storage.Write(_slot, _codec.Encode(state, _version));
            return true;
        }

        public void Delete()
        {
            _storage.Delete(_slot);
            _refusesWrites = false;
        }

        private SaveLoad<TState> SetAside()
        {
            _storage.Keep(_slot, _slot + UNREADABLE_SUFFIX);
            _storage.Delete(_slot);
            return new SaveLoad<TState>(SaveLoadStatus.Unreadable, default, 0);
        }
    }
}
