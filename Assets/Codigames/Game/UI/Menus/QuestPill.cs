using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codigames.Game.UI.Widgets;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The quest tracker: an unrolled scroll, bottom left, that is its own button. Running, it shows the quest's
    // name, its line, the goal's mark resting on its bar; done, only what it pays and the verb that takes it, and
    // it bobs. A new quest unrolls from the left roller; a claimed one rolls back up. View only: the
    // QuestPillPresenter decides.
    public class QuestPill : Menu
    {
        private const float UNROLL_SECONDS = 0.45f;
        private const float ROLL_SECONDS = 0.35f;
        private const float WORDS_SECONDS = 0.18f;
        private const float BOB_RPX = 8f;
        private const float BOB_SECONDS = 1.4f;

        [SerializeField] private Button _scroll;
        [SerializeField] private RectTransform _base;
        [SerializeField] private CanvasGroup _content;
        [SerializeField] private GameObject _run;
        [SerializeField] private GameObject _done;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private Image _mark;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private RectTransform _rewards;
        [SerializeField] private RewardChip _rewardPrefab;
        [SerializeField] private RectTransform _bob;

        private readonly List<RewardChip> _chips = new();
        private Tween _bobbing;
        private Tween _roll;

        public event Action Tapped;

        // Where the scroll is on the screen: what a claim's reward flies from.
        public RectTransform Scroll => (RectTransform)_scroll.transform;

        public void Show(string name, string line, Sprite mark)
        {
            _name.text = name;
            _line.text = line;
            _mark.sprite = mark;
        }

        public void SetProgress(float fraction, string label) => _bar.Set(fraction, label);

        // The done face: what it pays, and Claim; or the running face.
        public void SetDone(bool done, IReadOnlyList<(Sprite Icon, string Amount)> rewards)
        {
            _run.SetActive(!done);
            _done.SetActive(done);
            if (done)
            {
                for (var i = 0; i < rewards.Count; i++)
                {
                    if (i == _chips.Count) _chips.Add(Instantiate(_rewardPrefab, _rewards));
                    _chips[i].gameObject.SetActive(true);
                    _chips[i].Show(rewards[i].Icon, rewards[i].Amount);
                }

                for (var i = rewards.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);
            }

            _bobbing?.Kill();
            _bob.anchoredPosition = Vector2.zero;
            if (done) _bobbing = _bob.DOAnchorPosY(BOB_RPX, BOB_SECONDS).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        // The bar and its count climb from empty to the goal, eased out.
        public async Task FillUp(double goal, Func<double, string> label, float seconds)
        {
            var shown = 0f;
            var tween = DOTween.To(() => shown, v =>
            {
                shown = v;
                _bar.Set(v, label(v >= 1 ? goal : Math.Floor(v * goal)));
            }, 1f, seconds).SetEase(Ease.OutCubic);
            await tween.AsyncWaitForCompletion();
        }

        public void SetHidden(bool hidden)
        {
            CanvasGroup.alpha = hidden ? 0 : 1;
            CanvasGroup.blocksRaycasts = !hidden;
        }

        // The parchment widens rightwards from its left roller; the words fade in as it reaches full size.
        public async Task Unroll()
        {
            _roll?.Kill();
            _content.alpha = 0;
            _base.localScale = new Vector3(0.12f, 1, 1);
            var sequence = DOTween.Sequence()
                .Append(_base.DOScaleX(1, UNROLL_SECONDS).SetEase(Ease.OutCubic))
                .Insert(UNROLL_SECONDS - WORDS_SECONDS, _content.DOFade(1, WORDS_SECONDS));
            _roll = sequence;
            await sequence.AsyncWaitForCompletion();
        }

        public async Task RollUp()
        {
            _roll?.Kill();
            _bobbing?.Kill();
            var sequence = DOTween.Sequence()
                .Append(_content.DOFade(0, WORDS_SECONDS))
                .Append(_base.DOScaleX(0.12f, ROLL_SECONDS).SetEase(Ease.InCubic));
            _roll = sequence;
            await sequence.AsyncWaitForCompletion();
        }

        public void Settle()
        {
            _roll?.Kill();
            _base.localScale = Vector3.one;
            _content.alpha = 1;
        }

        protected override void SubscribeToEventsInternal() => _scroll.onClick.AddListener(OnTapped);

        protected override void UnsubscribeFromEventsInternal() => _scroll.onClick.RemoveListener(OnTapped);

        protected override void DisposeInternal()
        {
            _bobbing?.Kill();
            _roll?.Kill();
        }

        private void OnTapped() => Tapped?.Invoke();
    }
}
