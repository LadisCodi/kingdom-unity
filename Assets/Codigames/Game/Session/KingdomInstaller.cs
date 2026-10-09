using Codigames.Game.Saves;
using Codigames.Kingdom;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Map;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Saves;
using Codigames.Modules.Timeline;
using Newtonsoft.Json.Linq;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Session
{
    // Kingdom's rules and state, wired for this scene: one state (the saved one, else a new one), the services
    // that change it, the timeline every timed system is on, and the saver.
    public static class KingdomInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<ISaveStorage, FileSaveStorage>(Lifetime.Singleton);
            builder.Register(resolver => KingdomSaves.Slot(resolver.Resolve<ISaveStorage>()), Lifetime.Singleton);
            builder.Register(resolver => KingdomLoader.Load(resolver.Resolve<SaveSlot<KingdomState, JObject>>(),
                resolver.Resolve<IProvinceMap>(), resolver.Resolve<IConstructionSettings>(),
                resolver.Resolve<ICatalog<ICurrencyDefinition>>(), resolver.Resolve<IClock>()), Lifetime.Singleton);

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
            builder.RegisterEntryPoint<KingdomSaver>();
        }
    }
}
