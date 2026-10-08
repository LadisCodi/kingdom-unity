namespace Codigames.Modules.Lifecycle
{
    // What the platform reports, called from outside the module (an engine hook, a test).
    public interface IAppLifecycleNotifier
    {
        void Paused(bool paused);

        void FocusChanged(bool hasFocus);

        void Quitting();
    }
}
