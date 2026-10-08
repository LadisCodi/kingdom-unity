using System;

namespace Codigames.Modules.Core
{
    // Live instances looked up by id (the buildings standing, the armies out): a catalog whose content
    // changes, and says so.
    public interface IRegistry<T> : ICatalog<T> where T : IIdentifiable
    {
        event Action<T> Registered;

        event Action<T> Unregistered;

        // Throws when the id is taken: two live things with one id is a bug.
        void Register(T item);

        // False when there was nothing with that id.
        bool Unregister(string id);
    }
}
