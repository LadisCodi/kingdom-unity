using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.App
{
    // The screen over everything while the game boots. Lives on the project scope, so it survives the
    // switch from the Boot scene to the Game scene.
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private Image _progressFill;
        [SerializeField] private float _fadeDuration = 0.3f;

        private CanvasGroup _group;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        public void ShowNow()
        {
            gameObject.SetActive(true);
            Group.alpha = 1f;
            Group.blocksRaycasts = true;
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (_progressFill != null) _progressFill.fillAmount = Mathf.Clamp01(progress);
        }

        public async UniTask Hide(CancellationToken cancellation = default)
        {
            Group.blocksRaycasts = false;
            await Group.DOFade(0f, _fadeDuration).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellation);
            gameObject.SetActive(false);
        }
    }
}
