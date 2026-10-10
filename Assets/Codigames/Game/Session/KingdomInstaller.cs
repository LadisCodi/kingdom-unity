using Codigames.Game.Saves;
using Codigames.Kingdom;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Bag.State;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Harvest.State;
using Codigames.Kingdom.Lairs;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Map;
using Codigames.Kingdom.Notices;
using Codigames.Kingdom.Notices.State;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Kingdom.Tutorial;
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
            builder.Register(resolver => resolver.Resolve<KingdomState>().Sites, Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Bag, Lifetime.Singleton);
            builder.Register<Boosts>(Lifetime.Singleton).AsSelf().As<IBoosts>();
            builder.Register<BoostEffects>(Lifetime.Singleton);
            builder.Register<Codigames.Kingdom.Bag.Bag>(Lifetime.Singleton).AsSelf().As<IItemGrants>().As<IItemHoldings>();
            builder.Register<Speedups>(Lifetime.Singleton);
            builder.Register<SiteGround>(Lifetime.Singleton).AsSelf().As<ISiteGround>();
            builder.Register(resolver => resolver.Resolve<KingdomState>().Lairs, Lifetime.Singleton);
            builder.Register<LairGround>(Lifetime.Singleton).AsSelf().As<ILairGround>();
            builder.Register(resolver => resolver.Resolve<KingdomState>().Heroes, Lifetime.Singleton);
            builder.Register<Kingdom.Heroes.HeroLadder>(Lifetime.Singleton);
            builder.Register(resolver => new Kingdom.Heroes.Heroes(resolver.Resolve<KingdomState>().Heroes,
                resolver.Resolve<ICatalog<Kingdom.Heroes.IHeroDefinition>>(), resolver.Resolve<Kingdom.Heroes.HeroLadder>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<Kingdom.Goods.Stockpile>(), resolver.Resolve<CityState>(), resolver.Resolve<ICatalog<IBuildingDefinition>>(),
                resolver.Resolve<IBonuses>(), () => resolver.Resolve<Kingdom.Modifiers.IModifiers>()), Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Gacha, Lifetime.Singleton);
            builder.Register<Kingdom.Heroes.Reveals>(Lifetime.Singleton);
            builder.Register(resolver => new Kingdom.Heroes.Gacha(resolver.Resolve<KingdomState>().Gacha, resolver.Resolve<Kingdom.Heroes.Heroes>(),
                resolver.Resolve<ICatalog<Kingdom.Heroes.IBannerDefinition>>(), resolver.Resolve<IItemHoldings>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<KingdomState>().Seed, resolver.Resolve<IBonuses>()), Lifetime.Singleton);
            // The kingdom's modifier stack: the Legendaries' boons now; relics and events join it.
            builder.Register<Kingdom.Modifiers.IModifiers>(resolver => new Kingdom.Modifiers.ModifierStack(
                new Kingdom.Modifiers.IModifierSource[] { resolver.Resolve<Kingdom.Heroes.Heroes>() },
                () => resolver.Resolve<Timeline>().LastAdvance), Lifetime.Singleton);
            // The relics: what is held and restored, and the Shrines that host and wake them.
            builder.Register(resolver => resolver.Resolve<KingdomState>().Relics, Lifetime.Singleton);
            builder.Register(resolver => new Kingdom.Relics.Relics(resolver.Resolve<KingdomState>().Relics,
                resolver.Resolve<ICatalog<Kingdom.Relics.IRelicDefinition>>(), resolver.Resolve<Kingdom.Relics.IRelicSettings>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<KingdomState>().Seed, resolver.Resolve<Researching>()), Lifetime.Singleton).AsSelf().As<Kingdom.Relics.IRelicDrops>();
            builder.Register(resolver => new Kingdom.Relics.Shrines(resolver.Resolve<KingdomState>().Relics, resolver.Resolve<Kingdom.Relics.Relics>(),
                resolver.Resolve<CityState>(), resolver.Resolve<ICatalog<IBuildingDefinition>>(), resolver.Resolve<ManaPool>(),
                () => resolver.Resolve<Kingdom.Modifiers.IModifiers>()), Lifetime.Singleton).AsSelf().As<Kingdom.Relics.IRelicAura>();
            builder.Register<Kingdom.Relics.ShrineLadder>(Lifetime.Singleton).As<IBuildLadder>();
            builder.Register<Relics.AsleepRelics>(Lifetime.Singleton);
            builder.Register<Kingdom.Battles.Combat>(Lifetime.Singleton);
            builder.Register<Kingdom.Battles.EnemyGenerator>(Lifetime.Singleton);
            builder.Register(resolver => new LairAttack(resolver.Resolve<Codigames.Kingdom.Lairs.Lairs>(), resolver.Resolve<Kingdom.Battles.Combat>(),
                resolver.Resolve<Kingdom.Battles.EnemyGenerator>(), resolver.Resolve<Kingdom.Army.Army>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<KingdomState>().Seed, resolver.Resolve<Data.Army.CombatSettingsAsset>().TroopSlots, resolver.Resolve<IItemGrants>(),
                resolver.Resolve<IBonuses>(), resolver.Resolve<Kingdom.Heroes.Heroes>(), resolver.Resolve<Kingdom.Modifiers.IModifiers>(),
                resolver.Resolve<Kingdom.Relics.IRelicDrops>()), Lifetime.Singleton);
            builder.Register(resolver => new Codigames.Kingdom.Lairs.Lairs(resolver.Resolve<KingdomState>().Lairs, resolver.Resolve<LairGround>(),
                resolver.Resolve<ILairSettings>(), resolver.Resolve<CityState>(), resolver.Resolve<Stores>(), resolver.Resolve<Workforce>(),
                resolver.Resolve<KingdomState>().Seed, resolver.Resolve<IBonuses>(), resolver.Resolve<Kingdom.Modifiers.IModifiers>()), Lifetime.Singleton);
            // A ruin missing a part takes it from the Bag.
            builder.Register(resolver => new Ruins(resolver.Resolve<SitesState>(), resolver.Resolve<SiteGround>(),
                resolver.Resolve<ICatalog<IBuildingDefinition>>(), resolver.Resolve<Construction>(), resolver.Resolve<IRevealedGround>(),
                resolver.Resolve<Codigames.Kingdom.Bag.Bag>(), resolver.Resolve<ILairGround>()),
                Lifetime.Singleton);
            builder.Register(resolver => new Footprints(resolver.Resolve<IProvinceMap>(), resolver.Resolve<ICatalog<IFeatureDefinition>>(),
                resolver.Resolve<SiteGround>().Blocks()), Lifetime.Singleton);
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
            builder.Register(resolver => resolver.Resolve<KingdomState>().Notices, Lifetime.Singleton);
            builder.Register<Inbox>(Lifetime.Singleton);
            builder.Register<SiteFinds>(Lifetime.Singleton);
            builder.Register(resolver => new NewsDesk(resolver.Resolve<Inbox>(), resolver.Resolve<NoticesState>(),
                resolver.Resolve<Construction>(), resolver.Resolve<FogOfWar>(), resolver.Resolve<SiteFinds>(), resolver.Resolve<QuestChain>(),
                resolver.Resolve<ICatalog<ISceneDefinition>>().Items, resolver.Resolve<KingdomState>().Tutorial, resolver.Resolve<KingdomState>(),
                resolver.Resolve<Kingdom.Goods.Workshops>(), resolver.Resolve<Kingdom.Army.Army>(), resolver.Resolve<Codigames.Kingdom.Lairs.Lairs>()),
                Lifetime.Singleton);
            builder.Register<ITreasury>(resolver => new Treasury(
                resolver.Resolve<ICatalog<ICurrencyDefinition>>(), resolver.Resolve<KingdomState>().Balances), Lifetime.Singleton);

            builder.Register<Placement>(Lifetime.Singleton);
            builder.Register<Construction>(Lifetime.Singleton);
            builder.Register<Harmony>(Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Goods, Lifetime.Singleton);
            builder.Register<Kingdom.Goods.ShutPreciousGate>(Lifetime.Singleton).As<Kingdom.Goods.IPreciousGate>();
            builder.Register<Kingdom.Goods.Stockpile>(Lifetime.Singleton);
            builder.Register<Kingdom.Goods.Workshops>(Lifetime.Singleton);
            builder.Register(resolver => resolver.Resolve<KingdomState>().Army, Lifetime.Singleton);
            builder.Register<Kingdom.Army.Army>(Lifetime.Singleton);
            builder.Register<BuildingGroups>(Lifetime.Singleton);
            builder.Register(resolver => new Adjacency(resolver.Resolve<CityState>(), resolver.Resolve<ICatalog<IBuildingDefinition>>(),
                resolver.Resolve<IAdjacencyRules>(), resolver.Resolve<BuildingGroups>()), Lifetime.Singleton);
            builder.Register<ManaPool>(Lifetime.Singleton);
            builder.Register<Stores>(Lifetime.Singleton);
            builder.Register<VillagerTraining>(Lifetime.Singleton);
            builder.Register<Workforce>(Lifetime.Singleton);
            builder.Register<BuildingStats>(Lifetime.Singleton);
            builder.Register<GemRush>(Lifetime.Singleton);
            builder.Register(resolver => new Harvesting(resolver.Resolve<HarvestState>(), resolver.Resolve<GroundState>(),
                resolver.Resolve<CityState>(), resolver.Resolve<IProvinceMap>(), resolver.Resolve<ICatalog<IBuildingDefinition>>(),
                resolver.Resolve<ICatalog<IFeatureDefinition>>(), resolver.Resolve<ICatalog<IHarvestSource>>(),
                resolver.Resolve<ITerrainYields>(), resolver.Resolve<ITapSettings>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<ManaPool>(), resolver.Resolve<KingdomState>().Seed, resolver.Resolve<IRevealedGround>(),
                resolver.Resolve<IResearchGates>(), resolver.Resolve<IBonuses>(), resolver.Resolve<IBoosts>(), resolver.Resolve<ILairGround>(),
                resolver.Resolve<Kingdom.Relics.IRelicAura>()), Lifetime.Singleton)
                .AsSelf().As<IPlanting>();
            builder.Register<ResearchEffects>(Lifetime.Singleton);
            builder.Register<Transplanting>(Lifetime.Singleton);
            builder.Register<Production>(Lifetime.Singleton).As<IProduction>();
            builder.Register<SiteManaSources>(Lifetime.Singleton).As<IManaSources>();
            builder.Register<Sighting>(Lifetime.Singleton);
            builder.Register<QuestGoals>(Lifetime.Singleton).AsSelf().As<IQuestGoals>().As<IBuildingGroups>();
            builder.Register(resolver => resolver.Resolve<KingdomState>().Tutorial, Lifetime.Singleton);
            builder.Register(resolver => new Kingdom.Doors.Doors(resolver.Resolve<KingdomState>().Tutorial, resolver.Resolve<QuestChain>(),
                resolver.Resolve<ICatalog<IQuestDefinition>>(), resolver.Resolve<Researching>(), resolver.Resolve<CityState>(),
                resolver.Resolve<KingdomState>().Sites, resolver.Resolve<IProvinceSites>(), resolver.Resolve<IConstructionSettings>(),
                resolver.Resolve<BagState>(), resolver.Resolve<KingdomState>().Relics),
                Lifetime.Singleton).AsSelf().As<Kingdom.Doors.IMorning>();
            builder.Register(resolver => new QuestChain(resolver.Resolve<KingdomState>().Quests, resolver.Resolve<ICatalog<IQuestDefinition>>(),
                resolver.Resolve<IQuestGoals>(), resolver.Resolve<ITreasury>(), resolver.Resolve<CityState>(),
                resolver.Resolve<ICatalog<IBuildingDefinition>>(), resolver.Resolve<Stores>(), resolver.Resolve<Harvesting>(),
                resolver.Resolve<FogOfWar>(), resolver.Resolve<GroundState>(), resolver.Resolve<IBonuses>(), resolver.Resolve<IItemGrants>(),
                resolver.Resolve<Kingdom.Modifiers.IModifiers>(), resolver.Resolve<Kingdom.Relics.IRelicDrops>()),
                Lifetime.Singleton);
            builder.Register(resolver => new Landmarks(resolver.Resolve<SitesState>(), resolver.Resolve<IProvinceSites>(),
                resolver.Resolve<FogOfWar>(), resolver.Resolve<ITreasury>(), resolver.Resolve<IKnowledgeSettings>(),
                ((Data.Fog.FogSettingsAsset)resolver.Resolve<IFogSettings>()).ClaimDiscoverRadius, resolver.Resolve<IBonuses>(),
                resolver.Resolve<ManaPool>(), resolver.Resolve<ILairGround>(), resolver.Resolve<Kingdom.Modifiers.IModifiers>()), Lifetime.Singleton);
            builder.Register(resolver => new Treasures(resolver.Resolve<KingdomState>().Fog, resolver.Resolve<FogOfWar>(),
                resolver.Resolve<IProvinceMap>(), resolver.Resolve<Footprints>(), resolver.Resolve<GroundState>(),
                resolver.Resolve<ISiteGround>(), resolver.Resolve<ITreasureSettings>(), resolver.Resolve<ITreasury>(),
                resolver.Resolve<IProduction>(), resolver.Resolve<IResearchGates>(), resolver.Resolve<ICatalog<IHarvestSource>>(),
                resolver.Resolve<ICatalog<IBuildingDefinition>>(), resolver.Resolve<Construction>(), resolver.Resolve<KingdomState>().Seed,
                resolver.Resolve<IBonuses>(), resolver.Resolve<Kingdom.Relics.IRelicDrops>()), Lifetime.Singleton);

            builder.Register(resolver =>
            {
                var state = resolver.Resolve<KingdomState>();
                var timeline = new Timeline(state.LastAdvance);
                timeline.Register(resolver.Resolve<Construction>());
                resolver.Resolve<BuildingsLiftFog>();
                // The news desk before anything finishes: an absence leaves its bubbles.
                resolver.Resolve<NewsDesk>();
                timeline.Register(resolver.Resolve<VillagerTraining>());
                timeline.Register(resolver.Resolve<Kingdom.Goods.Workshops>());
                timeline.Register(resolver.Resolve<Kingdom.Army.Army>());

                // A pool left below its cap without a clock (an older save) starts gaining where the kingdom left off.
                var mana = resolver.Resolve<ManaPool>();
                mana.Wake(state.LastAdvance);
                timeline.Register(mana);

                // The bar likewise: a new kingdom starts with none, and drips from its first second.
                var knowledge = resolver.Resolve<KnowledgeBar>();
                knowledge.Wake(state.LastAdvance);
                timeline.Register(knowledge);
                timeline.Register(resolver.Resolve<Harvesting>());

                // The boosts before the stores: a boost ending settles the rent at the old rate first.
                timeline.Register(resolver.Resolve<Boosts>());
                resolver.Resolve<BoostEffects>();

                // Stores registered after construction, so a level finishing is counted before a store fills.
                var stores = resolver.Resolve<Stores>();
                stores.WakeAll(state.LastAdvance);
                timeline.Register(stores);

                // The lairs after the stores: a raid takes from what a boundary has already banked.
                timeline.Register(resolver.Resolve<Codigames.Kingdom.Lairs.Lairs>());

                // The chain's own clock: a tutorial quest's rent rush.
                var quests = resolver.Resolve<QuestChain>();
                quests.Wake(state.LastAdvance);
                timeline.Register(quests);

                // A relic's window closes at its own moment, before the crews step through it; one closed in an
                // absence is remembered as asleep.
                timeline.Register(resolver.Resolve<Kingdom.Relics.Shrines>());
                resolver.Resolve<Relics.AsleepRelics>();

                // The crews last: their steps run between the boundaries the others draw.
                timeline.Register(resolver.Resolve<Workforce>());

                // What a technology completing does to the rest of the kingdom; treasures set down as the fog is paid for.
                resolver.Resolve<ResearchEffects>();
                resolver.Resolve<Treasures>();
                return timeline;
            }, Lifetime.Singleton);

            builder.RegisterEntryPoint<KingdomTicker>();
            builder.RegisterEntryPoint<KingdomSaver>();
        }
    }
}
