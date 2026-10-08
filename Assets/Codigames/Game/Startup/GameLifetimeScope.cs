using System.Collections.Generic;
using Codigames.Game.Cameras;
using Codigames.Game.Feedback;
using Codigames.Game.Map;
using Codigames.Game.UI;
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
            builder.Register<ICameraSettings>(resolver => new ProvinceCameraSettings(_cameraSettings, _province), Lifetime.Singleton);
            builder.Register<CameraController>(Lifetime.Singleton);
            builder.RegisterComponent(_cameraInput);
            builder.RegisterEntryPoint<CameraTicker>();
        }
    }
}
