using System;
using UnityEngine;

namespace Codigames.Modules.Lifecycle
{
    // Turns Unity's application callbacks into one signal: the game may stop now (backgrounded on mobile,
    // focus lost on desktop, quitting).
    public class AppLifecycleHook : MonoBehaviour
    {
        public event Action Suspending;

        private void OnApplicationPause(bool paused)
        {
            if (paused) Suspending?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Suspending?.Invoke();
        }

        private void OnApplicationQuit() => Suspending?.Invoke();
    }
}
