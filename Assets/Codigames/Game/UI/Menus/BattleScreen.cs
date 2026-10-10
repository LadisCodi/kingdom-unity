using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Kit;
using DG.Tweening;
using TMPro;
using Codigames.Game.UI.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The fight, played back (Docs/features/combat.md §13): the wooden bar with what each side is still worth, the
    // place's plaque, the speed and Skip knobs, the field with every slot where the log has it, the effects layer and
    // the numbers over it, and at the end the verdict's plaque and the way out. View only: the BattleScreenPresenter
    // walks the log and says what happens; this draws it.
    public class BattleScreen : Menu
    {
        // One design pixel (the web's --px) in canvas units; a slot is 66 of them.
        public const float PX = 2.8f;
        private const float MERGE_SECONDS = 0.26f;
        private const int MAX_FLOATS = 6;
        private const float FLOAT_SECONDS = 0.9f;
        private static readonly Vector2[] FLOAT_SPREAD = { new(0, 0), new(-20, 10), new(20, -8), new(-12, -14), new(12, 14) };

        [SerializeField] private RectTransform _bar;
        [SerializeField] private RectTransform _barFill;
        [SerializeField] private TMP_Text _mine;
        [SerializeField] private TMP_Text _theirs;
        [SerializeField] private RectTransform _swords;
        [SerializeField] private RectTransform _where;
        [SerializeField] private TMP_Text _whereTitle;
        [SerializeField] private TMP_Text _whereLine;
        [SerializeField] private CanvasGroup _knobs;
        [SerializeField] private KitButton _speed;
        [SerializeField] private KitButton _skip;
        [SerializeField] private RectTransform _board;
        [SerializeField] private RectTransform _gap;
        [SerializeField] private BattleSlotView _slotPrefab;
        [SerializeField] private BattleFx _fx;
        [SerializeField] private RectTransform _floats;
        [SerializeField] private TMP_Text _floatPrefab;
        [SerializeField] private Image _flash;
        [SerializeField] private Image _veil;
        [SerializeField] private RectTransform _plaque;
        [SerializeField] private Image _plaqueBorder;
        [SerializeField] private TMP_Text _plaqueWord;
        [SerializeField] private CrackGraphic _plaqueCrack;
        [SerializeField] private KitButton _exit;
        // A skill's name on dyed cloth across the line between the armies.
        [SerializeField] private RectTransform _ribbon;
        [SerializeField] private Image _ribbonCloth;
        [SerializeField] private TMP_Text _ribbonText;
        [SerializeField] private Color _won = new(0.949f, 0.698f, 0.2f);
        [SerializeField] private Color _lost = new(0.831f, 0.333f, 0.243f);

        private readonly List<BattleSlotView> _slots = new();
        private readonly List<(TMP_Text Text, BattleSlotView Slot, string Kind, int Total, float At, Tween Life)> _live = new();
        private readonly Dictionary<BattleSlotView, int> _spread = new();
        private Rect _span;
        private float _scale = 1;
        private Vector2 _measured;


        // The way out, kept clear of any tutorial line that points nowhere on this screen.
        protected override void InitializeInternal() => CoachTarget.Tag(_exit, "battle-leave");

        public event Action SpeedTapped;
        public event Action SkipTapped;
        public event Action ExitTapped;

        public BattleFx Fx => _fx;

        // How a number is written: the presenter's, in the player's locale.
        public Func<int, string> Format { get; set; } = n => n.ToString();

        // Lays the board out afresh: one slot each, at the place the fight opened with them, the field fitted to the board.
        public IReadOnlyList<BattleSlotView> Build(IReadOnlyList<(Sprite Bust, Vector2 Shift, float Scale, bool Ours, bool Hero, string Count, int Rank, bool Back, Vector2 Home)> slots,
            string title, string line)
        {
            foreach (var slot in _slots) Destroy(slot.gameObject);
            _slots.Clear();
            foreach (var f in _live) Destroy(f.Text.gameObject);
            _live.Clear();
            _spread.Clear();
            _fx.Clear();
            _fx.Unit = PX;
            _whereTitle.text = title;
            _whereLine.text = line;
            _plaque.gameObject.SetActive(false);
            _exit.gameObject.SetActive(false);
            _knobs.alpha = 1;
            _knobs.blocksRaycasts = true;
            _veil.color = Color.clear;
            _flash.color = new Color(1, 0.973f, 0.902f, 0);

            var homes = slots.Select(s => s.Home).ToList();
            const float HALF = 50;
            _span = homes.Count == 0 ? new Rect(-100, -100, 200, 200) : Rect.MinMaxRect(homes.Min(h => h.x) - HALF, homes.Min(h => h.y) - HALF,
                homes.Max(h => h.x) + HALF, homes.Max(h => h.y) + HALF);
            _measured = Vector2.zero;
            Measure();

            foreach (var s in slots)
            {
                var view = Instantiate(_slotPrefab, _board);
                view.Show(s.Bust, s.Shift, s.Scale, s.Ours, s.Hero, s.Count, s.Rank, s.Back);
                ((RectTransform)view.transform).anchoredPosition = OnBoard(s.Home);
                _slots.Add(view);
            }

            return _slots;
        }

        // A field point, on the board: x across, y flipped (the field's y runs down towards the attacker).
        public Vector2 OnBoard(Vector2 field)
        {
            Measure();
            return new Vector2((field.x - _span.center.x) * _scale, -(field.y - _span.center.y) * _scale);
        }

        // The field fitted to the board — one scale both ways, never larger than a slot's pitch — again whenever the board's
        // size changes (it is laid out after it is filled).
        private void Measure()
        {
            var size = _board.rect.size;
            if (size == _measured || size.x <= 0 || size.y <= 0) return;
            _measured = size;
            _scale = Mathf.Min(size.x / _span.width, size.y / _span.height, 76 * PX / 100f);
            _gap.anchoredPosition = new Vector2(0, -(0 - _span.center.y) * _scale);
        }

        // The same point in the effects layer's and the numbers' space.
        public Vector2 OnFx(Vector2 field) => (Vector2)_fx.rectTransform.InverseTransformPoint(_board.TransformPoint(OnBoard(field)));

        public float Scale => _scale;

        public void Place(BattleSlotView slot, Vector2 field) => ((RectTransform)slot.transform).anchoredPosition = OnBoard(field);

        // The armies marching on: each side's rows slide in from its own edge, the front first; the swords clash on the bar
        // and the place's plaque swings down.
        public void March(IReadOnlyList<(BattleSlotView Slot, bool Ours, int Line)> slots)
        {
            foreach (var (slot, ours, line) in slots)
            {
                var body = slot.Body;
                var group = slot.GetComponent<CanvasGroup>();
                body.anchoredPosition = new Vector2(0, (ours ? -1 : 1) * 60 * PX);
                body.DOAnchorPos(Vector2.zero, 0.42f).SetDelay(line * 0.09f).SetEase(Ease.OutBack, 1.6f).SetUpdate(true);
                if (group == null) continue;
                group.alpha = 0;
                group.DOFade(1, 0.42f).SetDelay(line * 0.09f).SetUpdate(true);
            }

            _swords.localScale = Vector3.one * 2.6f;
            _swords.localRotation = Quaternion.Euler(0, 0, 30);
            _swords.DOScale(1, 0.38f).SetDelay(0.42f).SetEase(Ease.OutBack, 1.6f).SetUpdate(true);
            _swords.DOLocalRotate(Vector3.zero, 0.38f).SetDelay(0.42f).SetEase(Ease.OutBack, 1.6f).SetUpdate(true);
            _where.anchoredPosition += new Vector2(0, 40 * PX);
            _where.DOAnchorPosY(_where.anchoredPosition.y - 40 * PX, 0.52f).SetDelay(0.12f).SetEase(Ease.OutBack, 1.4f).SetUpdate(true);
            _knobs.alpha = 0;
            _knobs.DOFade(1, 0.3f).SetDelay(0.5f).SetUpdate(true);
        }

        public void SetBarFill(float share) => _barFill.anchorMax = new Vector2(Mathf.Clamp01(share), _barFill.anchorMax.y);

        public void SetBarText(string mine, string theirs)
        {
            _mine.text = mine;
            _theirs.text = theirs;
        }

        public void ShakeBar(bool ours)
        {
            _bar.DOKill(true);
            _bar.DOPunchAnchorPos(new Vector2(3 * PX, PX), 0.22f, 8, 0.5f).SetUpdate(true);
            var number = ours ? _mine : _theirs;
            number.transform.DOKill(true);
            number.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 1, 0).SetUpdate(true);
        }

        public void ShakeBoard()
        {
            _board.DOKill(true);
            _board.DOPunchAnchorPos(new Vector2(5 * PX, 3 * PX), 0.26f, 8, 0.6f).SetUpdate(true);
        }

        public void SetSpeed(string label) => _speed.Label = label;

        public void SetSkip(string label) => _skip.Label = label;

        public void HideKnobs()
        {
            _knobs.alpha = 0;
            _knobs.blocksRaycasts = false;
        }

        // A number off a slot — or onto the one already rising off it, if that one is fresh and of the same kind.
        public void Float(BattleSlotView slot, Vector2 at, string kind, int amount, Color color, float size)
        {
            var now = Time.unscaledTime;
            var i = _live.FindIndex(f => f.Slot == slot && f.Kind == kind);
            if (i >= 0 && now - _live[i].At < MERGE_SECONDS)
            {
                var (label, s, k, total, _, life) = _live[i];
                total += amount;
                label.text = Words(kind, total);
                life.Kill();
                _live[i] = (label, s, k, total, now, Animate(label));
                return;
            }

            _spread.TryGetValue(slot, out var turn);
            _spread[slot] = turn + 1;
            var node = Instantiate(_floatPrefab, _floats);
            node.gameObject.SetActive(true);
            node.text = Words(kind, amount);
            node.color = color;
            node.fontSize = size;
            var spread = FLOAT_SPREAD[turn % FLOAT_SPREAD.Length];
            node.rectTransform.anchoredPosition = at + new Vector2(spread.x, -(spread.y - 6)) * PX;
            _live.Add((node, slot, kind, amount, now, Animate(node)));
            while (_live.Count > MAX_FLOATS) Drop(0);
        }

        // The verdict comes down: carved gold for a win; a defeat lands cracked, tilted, and the field goes grey under it.
        // THE RIBBON: one strip of cloth across the line between the armies, naming the skill; a new one replaces it.
        // It unrolls, holds, and drifts away toward the caster's side.
        public void Ribbon(string name, Color cloth, bool ours)
        {
            if (_ribbon == null) return;
            _ribbon.DOKill();
            _ribbonCloth.DOKill();
            _ribbonText.DOKill();
            _ribbon.gameObject.SetActive(true);
            _ribbon.SetAsLastSibling();
            _ribbonText.text = name;
            _ribbonCloth.color = cloth;
            _ribbonText.alpha = 1;
            var gapY = _gap != null ? ((RectTransform)_ribbon.parent).InverseTransformPoint(_gap.position).y : 0;
            _ribbon.anchoredPosition = new Vector2(0, gapY);
            _ribbon.localScale = new Vector3(0, 1, 1);
            var drift = (ours ? -14 : 14) * PX;
            DOTween.Sequence().SetTarget(_ribbon).SetUpdate(true)
                .Append(_ribbon.DOScaleX(1.08f, 0.21f).SetEase(Ease.OutQuad))
                .Append(_ribbon.DOScaleX(1, 0.1f))
                .AppendInterval(0.7f)
                .Append(_ribbon.DOAnchorPosY(gapY + drift, 0.29f))
                .Join(_ribbonCloth.DOFade(0, 0.29f))
                .Join(_ribbonText.DOFade(0, 0.29f))
                .OnComplete(() => _ribbon.gameObject.SetActive(false));
        }

        public void ShowPlaque(bool won, string word)
        {
            _plaque.gameObject.SetActive(true);
            _plaqueWord.text = word;
            _plaqueWord.color = won ? _won : new Color(0.957f, 0.894f, 0.757f);
            _plaqueBorder.color = won ? _won : _lost;
            _plaqueCrack.gameObject.SetActive(!won);
            if (!won)
            {
                _plaqueCrack.Break(7);
                _plaqueCrack.Progress = 1;
                _veil.DOColor(new Color(0.15f, 0.12f, 0.1f, 0.45f), 0.7f).SetUpdate(true);
            }

            _plaque.localRotation = Quaternion.Euler(0, 0, won ? 0 : 3);
            _plaque.localScale = Vector3.one * (won ? 2f : 1.5f);
            _plaque.DOScale(1, won ? 0.52f : 0.38f).SetEase(won ? Ease.OutBack : Ease.InCubic, 1.4f).SetUpdate(true);
        }

        public void ShowExit(string label)
        {
            _exit.Label = label;
            _exit.gameObject.SetActive(true);
        }

        // The white of the last blow.
        public void FlashWhite()
        {
            _flash.DOKill();
            _flash.color = new Color(1, 0.973f, 0.902f, 0.55f);
            _flash.DOFade(0, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        protected override void SubscribeToEventsInternal()
        {
            _speed.onClick.AddListener(OnSpeed);
            _skip.onClick.AddListener(OnSkip);
            _exit.onClick.AddListener(OnExit);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _speed.onClick.RemoveListener(OnSpeed);
            _skip.onClick.RemoveListener(OnSkip);
            _exit.onClick.RemoveListener(OnExit);
        }

        private Tween Animate(TMP_Text node)
        {
            node.transform.DOKill();
            node.alpha = 0;
            node.transform.localScale = Vector3.one * 1.7f;
            var start = node.rectTransform.anchoredPosition;
            return DOTween.Sequence().SetUpdate(true)
                .Append(node.DOFade(1, FLOAT_SECONDS * 0.12f)).Join(node.transform.DOScale(1, FLOAT_SECONDS * 0.12f))
                .Append(node.rectTransform.DOAnchorPosY(start.y + 18 * PX, FLOAT_SECONDS * 0.53f))
                .Append(node.rectTransform.DOAnchorPosY(start.y + 30 * PX, FLOAT_SECONDS * 0.35f)).Join(node.DOFade(0, FLOAT_SECONDS * 0.35f))
                .OnComplete(() => Drop(_live.FindIndex(f => f.Text == node)));
        }

        private void Drop(int index)
        {
            if (index < 0 || index >= _live.Count) return;
            var f = _live[index];
            f.Life?.Kill();
            if (f.Text != null) Destroy(f.Text.gameObject);
            _live.RemoveAt(index);
        }

        private string Words(string kind, int n) => kind switch
        {
            "heal" => "+" + Format(n),
            "shield" => "(" + Format(n) + ")",
            _ => "−" + Format(n),
        };

        private void OnSpeed() => SpeedTapped?.Invoke();
        private void OnSkip() => SkipTapped?.Invoke();
        private void OnExit() => ExitTapped?.Invoke();
    }
}
