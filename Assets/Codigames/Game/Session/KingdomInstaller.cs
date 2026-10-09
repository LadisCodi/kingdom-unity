using Codigames.Game.Saves;
using Codigames.Kingdom;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Research;
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
            builder.Register(resolver => resolver.Resolve<KingdomState>().Harvest, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Mana, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Fog, Lifetime.Singleton);
            builder.Register<FogOfWar>(Lifetime.Singleton).AsSelf().As<IRevealedGround>().As<IExploredGround>();
            builder.Register(resolver => resolver.Resolve<KingdomState>().Research, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Knowledge, Lifetime.Singleton);
            builder.Register<KnowledgeBar>(Lifetime.Singleton);
            builder.Register<KnowledgeMarket>(Lifetime.Singleton);
            builder.Register<Bookshelf>(Lifetime.Singleton).As<IBookshelf>();
            builder.Register<Researching>(Lifetime.Singleton);
            builder.Register<TechBonuses>(Lifetime.Singleton).As<IBonuses>();
            builder.Register<ResearchGates>(Lifetime.Singleton).As<IResearchGates>();
            builder.Register<BuildingsLiftFog>(Lifetime.Singleton);
            builder.Register<ITreasury>(resolver => new Treasury(
                resolver.Resolve<ICatalog<ICurrencyDefinition>>(), resolver.Resolve<KingdomState>().Balances), Lifetime.Singleton);

            builder.Register<Placement>(Lifetime.Singleton);
            builder.Register<Construction>(Lifetime.Singleton);
            builder.Register<ManaPool>(Lifetime.Singleton);
            builder.Register<Stores>(Lifetime.Singleton);
            builder.Register<VillagerTraining>(Lifetime.Singleton);
            builder.Register<Workforce>(Lifetime.Singleton);
            builder.Register(resolver => new Harvesting(resolver.Resolve<HarvestState>(), resolver.Resolve<GroundState>(),
                resolver.Resolve<CityState>(), resolver.Resolve<IProvinceMap>(), resolver.Resolve<ICatalog<IBuildingDefinition>>(),
                resolver.Resolve<ICatalog<IFeatureDefinition>>(), resolver.Resolve<ICatalog<IHarvestSource>>(),
                resolver.Resolve<ITerrainYields>(), resolver.Resolve<ITapSettings>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<ManaPool>(), resolver.Resolve<KingdomState>().Seed, resolver.Resolve<IRevealedGround>(),
                resolver.Resolve<IResearchGates>(), resolver.Resolve<IBonuses>()), Lifetime.Singleton);
            builder.Register<ResearchEffects>(Lifetime.Singleton);

            builder.Register(resolver =>
            {
                var state = resolver.Resolve<KingdomState>();
                var timeline = new Timeline(state.LastAdvance);
                timeline.Register(resolver.Resolve<Construction>());
                resolver.Resolve<BuildingsLiftFog>();
                timeline.Register(resolver.Resolve<VillagerTraining>());

                // A pool left below its cap without a clock (an older save) starts gaining where the kingdom left off.
                var mana = resolver.Resolve<ManaPool>();
                mana.Wake(state.LastAdvance);
                timeline.Register(mana);

                // The bar likewise: a new kingdom starts with none, and drips from its first second.
                var knowledge = resolver.Resolve<KnowledgeBar>();
                knowledge.Wake(state.LastAdvance);
                timeline.Register(knowledge);
                timeline.Register(resolver.Resolve<Harvesting>());

                // Stores registered after construction, so a level finishing is counted before a store fills.
                var stores = resolver.Resolve<Stores>();
                stores.WakeAll(state.LastAdvance);
                timeline.Register(stores);

                // The crews last: their steps run between the boundaries the others draw.
                timeline.Register(resolver.Resolve<Workforce>());

                // What a technology completing does to the rest of the kingdom.
                resolver.Resolve<ResearchEffects>();
                return timeline;
            }, Lifetime.Singleton);

            builder.RegisterEntryPoint<KingdomTicker>();
            builder.RegisterEntryPoint<KingdomSaver>();
        }
    }
}
