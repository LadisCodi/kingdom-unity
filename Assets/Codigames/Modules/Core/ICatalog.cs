using System.Collections.Generic;

namespace Codigames.Modules.Core
{
    // Things looked up by id — definitions authored once (buildings, sounds, technologies), in their
    // authored order. Read-only: a catalog never changes what it holds.
    public interface ICatalog<T> where T : IIdentifiable
    {
        IReadOnlyList<T> Items { get; }

        bool Contains(string id);

        bool TryGet(string id, out T item);

        // Throws when there is none: asking for an id that does not exist is a bug, not a state.
        T Get(string id);

        TItem Get<TItem>(string id) where TItem : T;
    }
}
