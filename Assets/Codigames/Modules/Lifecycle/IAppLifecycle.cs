using System;

namespace Codigames.Modules.Lifecycle
{
    // What listeners see: the app may be stopped now, so whatever must survive is written.
    public interface IAppLifecycle
    {
        event Action Suspending;
    }
}
