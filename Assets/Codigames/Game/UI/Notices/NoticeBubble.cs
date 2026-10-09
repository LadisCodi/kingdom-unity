using System;
using Codigames.Game.UI.Kit;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Notices
{
    // A notice's bubble (Docs/features/26-notices.md §4, the web's nt-bubble): a round wooden medallion with a brass rim,
    // the picture set into its face, a red wax seal with a group's count. It pops in when it arrives, and blinks while
    // a news left unread is about to go.
    public class NoticeBubble : MonoBehaviour
    {
        private const float POP_SECONDS = 0.38f;
        private const float BLINK_SECONDS = 0.35f;

        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _body;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _carved;
        [SerializeField] private CtaBadge _seal;
        [SerializeField, Tooltip("A threat's face: a dark red disc, the creature on it.")] private GameObject _threat;
        [SerializeField, Tooltip("A countdown's nailed plaque, hung under the bubble.")] private GameObject _clock;
        [SerializeField] private TMP_Text _clockText;

        private Tween _blink;

        public event Action<string> Tapped;

        public string Id { get; private set; }

        public void Show(Notice notice)
        {
            Id = notice.Id;
            NoticeArt.Fit(_art, notice.Art, notice.ArtIsBuilding);
            _carved.gameObject.SetActive(!string.IsNullOrEmpty(notice.Carved));
            _carved.text = notice.Carved;
            _seal.Show(notice.Count > 1 ? notice.Count : 0);
            _threat.SetActive(notice.Threat);
            if (notice.Threat)
            {
                // The creature larger than the face, sunk into it: the web's 118%, sunk a fifth.
                var face = ((RectTransform)_art.transform.parent).rect.width;
                _art.rectTransform.localScale = Vector3.one * 1.18f;
                _art.rectTransform.anchoredPosition = new Vector2(0, -0.2f * face);
            }
            else
            {
                _art.rectTransform.localScale = Vector3.one;
            }

            _clock.SetActive(notice.Until.HasValue);
        }

        public void SetClock(string text) => _clockText.text = text;

        // A bubble that just arrived pops in.
        public void Pop()
        {
            _body.DOKill();
            _body.localScale = Vector3.one * 0.2f;
            _body.DOScale(1, POP_SECONDS).SetEase(Ease.OutBack, 2.2f).SetUpdate(true);
        }

        // A news about to go on its own fades in and out.
        public void SetLeaving(bool leaving)
        {
            if (leaving == (_blink != null)) return;
            _blink?.Kill();
            _blink = null;
            _group.alpha = 1;
            if (leaving) _blink = _group.DOFade(0.25f, BLINK_SECONDS).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void OnEnable() => _button.onClick.AddListener(OnTapped);

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnTapped);
            SetLeaving(false);
            _body.DOKill();
            _body.localScale = Vector3.one;
        }

        private void OnTapped() => Tapped?.Invoke(Id);
    }
}
