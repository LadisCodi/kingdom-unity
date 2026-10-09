using Codigames.Game.App;
using Codigames.Game.Audio;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Doors;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Fog;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Data.Magic;
using Codigames.Game.Data.Quests;
using Codigames.Game.Data.Research;
using Codigames.Game.Data.Sites;
using Codigames.Game.Data.Tutorial;
using Codigames.Game.Localization;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Lifecycle;
using Codigames.Modules.Localization;
using MoreMountains.Tools;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    // The app-wide scope (VContainer's root, created before any scene): what lives as long as the app.
    public class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Balance")]
        [SerializeField] private CurrencyCollection _currencies;
        [SerializeField] private BuildingCollection _buildings;
        [SerializeField] private ConstructionSettingsAsset _construction;
        [SerializeField] private HarvestSourceCollection _harvestSources;
        [SerializeField] private FeatureCollection _features;
        [SerializeField] private TerrainCollection _terrains;
        [SerializeField] private TapSettingsAsset _tap;
        [SerializeField] private ManaSettingsAsset _mana;
        [SerializeField] private EconomySettingsAsset _economy;
        [SerializeField] private TrainingSettingsAsset _training;
        [SerializeField] private FogSettingsAsset _fog;
        [SerializeField] private TechnologyCollection _technologies;
        [SerializeField] private TechTreeAsset _techTree;
        [SerializeField] private KnowledgeSettingsAsset _knowledge;
        [SerializeField] private ProvinceSitesAsset _sites;
        [SerializeField] private TreasureSettingsAsset _treasure;
        [SerializeField] private QuestCollection _quests;
        [SerializeField] private SceneCollection _scenes;
        [SerializeField] private SpeakerCollection _speakers;
        [SerializeField] private StageSettingsAsset _stage;
        [SerializeField] private UnlockCollection _unlocks;
        [SerializeField] private ItemCollection _items;

        [Header("App")]
        [SerializeField] private LocalizationCatalog _localization;
        [SerializeField] private SoundCatalog _soundCatalog;
        [SerializeField] private MusicSettings _music;
        [SerializeField] private MMSoundManager _soundManager;
        [SerializeField] private LoadingScreen _loadingScreen;
        [SerializeField] private AppLifecycleHook _lifecycleHook;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterBalance(builder);

            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register(_ =>
            {
                var localizer = new Localizer("en-US");
                _localization.Fill(localizer);
                localizer.SetCulture(DeviceLanguage.Culture());
                return localizer;
            }, Lifetime.Singleton);
            builder.Register<NumberFormat>(Lifetime.Singleton);

            builder.RegisterInstance(_soundCatalog).As<ISoundCatalog>();
            builder.RegisterInstance(_music);
            builder.RegisterComponent(_soundManager);
            builder.Register<ISoundPlayer, FeelSoundPlayer>(Lifetime.Singleton);
            builder.Register<ISoundService, SoundService>(Lifetime.Singleton);

            builder.RegisterComponent(_loadingScreen);
            builder.Register<AppLifecycle>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterComponent(_lifecycleHook);
        }

        // The balance, as Kingdom reads it: catalogs of definitions and settings, from the data assets.
        private void RegisterBalance(IContainerBuilder builder)
        {
            builder.RegisterInstance(_currencies).As<ICatalog<ICurrencyDefinition>>().As<IPlankCurrencies>().As<ICurrencyIcons>();
            builder.RegisterInstance(_buildings).AsSelf().As<ICatalog<IBuildingDefinition>>().As<IBuildingCards>();
            builder.RegisterInstance(_construction).As<IConstructionSettings>();
            builder.RegisterInstance(_harvestSources).As<ICatalog<IHarvestSource>>();
            builder.RegisterInstance(_features).AsSelf().As<ICatalog<IFeatureDefinition>>();
            builder.RegisterInstance(_terrains).As<ITerrainYields>();
            builder.RegisterInstance(_tap).As<ITapSettings>();
            builder.RegisterInstance(_mana).As<IManaSettings>();
            builder.RegisterInstance(_economy).As<IEconomySettings>().As<IWorkerSettings>().As<IBagSettings>().As<IRushSettings>();
            builder.RegisterInstance(_training).As<ITrainingSettings>();
            builder.RegisterInstance(_fog).As<IFogSettings>().As<ISightSettings>();
            builder.RegisterInstance(_technologies).As<ICatalog<ITechnology>>().As<ITechnologyCards>();
            builder.RegisterInstance(_techTree).AsSelf().As<ITechTree>();
            builder.RegisterInstance(_knowledge).As<IKnowledgeSettings>();
            builder.RegisterInstance(_sites).AsSelf().As<IProvinceSites>();
            builder.RegisterInstance(_treasure).As<ITreasureSettings>();
            builder.RegisterInstance(_quests).As<ICatalog<IQuestDefinition>>();
            builder.RegisterInstance(_scenes).As<ICatalog<ISceneDefinition>>();
            builder.RegisterInstance(_speakers).As<ICatalog<ISpeaker>>();
            builder.RegisterInstance(_stage).As<IStageSettings>();
            builder.RegisterInstance(_unlocks).As<ICatalog<IUnlock>>();
            builder.RegisterInstance(_items).AsSelf().As<ICatalog<IItemDefinition>>();
        }
    }
}
