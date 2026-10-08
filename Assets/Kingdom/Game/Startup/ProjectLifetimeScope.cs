using Kingdom.Game.App;
using Kingdom.Game.Audio;
using Kingdom.Game.Clock;
using Kingdom.Game.I18n;
using MoreMountains.Tools;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Kingdom.Game.Startup
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
            builder.Register<Localization>(Lifetime.Singleton);
            builder.Register<NumberFormat>(Lifetime.Singleton);

            builder.RegisterInstance(_soundCatalog);
            builder.RegisterComponent(_soundManager);
            builder.Register<ISoundService, SoundService>(Lifetime.Singleton);

            builder.RegisterComponent(_loadingScreen);
            builder.RegisterEntryPoint<AppLifecycle>().AsSelf();
        }
    }
}
