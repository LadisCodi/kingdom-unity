using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Battles
{
    // A slot on the playback's board (the web's bs-slot): a ring carved in the wood round the squad's bust — gold for a
    // hero — with a lacquer inlay that empties round the dial as it is hurt (leaf for ours, clay for theirs, what a
    // blow just took pale for a beat), its count on a pill, its rank's coin. Hit, it flashes; wiped, the ring cracks,
    // goes grey and takes the skull. The presenter moves it; its Body takes the lunges and flinches.
    public class BattleSlotView : MonoBehaviour
    {
        private const float GHOST_DELAY = 0.24f;
        private const float GHOST_SECONDS = 0.42f;
        private static readonly Color LEAF = new(0.435f, 0.749f, 0.290f);
        private static readonly Color CLAY = new(0.831f, 0.333f, 0.243f);
        private static readonly Color WOOD = new(0.663f, 0.443f, 0.247f);
        private static readonly Color WOOD_DARK = new(0.361f, 0.227f, 0.118f);
        private static readonly Color GOLD = new(0.949f, 0.698f, 0.200f);
        private static readonly Color GOLD_DARK = new(0.788f, 0.541f, 0.086f);
        private static readonly Color SHADE = new(0.886f, 0.800f, 0.627f);
        private static readonly Color PARCHMENT = new(0.957f, 0.894f, 0.757f);

        [SerializeField] private RectTransform _body;
        [SerializeField] private Image _rim;
        [SerializeField] private Image _ring;
        [SerializeField] private Image _face;
        [SerializeField] private Image _ghost;
        [SerializeField] private Image _life;
        [SerializeField] private Image _aura;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameObject _countPill;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GameObject _rankCoin;
        [SerializeField] private TMP_Text _rankNumeral;
        [SerializeField] private RectTransform _skull;
        [SerializeField] private CrackGraphic _crack;
        [SerializeField] private Material _drained;

        private Tween _walk;
        private Tween _ghostTween;
        private Tween _crackTween;
        private Tween _flash;
        private float _share = 1;

        public RectTransform Body => _body;
        public bool IsDead { get; private set; }

        public void Show(Sprite bust, Vector2 shift, float scale, bool ours, bool hero, string count, int rank, bool back)
        {
            _portrait.sprite = bust;
            var rect = _portrait.rectTransform;
            var side = ((RectTransform)_portrait.transform.parent).rect.width;
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(shift.x * side / 256f, -0.08f * side - shift.y * side / 256f);
            _life.color = ours ? LEAF : CLAY;
            _ring.color = hero ? GOLD : WOOD;
            _rim.color = hero ? GOLD_DARK : WOOD_DARK;
            _face.color = hero ? PARCHMENT : SHADE;
            _countPill.SetActive(!string.IsNullOrEmpty(count));
            _count.text = count;
            _rankCoin.SetActive(rank >= 2);
            if (rank >= 2) _rankNumeral.text = Kingdom.Army.Troops.Roman(rank);
            _skull.gameObject.SetActive(false);
            _crack.gameObject.SetActive(false);
            _aura.color = Color.clear;
            transform.localScale = Vector3.one * (back ? 0.88f : 1f);
            SetLife(1, true);
        }

        // Walking: the bust bobs while the slot crosses the field.
        public void SetWalking(bool walking)
        {
            if (walking == (_walk != null)) return;
            _walk?.Kill();
            _walk = null;
            var bust = _portrait.transform.parent;
            bust.localRotation = Quaternion.identity;
            bust.localPosition = Vector3.zero;
            if (!walking || IsDead) return;
            bust.localRotation = Quaternion.Euler(0, 0, 4);
            _walk = DOTween.Sequence().SetUpdate(true).SetLoops(-1, LoopType.Yoyo)
                .Append(bust.DOLocalRotate(new Vector3(0, 0, -4), 0.26f).SetEase(Ease.InOutSine))
                .Join(bust.DOLocalMoveY(4 * 2.8f, 0.26f).SetEase(Ease.InOutSine));
        }

        public void SetCount(string count)
        {
            _countPill.SetActive(!string.IsNullOrEmpty(count));
            _count.text = count;
        }

        // A count dropping: it swells red for a beat.
        public void PunchCount()
        {
            _count.transform.DOKill(true);
            _count.transform.DOPunchScale(Vector3.one * 0.4f, 0.28f, 1, 0).SetUpdate(true);
        }

        // The inlay at `share` of what it walked in with; the ghost catches up after a beat.
        public void SetLife(float share, bool instant = false)
        {
            _share = Mathf.Clamp01(share);
            _life.fillAmount = _share;
            _ghostTween?.Kill();
            if (instant) _ghost.fillAmount = _share;
            else _ghostTween = _ghost.DOFillAmount(_share, GHOST_SECONDS).SetDelay(GHOST_DELAY).SetEase(Ease.InQuad).SetUpdate(true);
        }

        // Hurt: a white frame for two frames.
        public void Flash()
        {
            _flash?.Kill(true);
            _face.color = PARCHMENT * 2.4f;
            _flash = DOVirtual.DelayedCall(0.09f, () => _face.color = SHADE, true);
        }

        public void Glow(Color tint, float seconds)
        {
            _aura.DOKill();
            _aura.color = new Color(tint.r, tint.g, tint.b, 0);
            _aura.transform.localScale = Vector3.one * 0.6f;
            DOTween.Sequence().SetUpdate(true)
                .Append(_aura.DOFade(1, seconds * 0.7f)).Join(_aura.transform.DOScale(1.25f, seconds * 0.7f))
                .Append(_aura.DOFade(0, seconds * 0.3f)).Join(_aura.transform.DOScale(1.45f, seconds * 0.3f));
        }

        // Wiped: the ring cracks, goes grey and takes the skull, stamped a beat later.
        public void Die(int seed, bool quiet)
        {
            IsDead = true;
            SetWalking(false);
            SetLife(0, true);
            SetCount(string.Empty);
            _portrait.material = _drained;
            _face.material = _drained;
            _ring.material = _drained;
            _portrait.color = new Color(1, 1, 1, 0.55f);
            _crack.gameObject.SetActive(true);
            _crack.Break(seed);
            _crackTween?.Kill();
            if (quiet) _crack.Progress = 1;
            else _crackTween = DOTween.To(() => _crack.Progress, p => _crack.Progress = p, 1, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            _skull.gameObject.SetActive(true);
            _skull.localScale = quiet ? Vector3.one * 0.7f : Vector3.one * 2.2f;
            if (!quiet) _skull.DOScale(0.7f, 0.24f).SetDelay(0.14f).SetEase(Ease.InCubic).SetUpdate(true);
            transform.SetAsFirstSibling();
        }

        private void OnDestroy()
        {
            _walk?.Kill();
            _ghostTween?.Kill();
            _crackTween?.Kill();
            _flash?.Kill();
            _aura.DOKill();
            _skull.DOKill();
            _count.transform.DOKill();
            _body.DOKill();
        }
    }
}
