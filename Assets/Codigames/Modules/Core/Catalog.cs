using System;
using System.Collections.Generic;

namespace Codigames.Modules.Core
{
    // A catalog over a fixed set of items, built once from wherever they come from (assets, a file, a test).
    public class Catalog<T> : ICatalog<T> where T : IIdentifiable
    {
        private readonly List<T> _items = new();
        private readonly Dictionary<string, T> _byId = new();

        public Catalog(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.Id)) throw new ArgumentException($"A {typeof(T).Name} has no id.");
                if (_byId.ContainsKey(item.Id)) throw new ArgumentException($"Two {typeof(T).Name}s share the id \"{item.Id}\".");

                _byId.Add(item.Id, item);
                _items.Add(item);
            }
        }

        public IReadOnlyList<T> Items => _items;

        public bool Contains(string id) => id != null && _byId.ContainsKey(id);

        public bool TryGet(string id, out T item)
        {
            if (id != null) return _byId.TryGetValue(id, out item);

            item = default;
            return false;
        }

        public T Get(string id)
            => TryGet(id, out var item) ? item : throw new KeyNotFoundException($"No {typeof(T).Name} with the id \"{id}\".");

        public TItem Get<TItem>(string id) where TItem : T => (TItem)Get(id);
    }
}
