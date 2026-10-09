using System.Threading;
using Codigames.Game.App;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Codigames.Game.Startup
{
    // Boot: cap the frame rate, cover the screen, then hand over to the Game scene, whose GameStartupFlow takes it
    // from there.
    public class BootFlow : IAsyncStartable
    {
        // Frames a second, at most: smooth on a phone without running its battery down.
        private const int TARGET_FRAME_RATE = 60;

        private readonly LoadingScreen _loadingScreen;

        public BootFlow(LoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // The target rate holds only with vertical sync off (mobile ignores vSyncCount; the editor does not).
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TARGET_FRAME_RATE;
            _loadingScreen.ShowNow();

            // Not tied to `cancellation`: loading Game unloads Boot, and with it this scope, by design.
            await SceneManager.LoadSceneAsync(SceneNames.GAME, LoadSceneMode.Single)
                .ToUniTask(Progress.Create<float>(_loadingScreen.SetProgress));
        }
    }
}
