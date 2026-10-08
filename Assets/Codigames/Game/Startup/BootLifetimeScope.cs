using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    public class BootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootFlow>();
        }
    }
}
