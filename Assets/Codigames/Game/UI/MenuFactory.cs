using System;
using System.Collections.Generic;
using Codigames.Modules.UI;
using Object = UnityEngine.Object;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.UI
{
    // Creates and caches menu views: a menu already placed under the UI root is adopted, otherwise its
    // prefab comes from the catalog. The only place that knows how a Menu comes into existence.
    public class MenuFactory : IMenuViewFactory, IDisposable
    {
        private readonly Dictionary<Type, Menu> _menusByType = new();
        private readonly IObjectResolver _resolver;
        private readonly MenuCatalog _catalog;
        private readonly UIRoot _root;

        public MenuFactory(IObjectResolver resolver, MenuCatalog catalog, UIRoot root)
        {
            _resolver = resolver;
            _catalog = catalog;
            _root = root;
        }

        public RectTransform Container => _root.Container;

        public TView Resolve<TView>() where TView : class, IMenuView => Resolve(typeof(TView)) as TView;

        public Menu Resolve(Type type)
        {
            if (_menusByType.TryGetValue(type, out var cached)) return cached;

            return FindPlaced(type) ?? Instantiate(type);
        }

        public void Dispose()
        {
            foreach (var menu in _menusByType.Values)
            {
                if (menu != null) menu.Dispose();
            }

            _menusByType.Clear();
        }

        private Menu FindPlaced(Type type)
        {
            var menu = _root.GetComponentInChildren(type, true) as Menu;
            return menu != null ? Adopt(type, menu) : null;
        }

        private Menu Instantiate(Type type)
        {
            var prefab = _catalog.GetPrefab(type);

            if (prefab == null)
            {
                Debug.LogError($"#UI# No prefab for {type.Name} in the MenuCatalog.");
                return null;
            }

            return Adopt(type, Object.Instantiate(prefab, _root.Container));
        }

        private Menu Adopt(Type type, Menu menu)
        {
            _resolver.InjectGameObject(menu.gameObject);
            menu.Initialize();
            _menusByType[type] = menu;
            return menu;
        }
    }
}
