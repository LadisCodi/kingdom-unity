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
using Codigames.Game.UI.Presenters;
using Codigames.Game.UI.Research;
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
        [SerializeField] private MenuCatalog _menuCatalog;
        [SerializeField] private WorldFeedbackCatalog _worldFeedbackCatalog;
        [SerializeField] private QuickInfoMessageSettings _quickInfoMessageSettings;
        [SerializeField] private UIRoot _uiRoot;
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
            builder.Register<CollectInput>(Lifetime.Singleton);
            builder.Register<FogInput>(Lifetime.Singleton);
            builder.RegisterComponent(_fogView);
            builder.RegisterComponent(_crewsView);
            builder.RegisterEntryPoint<MapTaps>();
            builder.RegisterEntryPoint<Sites.RuinsView>();
            builder.RegisterEntryPoint<WorldCues>();
            builder.RegisterEntryPoint<TapPunch>().AsSelf();
            builder.Register<RewardHold>(Lifetime.Singleton);
            builder.Register<RewardFlight>(Lifetime.Singleton);
            builder.Register<RewardFragments>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MusicDirector>();
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
            builder.Register<PlacementMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<DistrictCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.Register<TechProse>(Lifetime.Singleton);
            builder.Register<RuinCardMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<ResearchMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<TechSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>();
            builder.Register<KnowledgeSheetMenuPresenter>(Lifetime.Singleton).As<IMenuPresenter>().As<ITickable>();
            builder.RegisterEntryPoint<KnowledgeTabPresenter>();
            builder.RegisterEntryPoint<BuildPlacementFlow>();
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
