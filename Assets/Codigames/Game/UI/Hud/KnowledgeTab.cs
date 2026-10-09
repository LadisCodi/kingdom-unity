using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Hud
{
    // The Knowledge bar, a tab of painted wood hanging under the plank: the book, what is held, a copper capsule
    // of ten glass phials — full for each point held, rising in the one dripping in — and, under them, when the
    // next point and the whole bar arrive, taking turns. Its + (and the rest of it) opens the Knowledge sheet.
    // It slides up under the plank while a menu that does not spend Knowledge is open. View only.
    public class KnowledgeTab : MonoBehaviour
    {
        private const float TURN_SECONDS = 2.5f;

        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _body;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private List<Image> _phials = new();
        [SerializeField] private TMP_Text _next;
        [SerializeField] private TMP_Text _full;
        [SerializeField] private float _slideSeconds = 0.24f;

        private bool _turning;
        private bool _tucked;
        private Tween _slide;

        public event Action Tapped;

        // Held points; the share of the next one dripping in; the two captions (either may be empty).
        public void Show(int held, int cap, float next, string nextIn, string fullIn)
        {
            _value.text = held.ToString();
            for (var i = 0; i < _phials.Count; i++)
            {
                _phials[i].gameObject.SetActive(i < cap);
                var fill = i < held ? 1f : i == held && held < cap ? next : 0f;
                _phials[i].fillAmount = fill;
            }

            _next.text = nextIn;
            _full.text = fullIn;
            _turning = !string.IsNullOrEmpty(nextIn) && !string.IsNullOrEmpty(fullIn);
        }

        public void SetTucked(bool tucked)
        {
            if (_tucked == tucked) return;

            _tucked = tucked;
            _slide?.Kill();
            if (!tucked) _body.gameObject.SetActive(true);
            _slide = _body.DOAnchorPosY(tucked ? _body.rect.height + 12 : 0, _slideSeconds)
                .SetEase(tucked ? Ease.InCubic : Ease.OutCubic)
                .OnComplete(() => _body.gameObject.SetActive(!_tucked));
        }

        private void Update()
        {
            // The two captions share one place: the next point, then the whole bar, in turn.
            var showFull = _turning && Mathf.Repeat(Time.unscaledTime, 2 * TURN_SECONDS) >= TURN_SECONDS;
            _next.alpha = Mathf.MoveTowards(_next.alpha, showFull ? 0 : 1, Time.unscaledDeltaTime / 0.3f);
            _full.alpha = Mathf.MoveTowards(_full.alpha, showFull ? 1 : 0, Time.unscaledDeltaTime / 0.3f);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable() => _button.onClick.RemoveListener(OnTapped);

        private void OnDestroy() => _slide?.Kill();

        private void OnTapped() => Tapped?.Invoke();
    }
}
