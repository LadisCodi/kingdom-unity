using System;

namespace Codigames.Modules.Lifecycle
{
    // Turns the platform's pause, focus and quit reports into one signal: the app may be stopped now
    // (backgrounded on mobile, focus lost on desktop, quitting).
    public class AppLifecycle : IAppLifecycle, IAppLifecycleNotifier
    {
        public event Action Suspending;

        public void Paused(bool paused)
        {
            if (paused) Suspending?.Invoke();
        }

        public void FocusChanged(bool hasFocus)
        {
            if (!hasFocus) Suspending?.Invoke();
        }

        public void Quitting() => Suspending?.Invoke();
    }
}
