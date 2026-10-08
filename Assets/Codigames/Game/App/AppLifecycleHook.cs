using Codigames.Modules.Lifecycle;
using UnityEngine;
using VContainer;

namespace Codigames.Game.App
{
    // Reports Unity's application callbacks to the lifecycle module. Lives on the root scope's prefab, so
    // it lasts as long as the app.
    public class AppLifecycleHook : MonoBehaviour
    {
        private IAppLifecycleNotifier _lifecycle;

        [Inject]
        public void Construct(IAppLifecycleNotifier lifecycle)
        {
            _lifecycle = lifecycle;
        }

        private void OnApplicationPause(bool paused) => _lifecycle?.Paused(paused);

        private void OnApplicationFocus(bool hasFocus) => _lifecycle?.FocusChanged(hasFocus);

        private void OnApplicationQuit() => _lifecycle?.Quitting();
    }
}
