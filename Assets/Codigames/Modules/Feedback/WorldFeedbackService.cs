using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Pool;

namespace Codigames.Modules.Feedback
{
    public class WorldFeedbackService : IWorldFeedbackService
    {
        private readonly WorldFeedbackCatalog _catalog;
        private readonly Dictionary<Type, ObjectPool<WorldFeedbackView>> _pools = new();
        private Transform _root;

        public WorldFeedbackService(WorldFeedbackCatalog catalog)
        {
            _catalog = catalog;
        }

        private Transform Root => _root != null ? _root : _root = new GameObject(nameof(WorldFeedbackService)).transform;

        public T Spawn<T>(Vector3 worldPosition) where T : WorldFeedbackView
        {
            var view = GetPool(typeof(T)).Get();
            view.transform.position = worldPosition;
            return (T)view;
        }

        private ObjectPool<WorldFeedbackView> GetPool(Type type)
        {
            if (_pools.TryGetValue(type, out var pool)) return pool;

            var prefab = _catalog.GetPrefab(type);

            pool = new ObjectPool<WorldFeedbackView>(
                createFunc: () => Create(prefab),
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => view.gameObject.SetActive(false),
                actionOnDestroy: view => Object.Destroy(view.gameObject),
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 64);

            _pools[type] = pool;
            return pool;
        }

        private WorldFeedbackView Create(WorldFeedbackView prefab)
        {
            var view = Object.Instantiate(prefab, Root);
            view.gameObject.SetActive(false);
            view.Finished += Release;
            return view;
        }

        private void Release(WorldFeedbackView view)
        {
            if (_pools.TryGetValue(view.GetType(), out var pool)) pool.Release(view);
        }
    }
}
