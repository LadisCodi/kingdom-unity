using Codigames.Game.App;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Economy;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Economy;
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

        [Header("App")]
        [SerializeField] private SoundCatalog _soundCatalog;
        [SerializeField] private MMSoundManager _soundManager;
        [SerializeField] private LoadingScreen _loadingScreen;
        [SerializeField] private AppLifecycleHook _lifecycleHook;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterBalance(builder);

            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register<Localizer>(Lifetime.Singleton).WithParameter("sourceCulture", "en-US");
            builder.Register<NumberFormat>(Lifetime.Singleton);

            builder.RegisterInstance(_soundCatalog).As<ISoundCatalog>();
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
            builder.RegisterInstance(_currencies).As<ICatalog<ICurrencyDefinition>>();
            builder.RegisterInstance(_buildings).As<ICatalog<IBuildingDefinition>>();
            builder.RegisterInstance(_construction).As<IConstructionSettings>();
        }
    }
}
