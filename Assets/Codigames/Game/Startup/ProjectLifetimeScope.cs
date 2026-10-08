using Codigames.Game.App;
using Codigames.Game.Audio;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Lifecycle;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using MoreMountains.Tools;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    // The app-wide scope (VContainer's root, created before any scene): what lives as long as the app.
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private SoundCatalog _soundCatalog;
        [SerializeField] private MMSoundManager _soundManager;
        [SerializeField] private LoadingScreen _loadingScreen;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register<Localizer>(Lifetime.Singleton).WithParameter("sourceCulture", "en-US");
            builder.Register<NumberFormat>(Lifetime.Singleton);

            builder.RegisterInstance(_soundCatalog);
            builder.RegisterComponent(_soundManager);
            builder.Register<ISoundService, SoundService>(Lifetime.Singleton);
            builder.Register<IUISoundPlayer, UISoundPlayer>(Lifetime.Singleton);

            builder.RegisterComponent(_loadingScreen);
            builder.RegisterEntryPoint<AppLifecycle>().AsSelf();
        }
    }
}
