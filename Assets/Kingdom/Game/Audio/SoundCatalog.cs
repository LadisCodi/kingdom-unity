using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kingdom.Game.Audio
{
    // Maps each sound id to its clip and track. Code plays a sound by id; the clip is chosen here.
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Kingdom/Audio/Sound Catalog")]
    public class SoundCatalog : ScriptableObject
    {
        [TableList]
        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, Entry> _byId;

        public bool TryGet(string soundId, out AudioClip clip, out SoundTrack track)
        {
            _byId ??= BuildLookup();

            if (_byId.TryGetValue(soundId, out var entry) && entry.Clip != null)
            {
                clip = entry.Clip;
                track = entry.Track;
                return true;
            }

            clip = null;
            track = SoundTrack.Sfx;
            return false;
        }

        private Dictionary<string, Entry> BuildLookup()
        {
            var map = new Dictionary<string, Entry>();

            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.Id)) map[entry.Id] = entry;
            }

            return map;
        }

        private void OnValidate() => _byId = null;

        [Serializable]
        private class Entry
        {
            public string Id;
            public AudioClip Clip;
            public SoundTrack Track;
        }
    }
}
