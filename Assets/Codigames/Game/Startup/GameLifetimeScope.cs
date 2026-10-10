using System.Collections.Generic;
using Codigames.Game.Audio;
using Codigames.Game.Cameras;
using Codigames.Game.City;
using Codigames.Game.Crews;
using Codigames.Game.Feedback;
using Codigames.Game.Fog;
using Codigames.Game.Harvest;
using Codigames.Game.Map;
using Codigames.Game.Session;
using Codigames.Game.UI;
using Codigames.Game.UI.Hud;
using Codigames.Game.UI.Notices;
using Codigames.Game.UI.Presenters;
using Codigames.Game.UI.Research;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Unlocks;
using Codigames.Kingdom;
using Codigames.Kingdom.Doors;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Core;
using Codigames.Kingdom.Map;
using Codigames.Modules.Cameras;
using Codigames.Modules.Feedback;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    // The Game scene's scope: what lives while the province and the world map are on screen.
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private ProvinceMap _province;
        [SerializeField] private CityView _cityView;
        [SerializeField] private GroundView _groundView;
        [SerializeField] private GhostView _ghostView;
        [SerializeField] private FogView _fogView;
        [SerializeField] private CrewsView _crewsView;
        [SerializeField] private VillagersView _villagersView;
        [SerializeField] private TreasureArt _treasureArt;
        [SerializeField] private SightArt _sightArt;
        [SerializeField] private Lairs.LairBubbleView _lairBubble;
        [SerializeField] private MenuCatalog _menuCatalog;
        [SerializeField] private WorldFeedbackCatalog _worldFeedbackCatalog;
        [SerializeField] private QuickInfoMessageSettings _quickInfoMessageSettings;
        [SerializeField] private UIRoot _uiRoot;
        [SerializeField] private StageView _stage;
        [SerializeField] private UnlockSplash _unlockSplash;
        [SerializeField] private SownPlot _sownPlot;
        [SerializeField] private SpriteArt _spriteArt;
        [SerializeField] private TapFlashArt _tapFlashArt;
        [SerializeField] private Codigames.Game.UI.Kit.UiIcons _uiIcons;
        [SerializeField] private Codigames.Game.UI.Kit.PortraitArt _portraitArt;
        [SerializeField] private HoldRing _holdRing;
        [SerializeField] private CinemachineCameraRig _cameraRig;
        [SerializeField] private CameraInputHook _cameraInput;
        [SerializeField] private CameraSettings _cameraSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_province).AsSelf().As<IProvinceMap>();
            KingdomInstaller.Install(builder);
            builder.RegisterComponent(_cityView);
            builder.RegisterComponent(_groundView);
            builder.RegisterComponent(_ghostView);
            builder.RegisterEntryPoint<MapGestures>().AsSelf();
            builder.Register<HarvestInput>(Lifetime.Singleton);
            builder.Register<Lairs.LairWords>(Lifetime.Singleton);
            builder.Register<Heroes.HeroWords>(Lifetime.Singleton);
            builder.Register<UI.Heroes.HeroCards>(Lifetime.Singleton);
            builder.Register<CollectInput>(Lifetime.Singleton);
            builder.Register<FogInput>(Lifetime.Singleton);
            builder.Register<TreasureInput>(Lifetime.Singleton);
            builder.RegisterInstance(_treasureArt);
            builder.RegisterEntryPoint<TreasuresView>();
            builder.RegisterInstance(_sownPlot);
            builder.RegisterEntryPoint<SownPlotsView>();
            builder.RegisterInstance(_spriteArt);
            if (_tapFlashArt != null) builder.RegisterInstance(_tapFlashArt);
            builder.RegisterInstance(_uiIcons);
            builder.RegisterInstance(_portraitArt);
            builder.RegisterEntryPoint<CellBarsView>();
            builder.RegisterInstance(_sightArt);
            builder.RegisterEntryPoint<SilhouettesView>();
            builder.RegisterEntryPoint<ReachBorderView>();
            builder.Register<Codigames.Game.City.WorkAreaView>(Lifetime.Singleton).AsSelf().As<ITickable>();
            builder.RegisterComponent(_fogView);
            builder.RegisterComponent(_crewsView);
            if (_villagersView != null) builder.RegisterComponent(_villagersView);
            builder.RegisterEntryPoint<MapTaps>();
            builder.RegisterEntryPoint<Sites.RuinsView>();
            builder.RegisterEntryPoint<Sites.LandmarksView>();
            builder.RegisterInstance(_lairBubble);
            builder.RegisterEntryPoint<Lairs.LairsView>().AsSelf();
            builder.RegisterEntryPoint<Relics.ShrinesView>().AsSelf();
            builder.RegisterEntryPoint<Lairs.RaidAlarm>();
            builder.RegisterEntryPoint<WorldCues>();
            builder.RegisterEntryPoint<TapPunch>().AsSelf();
            builder.RegisterEntryPoint<LandingFx>().AsSelf();
            builder.Register<BuildBurst>(Lifetime.Singleton);
            builder.Register<RewardHold>(Lifetime.Singleton);
            builder.Register<RewardFlight>(Lifetime.Singleton);
            builder.Register<RewardFragments>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MusicDirector>().AsSelf();
            builder.RegisterEntryPoint<AmbienceDirector>();
            RegisterUI(builder);
            RegisterFeedback(builder);
            RegisterCamera(builder);

            builder.RegisterEntryPoint<GameStartupFlow>();
        }

        private void RegisterUI(IContainerBuilder builder)
        {
            builder.RegisterInstance(_menuCatalog).AsSelf().As<IMenuGroups>();
            builder.RegisterComponent(_uiRoot);
            builder.Register<MenuFactory>(Lifetime.Singleton).AsSelf().As<IMenuViewFactory>();
            // The presenters are read when a menu is first shown: a presenter may itself depend on the UIManager.
            builder.Register(resolver => new UIManager(
                () => resolver.Resolve<IEnumerable<IMenuPresenter>>(), resolver.Resolve<IMenuGroups>()), Lifetime.Singleton);
            builder.RegisterEntryPoint<MenuBackInputHandler>();

            builder.Register<HeaderMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<NavMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<BuildMenuPresenter>(Lifetime.Singleton).AsSelf().As<IMenuPresenter>();
            builder.Register<PlacementMenuPresenter>(Lifetime.Singleton).AsSelf().As<IMenuPresenter>();
            builder.Register<DistrictCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>().As<IInspectedDistrict>();
            builder.Register<UpgradeSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<CardFraming>(Lifetime.Singleton);
            builder.Register<Codigames.Game.UI.Bag.ItemProse>(Lifetime.Singleton);
            builder.Register<Codigames.Game.UI.Bag.ItemTiles>(Lifetime.Singleton);
            builder.Register<BagMenuPresenter>(Lifetime.Singleton).AsSelf().As<IMenuPresenter>().As<ITickable>();
            builder.Register<Relics.RelicWords>(Lifetime.Singleton);
            builder.Register<Relics.RelicActions>(Lifetime.Singleton);
            builder.Register<UI.Relics.RelicCards>(Lifetime.Singleton);
            builder.Register<RelicSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<UI.Relics.ShrinePanels>(Lifetime.Singleton);
            builder.Register<RelicPickerMenuPresenter>(Lifetime.Singleton).AsSelf().As<IMenuPresenter>();
            builder.Register<ConfirmMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<Codigames.Game.UI.Store.ProductProse>(Lifetime.Singleton);
            builder.Register<IapMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<PayerMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.RegisterEntryPoint<Codigames.Game.Store.PayerGate>().AsSelf();
            builder.RegisterEntryPoint<Codigames.Game.Store.OfferLatch>();
            builder.RegisterEntryPoint<Codigames.Game.Store.OfferSplashGate>().AsSelf();
            builder.Register<Codigames.Game.UI.Store.OfferTiles>(Lifetime.Singleton);
            builder.Register<OfferSplashMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<SpeedupMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<Codigames.Game.UI.Buildings.BuildingStatProse>(Lifetime.Singleton);
            builder.Register<TechProse>(Lifetime.Singleton);
            builder.Register<RuinCardMenuPresenter>(Lifetime.Singleton).AsSelf().As<IMenuPresenter>();
            builder.Register<Codigames.Game.UI.Quests.QuestProse>(Lifetime.Singleton);
            builder.Register<Codigames.Game.UI.Quests.QuestFocus>(Lifetime.Singleton);
            builder.Register<QuestPillPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<NoticeBoard>(Lifetime.Singleton);
            builder.Register<Codigames.Game.UI.Data.PriceTerms>(Lifetime.Singleton);
            builder.Register<NoticesColumnPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<StandingColumnPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<NoticeCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<LandmarkCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<LairCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<Lairs.AttackParty>(Lifetime.Singleton);
            builder.Register<Battles.PlaybackPreferences>(Lifetime.Singleton);
            builder.Register<AttackSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<HeroesMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<HeroCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<RevealScreenPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<StoreMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<HeroPickerMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<AdScreenPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<Ads.RewardedAd>(Lifetime.Singleton);
            builder.Register<BattleScreenPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<ResearchMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<TechSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<KnowledgeSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.RegisterEntryPoint<KnowledgeTabPresenter>();
            builder.RegisterEntryPoint<BuildPlacementFlow>();
            builder.RegisterEntryPoint<MoveStarter>();
            builder.RegisterComponent(_holdRing);
            RegisterStage(builder);
        }

        // The tutorial stage: the kingdom's conditions and the screen's, the director, and the stage itself.
        private void RegisterStage(IContainerBuilder builder)
        {
            builder.Register<Dev.DevSwitches>(Lifetime.Singleton);
            builder.Register<UiTargets>(Lifetime.Singleton);
            builder.Register<StageHint>(Lifetime.Singleton);
            builder.Register(resolver => new MapTargets(resolver.Resolve<KingdomState>(), resolver.Resolve<Kingdom.Map.IProvinceMap>(),
                resolver.Resolve<Kingdom.Fog.FogOfWar>(), resolver.Resolve<Kingdom.Harvest.Harvesting>(),
                resolver.Resolve<Modules.Core.ICatalog<Kingdom.City.IBuildingDefinition>>(), resolver.Resolve<Kingdom.Sites.IProvinceSites>(),
                resolver.Resolve<Kingdom.Crews.Workforce>(), resolver.Resolve<Kingdom.Economy.Stores>(), resolver.Resolve<Kingdom.City.Placement>(),
                resolver.Resolve<ReachSpots>()), Lifetime.Singleton);
            builder.Register<PlotGlow>(Lifetime.Singleton);
            builder.RegisterEntryPoint<TapCount>().AsSelf();
            builder.Register<KingdomConditions>(Lifetime.Singleton);
            builder.Register<CampaignConditions>(Lifetime.Singleton);
            builder.Register<ReachSpots>(Lifetime.Singleton);
            builder.Register<ScreenConditions>(Lifetime.Singleton);
            builder.Register<IConditions>(resolver => new Conditions(new IConditionReader[]
            {
                resolver.Resolve<KingdomConditions>(), resolver.Resolve<CampaignConditions>(), resolver.Resolve<ScreenConditions>(),
            }), Lifetime.Singleton);
            builder.Register<ScenePurse>(Lifetime.Singleton).As<IScenePurse>();
            builder.Register(resolver => new SceneDirector(resolver.Resolve<ICatalog<ISceneDefinition>>().Items,
                resolver.Resolve<KingdomState>().Tutorial, resolver.Resolve<IConditions>(), resolver.Resolve<IScenePurse>(),
                resolver.Resolve<IMorning>()), Lifetime.Singleton);
            builder.Register<StageContext>(Lifetime.Singleton).As<IStageContext>();
            builder.Register<Openings>(Lifetime.Singleton);
            builder.RegisterComponent(_unlockSplash);
            builder.RegisterEntryPoint<UnlockSplashPresenter>().AsSelf();
            builder.RegisterComponent(_stage);
            builder.RegisterEntryPoint<StagePresenter>().AsSelf();
            builder.RegisterEntryPoint<IdleHelp>();
        }

        private void RegisterFeedback(IContainerBuilder builder)
        {
            builder.RegisterInstance(_worldFeedbackCatalog);
            builder.RegisterInstance(_quickInfoMessageSettings);
            builder.Register<IWorldFeedbackService, WorldFeedbackService>(Lifetime.Singleton);
            builder.Register<IQuickInfoMessageService, QuickInfoMessageService>(Lifetime.Singleton);
        }

        private void RegisterCamera(IContainerBuilder builder)
        {
            builder.RegisterComponent(_cameraRig).As<ICameraRig>();
            // Tuned in its asset, bounded by the province.
            builder.Register(resolver => new ProvinceCameraSettings(_cameraSettings, _province, resolver.Resolve<ICameraRig>()), Lifetime.Singleton)
                .AsSelf().As<ICameraSettings>();
            builder.Register<CameraController>(Lifetime.Singleton);
            builder.RegisterComponent(_cameraInput);
            builder.RegisterEntryPoint<CameraTicker>();
        }
    }
}
