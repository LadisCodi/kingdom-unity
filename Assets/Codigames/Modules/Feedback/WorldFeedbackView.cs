using System;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Codigames.Modules.Feedback
{
    // A pooled, fire-and-forget visual in the world (a coin that floats up and fades). What it does is its
    // MMF_Player, tuned in the editor; keep the player's OnComplete event enabled so the view is released.
    public abstract class WorldFeedbackView : MonoBehaviour
    {
        [SerializeField] private MMF_Player _player;

        public event Action<WorldFeedbackView> Finished;

        public async UniTask Play()
        {
            var completion = new UniTaskCompletionSource();
            void HandleComplete() => completion.TrySetResult();
            _player.Events.OnComplete.AddListener(HandleComplete);

            try
            {
                _player.PlayFeedbacks();
                await completion.Task.AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException) { }
            finally
            {
                _player.Events.OnComplete.RemoveListener(HandleComplete);
                Finished?.Invoke(this);
            }
        }
    }
}
