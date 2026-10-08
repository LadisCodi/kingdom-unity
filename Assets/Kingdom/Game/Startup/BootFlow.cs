using System.Threading;
using Cysharp.Threading.Tasks;
using Kingdom.Game.App;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Kingdom.Game.Startup
{
    // Boot: cover the screen, then hand over to the Game scene, whose GameStartupFlow takes it from there.
    public class BootFlow : IAsyncStartable
    {
        private readonly LoadingScreen _loadingScreen;

        public BootFlow(LoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            _loadingScreen.ShowNow();

            // Not tied to `cancellation`: loading Game unloads Boot, and with it this scope, by design.
            await SceneManager.LoadSceneAsync(SceneNames.GAME, LoadSceneMode.Single)
                .ToUniTask(Progress.Create<float>(_loadingScreen.SetProgress));
        }
    }
}
