using Codigames.Game.UI;
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
        [SerializeField] private MenuCatalog _menuCatalog;
        [SerializeField] private WorldFeedbackCatalog _worldFeedbackCatalog;
        [SerializeField] private QuickInfoMessageSettings _quickInfoMessageSettings;
        [SerializeField] private UIRoot _uiRoot;
        [SerializeField] private CameraManager _cameraManager;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_menuCatalog);
            builder.RegisterInstance(_worldFeedbackCatalog);
            builder.RegisterInstance(_quickInfoMessageSettings);
            builder.RegisterComponent(_uiRoot);
            builder.RegisterComponent(_cameraManager);

            builder.Register<MenuFactory>(Lifetime.Singleton);
            builder.Register<UIManager>(Lifetime.Singleton);
            builder.RegisterEntryPoint<MenuBackInputHandler>();

            builder.Register<IWorldFeedbackService, WorldFeedbackService>(Lifetime.Singleton);
            builder.Register<IQuickInfoMessageLayer, QuickInfoMessageLayer>(Lifetime.Singleton);
            builder.Register<IQuickInfoMessageService, QuickInfoMessageService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameStartupFlow>();
        }
    }
}
