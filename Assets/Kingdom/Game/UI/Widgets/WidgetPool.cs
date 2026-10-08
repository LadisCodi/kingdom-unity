using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Kingdom.Game.UI.Widgets
{
    // Pools widgets under a container, reusing any authored there (so a prefab can keep a couple for
    // preview). Spawned instances are injected, so their [Inject] dependencies resolve.
    public class WidgetPool<TWidget> where TWidget : Widget
    {
        private readonly TWidget _prefab;
        private readonly Transform _container;
        private readonly IObjectResolver _resolver;
        private readonly List<TWidget> _active = new();
        private readonly ObjectPool<TWidget> _pool;

        public WidgetPool(TWidget prefab, Transform container, IObjectResolver resolver, int defaultCapacity = 4, int maxSize = 16)
        {
            _prefab = prefab;
            _container = container;
            _resolver = resolver;

            _pool = new ObjectPool<TWidget>(
                createFunc: Create,
                actionOnGet: widget => widget.gameObject.SetActive(true),
                actionOnRelease: widget => widget.gameObject.SetActive(false),
                actionOnDestroy: widget => Object.Destroy(widget.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);

            AdoptAuthored();
        }

        public IReadOnlyList<TWidget> Active => _active;

        public TWidget Spawn()
        {
            var widget = _pool.Get();
            widget.transform.SetAsLastSibling();

            _active.Add(widget);
            return widget;
        }

        public void DespawnAll()
        {
            foreach (var widget in _active)
            {
                _pool.Release(widget);
            }

            _active.Clear();
        }

        public void Clear()
        {
            DespawnAll();
            _pool.Clear();
        }

        private void AdoptAuthored()
        {
            if (_container == null) return;

            foreach (var widget in _container.GetComponentsInChildren<TWidget>(true))
            {
                _resolver?.InjectGameObject(widget.gameObject);
                _pool.Release(widget);
            }
        }

        private TWidget Create()
        {
            var widget = Object.Instantiate(_prefab, _container, worldPositionStays: false);
            _resolver?.InjectGameObject(widget.gameObject);
            widget.gameObject.SetActive(false);
            return widget;
        }
    }
}
