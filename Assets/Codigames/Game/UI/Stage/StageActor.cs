using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Stage
{
    // One side of the stage: a figure standing on the box, cut at the waist by it. It slides in from its edge,
    // changes face in place, and steps back dimmed while the other side speaks.
    public class StageActor : MonoBehaviour
    {
        private const float ENTER_SECONDS = 0.28f;
        private const float FADE_SECONDS = 0.22f;
        private const float DIM_SECONDS = 0.2f;
        private const float ENTER_FROM = 0.4f;
        private const float UNLIT_SCALE = 0.94f;
        private static readonly Color UNLIT = new(0.6f, 0.58f, 0.56f, 1f);

        [SerializeField] private RectTransform _clip;
        [SerializeField] private Image _figure;
        [SerializeField] private CanvasGroup _group;
        [SerializeField, Tooltip("+1 slides in from the right, -1 from the left.")] private float _edge = -1;
        [SerializeField, Tooltip("The tallest a figure shows above the box, in rpx.")] private float _maxHeight = 690;

        private Sequence _entering;
        private Tween _dimming;

        public string Speaker { get; private set; }

        public bool IsOn => Speaker != null;

        // How tall the figure stands from its feet, in the stage's units.
        public float StandingHeight => _clip.sizeDelta.y;

        // `speaker` on this side: an entrance when it is someone new, a change of face when they are already here.
        public void Cast(string speaker, Sprite picture)
        {
            var entering = Speaker != speaker;
            Speaker = speaker;
            SetPicture(picture);
            if (!entering) return;

            gameObject.SetActive(true);
            _entering?.Kill();
            var width = _clip.rect.width;
            _clip.anchoredPosition = new Vector2(_edge * width * ENTER_FROM, _clip.anchoredPosition.y);
            _group.alpha = 0;
            _entering = DOTween.Sequence()
                .Join(_clip.DOAnchorPosX(0, ENTER_SECONDS).SetEase(Ease.OutCubic))
                .Join(_group.DOFade(1, FADE_SECONDS))
                .SetUpdate(true);
        }

        // A figure with no room above the box (it would stand under the header) is not shown at all.
        public void SetRoom(bool room) => _clip.gameObject.SetActive(room);

        public void Leave()
        {
            Speaker = null;
            _entering?.Kill();
            _dimming?.Kill();
            gameObject.SetActive(false);
        }

        // The speaker is lit; the other side is dimmed and set back a step.
        public void Light(bool lit)
        {
            _dimming?.Kill();
            var scale = lit ? 1f : UNLIT_SCALE;
            _dimming = DOTween.Sequence()
                .Join(_figure.DOColor(lit ? Color.white : UNLIT, DIM_SECONDS))
                .Join(_figure.rectTransform.DOScale(scale, DIM_SECONDS))
                .SetUpdate(true);
        }

        // The figure fills the side's width; only its top shows, up to the tallest a figure may stand.
        private void SetPicture(Sprite picture)
        {
            _figure.sprite = picture;
            _figure.enabled = picture != null;
            if (picture == null) return;

            var width = _clip.rect.width;
            var height = width * picture.rect.height / picture.rect.width;
            _figure.rectTransform.sizeDelta = new Vector2(_figure.rectTransform.sizeDelta.x, height);
            _clip.sizeDelta = new Vector2(_clip.sizeDelta.x, Mathf.Min(height, _maxHeight));
        }
    }
}
