using System;
using System.Collections.Generic;
using Codigames.Modules.Core;
using UnityEngine;

namespace Codigames.Game.Data
{
    // A collection of the balance as an asset: its entries in authored order, served to Kingdom as a catalog
    // of their definition interface. Every collection derives from this once and implements nothing else.
    public abstract class DefinitionCollection<TDefinition, TAsset> : DataCollection, ICatalog<TDefinition>
        where TDefinition : IIdentifiable
        where TAsset : DefinitionAsset, TDefinition
    {
        [SerializeField] private List<TAsset> _entries = new();

        private Catalog<TDefinition> _catalog;

        public override Type EntryType => typeof(TAsset);

        public override IEnumerable<DefinitionAsset> Entries => _entries;

        public IReadOnlyList<TDefinition> Items => Catalog.Items;

        private Catalog<TDefinition> Catalog => _catalog ??= BuildCatalog();

        public bool Contains(string id) => Catalog.Contains(id);

        public bool TryGet(string id, out TDefinition item) => Catalog.TryGet(id, out item);

        public TDefinition Get(string id) => Catalog.Get(id);

        public TItem Get<TItem>(string id) where TItem : TDefinition => Catalog.Get<TItem>(id);

        public override void Add(DefinitionAsset entry)
        {
            if (entry is not TAsset asset || _entries.Contains(asset)) return;

            _entries.Add(asset);
            _catalog = null;
        }

        public override void Remove(DefinitionAsset entry)
        {
            if (entry is TAsset asset && _entries.Remove(asset)) _catalog = null;
        }

        private Catalog<TDefinition> BuildCatalog()
        {
            var items = new List<TDefinition>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (entry != null) items.Add(entry);
            }

            return new Catalog<TDefinition>(items);
        }

        // An edit in the inspector rebuilds the lookup.
        private void OnValidate() => _catalog = null;
    }
}
