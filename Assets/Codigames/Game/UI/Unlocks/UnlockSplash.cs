using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codigames.Game.UI.Unlocks
{
    // THE UNLOCK SPLASH (Docs/features/23-tutorials.md §4.6): a door of the game or a book of research has just
    // opened, and for a moment nothing else is happening — a dark veil over the whole game, the thing's icon on a
    // slow golden burst, its name, one paragraph, and a quiet way out. The way out is late on purpose: the prompt
    // comes two seconds after the entrance, and until then a tap does nothing. View only.
    public class UnlockSplash : MonoBehaviour, IPointerClickHandler
    {
        private const float VEIL_SECONDS = 0.25f;
        private const float BURST_SECONDS = 0.6f;
        private const float POP_SECONDS = 0.5f;
        private const float RISE_SECONDS = 0.4f;
        private const float RISE_RPX = 30f;
        private const float ENTRANCE_SECONDS = 0.8f;
        private const float PROMPT_DELAY_SECONDS = 2f;
        private const float BREATHE_SECONDS = 0.9f;
        private const float LONG_RAYS_TURN_SECONDS = 40f;
        private const float SHORT_RAYS_TURN_SECONDS = 60f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private CanvasGroup _burst;
        [SerializeField] private RectTransform _longRays;
        [SerializeField] private RectTransform _shortRays;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_Text _prompt;

        private Sequence _entrance;
        private Tween _breathe;
        private bool _ready;

        public event Action Tapped;

        public bool IsShown => gameObject.activeSelf;

        private void Awake() => gameObject.SetActive(false);

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _longRays.Rotate(0, 0, -360f / LONG_RAYS_TURN_SECONDS * dt);
            _shortRays.Rotate(0, 0, 360f / SHORT_RAYS_TURN_SECONDS * dt);
        }

        private void OnDestroy()
        {
            _entrance?.Kill();
            _breathe?.Kill();
        }

        public void Show(string title, string text, Sprite icon, string prompt)
        {
            _prompt.text = prompt;
            _title.text = title;
            _text.text = text;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _ready = false;
            gameObject.SetActive(true);

            _entrance?.Kill();
            _breathe?.Kill();
            _group.alpha = 0;
            _burst.alpha = 0;
            _icon.rectTransform.localScale = Vector3.one * 0.4f;
            var iconColour = _icon.color;
            _icon.color = new Color(iconColour.r, iconColour.g, iconColour.b, 0);
            _prompt.alpha = 0;
            var title0 = Rise(_title);
            var text0 = Rise(_text);

            _entrance = DOTween.Sequence().SetUpdate(true)
                .Join(_group.DOFade(1, VEIL_SECONDS))
                .Insert(0.1f, _burst.DOFade(1, BURST_SECONDS).SetEase(Ease.OutQuad))
                .Insert(0.1f, _icon.rectTransform.DOScale(1, POP_SECONDS).SetEase(Ease.OutBack, 1.56f))
                .Insert(0.1f, _icon.DOFade(1, POP_SECONDS * 0.6f))
                .Insert(0.35f, _title.rectTransform.DOAnchorPosY(title0, RISE_SECONDS).SetEase(Ease.OutQuad))
                .Insert(0.35f, _title.DOFade(1, RISE_SECONDS))
                .Insert(0.45f, _text.rectTransform.DOAnchorPosY(text0, RISE_SECONDS).SetEase(Ease.OutQuad))
                .Insert(0.45f, _text.DOFade(1, RISE_SECONDS))
                .InsertCallback(ENTRANCE_SECONDS + PROMPT_DELAY_SECONDS, BeReady);
        }

        public void Hide()
        {
            _entrance?.Kill();
            _breathe?.Kill();
            _ready = false;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_ready) return;
            _ready = false;
            Tapped?.Invoke();
        }

        // The way out appears, quietly breathing.
        private void BeReady()
        {
            _ready = true;
            _prompt.alpha = 0.8f;
            _breathe = DOTween.To(() => _prompt.alpha, a => _prompt.alpha = a, 0.4f, BREATHE_SECONDS)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        // Starts a line of words a little low and clear; returns where it rests.
        private static float Rise(TMP_Text words)
        {
            var rect = words.rectTransform;
            var rest = rect.anchoredPosition.y;
            words.alpha = 0;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rest - RISE_RPX);
            return rest;
        }
    }
}
