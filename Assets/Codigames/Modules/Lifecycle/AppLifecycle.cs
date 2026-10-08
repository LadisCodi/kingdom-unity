using System;
using Object = UnityEngine.Object;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Modules.Lifecycle
{
    // Raised when the game may be stopped: whatever must survive (the save) is written on this signal.
    public class AppLifecycle : IStartable, IDisposable
    {
        private AppLifecycleHook _hook;

        public event Action Suspending;

        public void Start()
        {
            var go = new GameObject(nameof(AppLifecycleHook));
            Object.DontDestroyOnLoad(go);

            _hook = go.AddComponent<AppLifecycleHook>();
            _hook.Suspending += OnSuspending;
        }

        public void Dispose()
        {
            if (_hook == null) return;

            _hook.Suspending -= OnSuspending;
            Object.Destroy(_hook.gameObject);
            _hook = null;
        }

        private void OnSuspending() => Suspending?.Invoke();
    }
}
