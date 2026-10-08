using System.Collections.Generic;
using Codigames.Modules.Audio;
using Codigames.Modules.Core;
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

        private Catalog<ISound> _catalog;

        private Catalog<ISound> Sounds => _catalog ??= new Catalog<ISound>(_sounds);

        public IReadOnlyList<ISound> Items => Sounds.Items;

        public bool Contains(string id) => Sounds.Contains(id);

        public bool TryGet(string id, out ISound item) => Sounds.TryGet(id, out item);

        public ISound Get(string id) => Sounds.Get(id);

        public TItem Get<TItem>(string id) where TItem : ISound => Sounds.Get<TItem>(id);

        // An edit in the inspector rebuilds the lookup.
        private void OnValidate() => _catalog = null;
    }
}
