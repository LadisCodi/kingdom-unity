
using System.Threading;
using Codigames.Game.UI.Widgets;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.UI.Widgets
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeScaleAnimation : MonoBehaviour, IStateViewAnimation
    {
        [SerializeField] private float _duration = 0.25f;
        private CanvasGroup _group;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        public async UniTask PlayShow(CancellationToken ct)
        {
            _group.interactable = false;

            var seq = DOTween.Sequence()
                .Append(_group.DOFade(1f, _duration))
                .Join(transform.DOScale(1f, _duration).From(0.8f).SetEase(Ease.OutBack));

            await seq.Play().ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);

            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        public async UniTask PlayHide(CancellationToken ct)
        {
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var seq = DOTween.Sequence()
                .Append(_group.DOFade(0f, _duration))
                .Join(transform.DOScale(0.8f, _duration).SetEase(Ease.InQuad));

            await seq.Play().ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
        }
    }
}