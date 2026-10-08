using System.Collections.Generic;
using Codigames.Modules.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Audio
{
    // Every sound of the game, by id: code plays a sound by id, the clip is chosen here.
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Kingdom/Audio/Sound Catalog")]
    public class SoundCatalog : ScriptableObject, ISoundCatalog
    {
        [TableList]
        [SerializeField] private List<UnitySound> _sounds = new();

        private Dictionary<string, UnitySound> _byId;

        public bool TryGet(string soundId, out ISound sound)
        {
            _byId ??= BuildLookup();

            if (_byId.TryGetValue(soundId, out var found) && found.Clip != null)
            {
                sound = found;
                return true;
            }

            sound = null;
            return false;
        }

        private Dictionary<string, UnitySound> BuildLookup()
        {
            var map = new Dictionary<string, UnitySound>();

            foreach (var entry in _sounds)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Id)) map[entry.Id] = entry;
            }

            return map;
        }

        private void OnValidate() => _byId = null;
    }
}
