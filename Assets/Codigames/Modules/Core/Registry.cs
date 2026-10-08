using System;
using System.Collections.Generic;

namespace Codigames.Modules.Core
{
    // A registry in registration order.
    public class Registry<T> : IRegistry<T> where T : IIdentifiable
    {
        private readonly List<T> _items = new();
        private readonly Dictionary<string, T> _byId = new();

        public event Action<T> Registered;
        public event Action<T> Unregistered;

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

        public virtual void Register(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(item.Id)) throw new ArgumentException($"A {typeof(T).Name} has no id.");
            if (_byId.ContainsKey(item.Id)) throw new InvalidOperationException($"A {typeof(T).Name} with the id \"{item.Id}\" is already registered.");

            _byId.Add(item.Id, item);
            _items.Add(item);
            Registered?.Invoke(item);
        }

        public virtual bool Unregister(string id)
        {
            if (!TryGet(id, out var item)) return false;

            _byId.Remove(id);
            _items.Remove(item);
            Unregistered?.Invoke(item);
            return true;
        }
    }
}
