using System.Threading;
using Cysharp.Threading.Tasks;
using Kingdom.Game.App;
using VContainer.Unity;

namespace Kingdom.Game.Startup
{
    // The Game scene's start: once the game is ready under it, the loading screen lifts.
    public class GameStartupFlow : IAsyncStartable
    {
        private readonly LoadingScreen _loadingScreen;

        public GameStartupFlow(LoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _loadingScreen.Hide(cancellation);
        }
    }
}
