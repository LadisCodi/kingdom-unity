using Codigames.Kingdom;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Map;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Session
{
    // Kingdom's rules and state, wired for this scene: one state, the services that change it, and the
    // timeline every timed system is on.
    public static class KingdomInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register(resolver => NewKingdom.Create(
                resolver.Resolve<IProvinceMap>(), resolver.Resolve<IConstructionSettings>(),
                resolver.Resolve<ICatalog<ICurrencyDefinition>>(), resolver.Resolve<IClock>().NowMs), Lifetime.Singleton);

            builder.Register(resolver => resolver.Resolve<KingdomState>().City, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Ground, Lifetime.Singleton);
            builder.Register<ITreasury>(resolver => new Treasury(
                resolver.Resolve<ICatalog<ICurrencyDefinition>>(), resolver.Resolve<KingdomState>().Balances), Lifetime.Singleton);

            builder.Register<Placement>(Lifetime.Singleton);
            builder.Register<Construction>(Lifetime.Singleton);

            builder.Register(resolver =>
            {
                var timeline = new Timeline(resolver.Resolve<KingdomState>().LastAdvance);
                timeline.Register(resolver.Resolve<Construction>());
                return timeline;
            }, Lifetime.Singleton);

            builder.RegisterEntryPoint<KingdomTicker>();
        }
    }
}
