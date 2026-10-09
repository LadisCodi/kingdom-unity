using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Stage
{
    // Isolde leaning in from the left edge with an offer of help, for a player who seems stuck. A tap on her shows
    // the way to the quest.
    public class StagePeek : MonoBehaviour
    {
        private const float SLIDE_SECONDS = 0.32f;
        private const float IN_X = -66f;

        [SerializeField] private RectTransform _body;
        [SerializeField] private Button _button;
        [SerializeField] private Image _face;
        [SerializeField] private TMP_Text _say;

        private Tween _slide;
        private bool _in;

        public event Action Tapped;

        private void Awake()
        {
            _button.onClick.AddListener(() => Tapped?.Invoke());
            _body.anchoredPosition = new Vector2(Out, _body.anchoredPosition.y);
            gameObject.SetActive(false);
        }

        private void OnDestroy() => _slide?.Kill();

        private float Out => -_body.rect.width * 1.1f;

        public void Show(Sprite face, string say)
        {
            if (_in) return;
            _in = true;
            _face.sprite = face;
            _say.text = say;
            gameObject.SetActive(true);
            _slide?.Kill();
            _slide = _body.DOAnchorPosX(IN_X, SLIDE_SECONDS).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void Hide()
        {
            if (!_in) return;
            _in = false;
            _slide?.Kill();
            _slide = _body.DOAnchorPosX(Out, SLIDE_SECONDS).SetEase(Ease.InCubic).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
