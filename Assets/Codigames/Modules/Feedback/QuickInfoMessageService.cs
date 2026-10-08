using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Pool;

namespace Codigames.Modules.Feedback
{
    public class QuickInfoMessageService : IQuickInfoMessageService
    {
        private readonly QuickInfoMessageSettings _settings;
        private readonly IQuickInfoMessageLayer _layer;
        private ObjectPool<QuickInfoMessageView> _pool;
        private RectTransform _root;

        public QuickInfoMessageService(QuickInfoMessageSettings settings, IQuickInfoMessageLayer layer)
        {
            _settings = settings;
            _layer = layer;
        }

        public void Show(QuickInfoMessageData data)
        {
            var view = Pool.Get();
            view.Configure(data);
            view.Play().Forget();
        }

        private ObjectPool<QuickInfoMessageView> Pool => _pool ??= new ObjectPool<QuickInfoMessageView>(
            createFunc: Create,
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => Object.Destroy(view.gameObject),
            collectionCheck: true,
            defaultCapacity: 4,
            maxSize: 32);

        private QuickInfoMessageView Create()
        {
            var view = Object.Instantiate(_settings.Prefab, Root, worldPositionStays: false);
            view.gameObject.SetActive(false);
            view.Finished += Release;
            return view;
        }

        private void Release(QuickInfoMessageView view) => Pool.Release(view);

        // A full-screen layer above the menus, so every message stays grouped on top.
        private RectTransform Root
        {
            get
            {
                if (_root != null) return _root;

                var go = new GameObject(nameof(QuickInfoMessageService), typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_layer.Container, worldPositionStays: false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsLastSibling();

                _root = rect;
                return _root;
            }
        }
    }
}
