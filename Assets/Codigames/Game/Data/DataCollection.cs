using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.Data
{
    // What the data window and the importer see of any collection, whatever its entry type.
    public abstract class DataCollection : ScriptableObject
    {
        // The menu path the data window shows it under ("Buildings").
        public abstract string Title { get; }

        public abstract Type EntryType { get; }

        public abstract IEnumerable<DefinitionAsset> Entries { get; }

        public abstract void Add(DefinitionAsset entry);

        public abstract void Remove(DefinitionAsset entry);
    }
}
