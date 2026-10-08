
using System;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

namespace Kingdom.Game.Feedback
{
    // A pooled quick-info message: shows a line of text and plays its MMF_Player, then returns to the pool.
    // The animation lives on the prefab's player so designers tune each message; keep its OnComplete event
    // enabled (the default) so the view is released when the feedbacks finish.
    public class QuickInfoMessageView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private MMF_Player _player;

        public event Action<QuickInfoMessageView> Finished;

        public void Configure(QuickInfoMessageData data)
        {
            if (_label != null) _label.text = data.Message;

            Place(data.ScreenPosition ?? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

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

        // Places the message at a screen point inside its UI container. Converts the screen point to the
        // container's local space so it works for both overlay and screen-space-camera canvases.
        private void Place(Vector2 screenPoint)
        {
            var rect = (RectTransform)transform;

            if (transform.parent is RectTransform container)
            {
                var canvas = GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPoint, camera, out var local))
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = local;
                }
            }

            transform.SetAsLastSibling();
        }
    }
}
