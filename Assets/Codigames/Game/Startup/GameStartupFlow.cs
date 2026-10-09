using System.Threading;
using Codigames.Game.App;
using Codigames.Game.UI.Menus;
using Codigames.Modules.UI;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    // The Game scene's start: the persistent menus come up, then the loading screen lifts off the ready game.
    public class GameStartupFlow : IAsyncStartable
    {
        private readonly LoadingScreen _loadingScreen;
        private readonly UIManager _ui;

        public GameStartupFlow(LoadingScreen loadingScreen, UIManager ui)
        {
            _loadingScreen = loadingScreen;
            _ui = ui;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await UniTask.WhenAll(_ui.ShowMenu<HeaderMenu>().AsUniTask(), _ui.ShowMenu<NavMenu>().AsUniTask(),
                _ui.ShowMenu<QuestPill>().AsUniTask(), _ui.ShowMenu<NoticesColumn>().AsUniTask(),
                _ui.ShowMenu<StandingColumn>().AsUniTask());
            await _loadingScreen.Hide(cancellation);
        }
    }
}
