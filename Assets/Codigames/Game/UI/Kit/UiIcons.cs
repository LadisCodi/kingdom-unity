using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // The UI's icons by id — the file's name in Art/UI/Icons (bed, hourglass, Townhall…), as the web's atlas names
    // them. Filled by Kingdom › UI › Rebuild icon catalog; a screen asks for an id, never for a file.
    [CreateAssetMenu(fileName = "UiIcons", menuName = "Kingdom/UI/UI Icons")]
    public class UiIcons : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private string _id;
            [SerializeField, PreviewField(32)] private Sprite _sprite;

            public Entry(string id, Sprite sprite)
            {
                _id = id;
                _sprite = sprite;
            }

            public string Id => _id;
            public Sprite Sprite => _sprite;
        }

        [SerializeField, ListDrawerSettings(ListElementLabelName = "_id")] private List<Entry> _icons = new();

        private Dictionary<string, Sprite> _lookup;

        public Sprite Get(string id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Sprite>();
                foreach (var entry in _icons) _lookup[entry.Id] = entry.Sprite;
            }

            return id != null && _lookup.TryGetValue(id, out var sprite) ? sprite : null;
        }

        public void Set(IEnumerable<Entry> icons)
        {
            _icons = new List<Entry>(icons);
            _lookup = null;
        }
    }
}
