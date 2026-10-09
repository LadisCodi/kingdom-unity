using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.UI.Effects;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Reveal;
using Codigames.Kingdom.Heroes;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // THE REVEAL (the web's gachaScreen, Docs/features/10-heroes.md §8.3): a chest of prizes opened one card at a time,
    // on a stage of its own above everything. The chest drops onto the carpet and opens on its own; a card rises face
    // down, a tap flips it, the next sends it to its place — smaller, dimmed — while the next rises. When the last
    // lands they all light up as the summary, and Collect is the way out. A whole new hero is celebrated: the back
    // glows and trembles, and the flip darkens the room, flashes, shakes, raises rays, fires confetti and fireworks.
    // A ten-call is dealt in beats: its goods together face up, then the bag, then each new hero. Skip deals the rest
    // at once and still stops at every new hero. Presentation only: everything was paid before it opened.
    public class RevealScreen : Menu, IPointerClickHandler
    {
        private const float U = 2.8f;
        private const float CARD_W = 300;
        private const float CARD_H = 450;
        private const float CHEST = 644;

        private static readonly Color[] GOLD = { Hex(0xffe9a8), Hex(0xf2b233), Hex(0xffd36b), Hex(0xfff6da) };
        private static readonly Color[] CONFETTI = { Hex(0xf2b233), Hex(0xffd36b), Hex(0xd4553e), Hex(0x4fa3c7), Hex(0x6fbf4a), Hex(0xfff6e0) };
        private static readonly Color[] DUST = { new(214 / 255f, 186 / 255f, 140 / 255f, 0.8f) };

        [SerializeField] private RectTransform _safe;
        [SerializeField] private RectTransform _cards;
        [SerializeField] private RevealCardView _cardPrefab;
        [SerializeField] private RectTransform _chest;
        [SerializeField] private Image _chestArt;
        [SerializeField] private CanvasGroup _chestGroup;
        [SerializeField] private Image _bloom;
        [SerializeField] private TMP_Text _left;
        [SerializeField] private Image _veil;
        [SerializeField] private RectTransform _rays;
        [SerializeField] private Image _raysImage;
        [SerializeField] private CanvasGroup _kicker;
        [SerializeField] private TMP_Text _kickerText;
        [SerializeField] private CanvasGroup _heroLine;
        [SerializeField] private TMP_Text _heroName;
        [SerializeField] private TMP_Text _heroTitle;
        [SerializeField] private TMP_Text _heroRarity;
        [SerializeField] private TMP_Text _prompt;
        [SerializeField] private CanvasGroup _title;
        [SerializeField] private TMP_Text _plaque;
        [SerializeField] private TMP_Text _calls;
        [SerializeField] private CanvasGroup _foot;
        [SerializeField] private KitButton _collect;
        [SerializeField] private Button _skip;
        [SerializeField] private CanvasGroup _skipGroup;
        [SerializeField] private Image _flash;
        [SerializeField] private ParticleLayer _fx;
        [SerializeField] private RectTransform _shake;
        [SerializeField] private Sprite[] _closed = new Sprite[4];
        [SerializeField] private Sprite[] _open = new Sprite[4];

        private enum Phase
        {
            Landing,
            Busy,
            Down,
            Up,
            Done,
        }

        private readonly List<RevealCardView> _views = new();
        private readonly List<Vector2> _home = new();
        private readonly List<float> _homeScale = new();
        private readonly List<List<int>> _deals = new();
        private readonly List<(RevealCardView Card, Vector2 At, float Scale)> _crowd = new();

        private RevealScreenData _data;
        private Phase _phase;
        private bool _skipping;
        private int _nextDeal;
        private int _dealt;
        private int _current = -1;
        private Vector2 _centre;
        private float _centreScale;
        private float _holdUntil;
        private Tween _breathe;
        private Tween _spin;
        private Tween _tremble;
        private Coroutine _run;
        private readonly List<Tween> _party = new();
        private Vector2 _chestRest;
        private Vector2 _kickerRest;

        // How a count is written (the presenter's NumberFormat).
        public Func<int, string> Count { get; set; } = n => n.ToString();

        public event Action CollectTapped;

        protected override void InitializeInternal()
        {
            _collect.onClick.AddListener(() => CollectTapped?.Invoke());
            _skip.onClick.AddListener(OnSkip);
            _chestRest = _chest.anchoredPosition;
            _kickerRest = ((RectTransform)_kicker.transform).anchoredPosition;
        }

        private float Pace => _skipping ? 0.35f : 1;
        private float Width => ((RectTransform)transform).rect.width;
        private float Height => ((RectTransform)transform).rect.height;

        public void Play(RevealScreenData data)
        {
            Stop();
            _data = data;
            _skipping = false;
            _nextDeal = 0;
            _dealt = 0;
            _current = -1;
            _crowd.Clear();
            _chestArt.sprite = _closed[(int)data.Chest];
            _chestGroup.alpha = 0;
            _chest.anchoredPosition = _chestRest;
            _chest.localScale = Vector3.one;
            _chestArt.rectTransform.localRotation = Quaternion.identity;
            _chestArt.rectTransform.localScale = Vector3.one;
            _shake.anchoredPosition = Vector2.zero;
            ((RectTransform)_kicker.transform).anchoredPosition = _kickerRest;
            _bloom.color = new Color(1, 1, 1, 0);
            _veil.color = new Color(_veil.color.r, _veil.color.g, _veil.color.b, 0);
            _raysImage.color = new Color(1, 1, 1, 0);
            _kicker.alpha = 0;
            _heroLine.alpha = 0;
            _title.alpha = 0;
            _foot.alpha = 0;
            _foot.blocksRaycasts = false;
            _flash.color = new Color(1, 1, 1, 0);
            _calls.text = data.Caption;
            Say(string.Empty);
            _fx.Clear();

            for (var i = 0; i < data.Cards.Count; i++)
            {
                if (i == _views.Count) _views.Add(Instantiate(_cardPrefab, _cards));
                _views[i].gameObject.SetActive(true);
                _views[i].Show(data.Cards[i]);
                Hide(_views[i]);
            }

            for (var i = data.Cards.Count; i < _views.Count; i++) _views[i].gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            Layout();
            Deals();
            Left(data.Cards.Count);
            SyncSkip();
            _phase = Phase.Landing;
            _run = StartCoroutine(Land());
        }

        public void Stop()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
            DOTween.Kill(this);
            _breathe?.Kill();
            _spin?.Kill();
            _tremble?.Kill();
            EndParty();
            if (_fx != null) _fx.Clear();
        }

        private void OnDisable() => Stop();

        // ---- the board: every card's place from the start, so the summary never reflows

        private void Layout()
        {
            var cards = _data.Cards;
            var bagged = cards.Any(c => c.Kind == PrizeKind.Bag);
            (int Cols, float W) shape;
            if (bagged)
            {
                var others = cards.Where(c => c.Kind != PrizeKind.Bag).ToList();
                var n = Math.Max(others.Count(c => c.Kind != PrizeKind.Hero), others.Count(c => c.Kind == PrizeKind.Hero));
                var cols = Math.Min(5, Math.Max(3, n));
                shape = (cols, cols == 3 ? 100 : cols == 4 ? 82 : 66);
            }
            else shape = cards.Count <= 3 ? (Math.Max(1, cards.Count), 104) : cards.Count <= 6 ? (3, 100) : cards.Count <= 12 ? (4, 82) : (5, 66);

            var cw = shape.W * U;
            var k = cw / CARD_W;
            var gap = 10 * U;
            var rowGap = 12 * U + cw * 0.2f;
            var boardW = shape.Cols * cw + (shape.Cols - 1) * gap;
            var top = -(TopInset + 92 * U);
            _home.Clear();
            _homeScale.Clear();
            int col = 0;
            var y = top;
            var rowH = 0f;
            foreach (var card in cards)
            {
                var view = _views[_home.Count];
                if (card.Kind == PrizeKind.Bag)
                {
                    if (col > 0)
                    {
                        y -= rowH + rowGap;
                        col = 0;
                    }

                    var rows = Mathf.CeilToInt(card.Rows.Count / 2f);
                    var h = CARD_W * (0.42f + 0.5f * rows);
                    view.Rect.sizeDelta = new Vector2(boardW / k, h);
                    view.FitBag();
                    _home.Add(new Vector2(0, y - h * k / 2));
                    _homeScale.Add(k);
                    y -= h * k + rowGap;
                    rowH = 0;
                    continue;
                }

                view.Rect.sizeDelta = new Vector2(CARD_W, CARD_H);
                var x = -boardW / 2 + cw / 2 + col * (cw + gap);
                _home.Add(new Vector2(x, y - cw * 1.5f / 2));
                _homeScale.Add(k);
                rowH = cw * 1.5f;
                col++;
                if (col == shape.Cols)
                {
                    col = 0;
                    y -= rowH + rowGap;
                    rowH = 0;
                }
            }

            _centre = new Vector2(0, -Height * 0.4f);
            _centreScale = Mathf.Min(Width * 0.54f, Height * 0.38f / 1.5f) / CARD_W;
        }

        private float TopInset => _safe != null ? -_safe.offsetMax.y : 0;
        private float BottomInset => _safe != null ? _safe.offsetMin.y : 0;

        // One card at a time — or a ten-call's goods together, then the bag, then each hero.
        private void Deals()
        {
            _deals.Clear();
            var cards = _data.Cards;
            if (cards.Any(c => c.Kind is PrizeKind.Bag or PrizeKind.Supplies))
            {
                var goods = Enumerable.Range(0, cards.Count).Where(i => cards[i].Kind is not (PrizeKind.Bag or PrizeKind.Hero)).ToList();
                if (goods.Count > 0) _deals.Add(goods);
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].Kind is PrizeKind.Bag or PrizeKind.Hero) _deals.Add(new List<int> { i });
            }
            else
            {
                for (var i = 0; i < cards.Count; i++) _deals.Add(new List<int> { i });
            }
        }

        // Where the chest's mouth is, in the cards' space.
        private Vector2 Mouth => new(0, -(Height - BottomInset - 64 * U - CHEST) - CHEST * 0.45f);

        // ---- pacing: every tween is this screen's, so a tap can finish them all at once

        private Tween Own(Tween t) => t.SetId(this).SetUpdate(true);

        private IEnumerator Wait(float ms)
        {
            if (ms * Pace <= 0) yield break;
            yield return Own(DOVirtual.DelayedCall(ms * Pace / 1000, () => { })).WaitForCompletion();
        }

        private IEnumerator Run(Tween t)
        {
            Own(t);
            yield return t.WaitForCompletion();
        }

        private float Ms(float ms) => Mathf.Max(0.001f, ms * Pace / 1000);

        private void Hurry() => DOTween.Complete(this, true);

        private void Sfx(string id, float volume = 1)
        {
            if (_skipping && id == "cardWhoosh") return;
            Sounds?.Play(id, volume);
        }

        private void Say(string text)
        {
            _prompt.text = text;
            _prompt.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private void SyncSkip()
        {
            var on = _phase != Phase.Landing && _phase != Phase.Done && !_skipping && _data != null && _data.Cards.Count - _dealt > 1;
            _skipGroup.alpha = on ? 1 : 0;
            _skipGroup.blocksRaycasts = on;
        }

        private void Left(int n)
        {
            _left.text = Count(n);
            _left.alpha = n == 0 ? 0.45f : 1;
        }

        // A point of a card or the chest, in the particle layer's web-style coordinates (y down from the top-left).
        private Vector2 Fx(RectTransform rect, float fx = 0.5f, float fy = 0.5f)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var world = Vector3.Lerp(Vector3.Lerp(corners[1], corners[2], fx), Vector3.Lerp(corners[0], corners[3], fx), fy);
            var local = (Vector2)_fx.rectTransform.InverseTransformPoint(world);
            var r = _fx.rectTransform.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        private void Burst(Vector2 at, ParticleKind kind, int count, Color[] colors, float speed, float size, float life, float? angle = null,
            float spread = 0, float gravity = 0, float radius = 0)
            => _fx.Emit(at, new Burst
            {
                Kind = kind, Count = count, Colors = colors, Speed = speed, Size = size, Life = life, Angle = angle, Spread = spread, Gravity = gravity,
                Radius = radius,
            });

        private static void Hide(RevealCardView card) => card.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        private static CanvasGroup Group(RevealCardView card) => card.gameObject.GetComponent<CanvasGroup>();

        // ---- 1. the chest lands, and opens on its own: the player already paid

        private IEnumerator Land()
        {
            _fx.Ambient(true);
            var rest = _chestRest;
            _chest.anchoredPosition = rest + new Vector2(0, CHEST * 1.3f);
            _chest.localScale = Vector3.one * 0.9f;
            var land = DOTween.Sequence()
                .Append(_chest.DOAnchorPos(rest, Ms(450)).SetEase(Ease.InQuad))
                .Join(_chestGroup.DOFade(1, Ms(450)))
                .Join(_chest.DOScale(new Vector3(1.05f, 0.92f, 1), Ms(450)))
                .Append(_chest.DOScale(new Vector3(0.98f, 1.03f, 1), Ms(85)))
                .Append(_chest.DOScale(Vector3.one, Ms(85)));
            yield return Run(land);
            _chest.anchoredPosition = rest;
            Sounds?.Play(SoundIds.CHEST_LAND);
            var at = Fx(_chest, 0.5f, 0.88f);
            var w = _chest.rect.width / U;
            Burst(at - new Vector2(w * 0.3f * U, 0), ParticleKind.Dust, 9, DUST, 90, 14, 900, Mathf.PI, 0.9f);
            Burst(at + new Vector2(w * 0.3f * U, 0), ParticleKind.Dust, 9, DUST, 90, 14, 900, 0, 0.9f);
            yield return Wait(260);
            yield return Open();
        }

        // ---- 2. the latch, the lid flies

        private IEnumerator Open()
        {
            _phase = Phase.Busy;
            Say(string.Empty);
            SyncSkip();
            Sounds?.Play(SoundIds.CHEST_UNLOCK);
            var art = _chestArt.rectTransform;
            var wiggle = DOTween.Sequence()
                .Append(art.DOLocalRotate(new Vector3(0, 0, 3), Ms(90))).Join(art.DOScale(1.03f, Ms(90)))
                .Append(art.DOLocalRotate(new Vector3(0, 0, -3), Ms(90))).Join(art.DOScale(1.05f, Ms(90)))
                .Append(art.DOLocalRotate(new Vector3(0, 0, 2), Ms(90))).Join(art.DOScale(1.07f, Ms(90)))
                .Append(art.DOLocalRotate(Vector3.zero, Ms(90))).Join(art.DOScale(1f, Ms(90)));
            yield return Run(wiggle);
            _chestArt.sprite = _open[(int)_data.Chest];
            Sounds?.Play(SoundIds.CHEST_OPEN);
            _bloom.color = new Color(1, 1, 1, 1);
            _breathe = DOTween.Sequence().Append(_bloom.DOFade(0.45f, 1.2f)).Append(_bloom.DOFade(1, 1.2f)).SetLoops(-1).SetUpdate(true);
            var at = Fx(_chest, 0.5f, 0.42f);
            Burst(at, ParticleKind.Spark, 26, GOLD, 260, 9, 900, -Mathf.PI / 2, 2.2f, 160);
            Burst(at, ParticleKind.Mote, 14, new[] { new Color(1, 0.82f, 0.47f, 0.9f) }, 120, 4, 1300, -Mathf.PI / 2, 1.6f);
            yield return Run(DOTween.Sequence().Append(art.DOScale(new Vector3(1.1f, 0.94f, 1), Ms(120))).Append(art.DOScale(1, Ms(120))));
            yield return Draw();
        }

        // ---- the next card out of the chest, face down — or the next crowd, face up

        private IEnumerator Draw()
        {
            _phase = Phase.Busy;
            var deal = _deals[_nextDeal++];
            _dealt += deal.Count;
            Left(_data.Cards.Count - _dealt);
            if (deal.Count > 1)
            {
                yield return DrawCrowd(deal);
                yield break;
            }

            var i = deal[0];
            var card = _views[i];
            _current = i;
            var data = card.Data;
            card.transform.SetAsLastSibling();
            var bag = data.Kind == PrizeKind.Bag;
            _centreScale = bag
                ? Mathf.Min(Width * 0.94f / card.Rect.sizeDelta.x, Height * 0.5f / card.Rect.sizeDelta.y)
                : Mathf.Min(Width * 0.54f, Height * 0.38f / 1.5f) / CARD_W;
            if (data.Charged)
            {
                card.Glow.gameObject.SetActive(true);
                _tremble = DOTween.Sequence()
                    .Append(card.Inner.DOLocalRotate(new Vector3(0, 0, 2.5f), 0.18f)).Append(card.Inner.DOLocalRotate(new Vector3(0, 0, -2.5f), 0.18f))
                    .Append(card.Inner.DOLocalRotate(new Vector3(0, 0, 1.5f), 0.18f)).Append(card.Inner.DOLocalRotate(new Vector3(0, 0, -1f), 0.18f))
                    .Append(card.Inner.DOLocalRotate(Vector3.zero, 0.18f)).SetLoops(-1).SetUpdate(true);
                card.Glow.DOFade(0.5f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(card);
            }

            Sfx(SoundIds.CARD_DRAW);
            yield return Rise(card, _centre, _centreScale, 0);
            if (data.NewHero) Sounds?.Play(data.Rarity == HeroRarity.Legendary ? SoundIds.HERO_RISER : SoundIds.HERO_RISER_SHORT);
            _phase = Phase.Down;
            Say(_prompts.Reveal);
            SyncSkip();
            if (_skipping && !data.Charged) yield return Flip();
        }

        // Out of the chest's mouth, small and tilted, to its spot with a little overshoot.
        private IEnumerator Rise(RevealCardView card, Vector2 to, float scale, float delayMs)
        {
            if (delayMs > 0) yield return Wait(delayMs);
            var rect = card.Rect;
            rect.anchoredPosition = Mouth;
            rect.localScale = Vector3.one * (scale * 0.3f);
            rect.localRotation = Quaternion.Euler(0, 0, 8);
            var group = Group(card);
            group.alpha = 0;
            var lift = new Vector2(0, card.Rect.sizeDelta.y * scale * 0.08f);
            var rise = DOTween.Sequence()
                .Append(rect.DOAnchorPos(to + lift, Ms(400)).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(scale, Ms(400)).SetEase(Ease.OutCubic))
                .Join(rect.DOLocalRotate(Vector3.zero, Ms(400)))
                .Join(group.DOFade(1, Ms(300)))
                .Append(rect.DOAnchorPos(to, Ms(160)).SetEase(Ease.OutQuad));
            yield return Run(rise);
        }

        // A ten-call's goods rise together, face up: nothing in them is a surprise worth a flip each.
        private IEnumerator DrawCrowd(List<int> deal)
        {
            _crowd.Clear();
            var n = deal.Count;
            var gap = Width * 0.03f;
            var w = Mathf.Min(Width * 0.3f, Height * 0.3f / 1.5f, (Width * 0.94f - gap * (n - 1)) / n);
            var scale = w / CARD_W;
            for (var j = 0; j < n; j++)
            {
                var card = _views[deal[j]];
                card.SetDown(false);
                card.transform.SetAsLastSibling();
                var at = new Vector2((j - (n - 1) / 2f) * (w + gap), _centre.y);
                _crowd.Add((card, at, scale));
            }

            Sfx(SoundIds.CARD_DRAW);
            var risings = _crowd.Select((c, j) => StartCoroutine(Rise(c.Card, c.At, c.Scale, j * 70))).ToList();
            foreach (var r in risings) yield return r;
            Sounds?.Play(SoundIds.CARD_IMPACT);
            Sounds?.Play(SoundIds.CARD_REVEAL_COMMON);
            foreach (var c in _crowd) Burst(Fx(c.Card.Rect), ParticleKind.Spark, 8, GOLD, 200, 7, 650);
            _phase = Phase.Up;
            Say(_prompts.Continue);
            SyncSkip();
            if (_skipping) yield return Settle();
        }

        // ---- 3. the flip

        private IEnumerator Flip()
        {
            if (_current < 0) yield break;
            _phase = Phase.Busy;
            Say(string.Empty);
            var card = _views[_current];
            var data = card.Data;
            _tremble?.Kill();
            DOTween.Kill(card);
            card.Inner.localRotation = Quaternion.identity;
            card.Glow.gameObject.SetActive(false);
            Sfx(SoundIds.CARD_FLIP);
            yield return Run(DOTween.Sequence()
                .Append(card.Inner.DOScale(new Vector3(0, 1.12f, 1), Ms(210)).SetEase(Ease.InQuad)));
            card.SetDown(false);
            yield return Run(DOTween.Sequence()
                .Append(card.Inner.DOScale(Vector3.one, Ms(210)).SetEase(Ease.OutQuad)));
            var at = Fx(card.Rect);
            if (!data.NewHero)
            {
                Sounds?.Play(SoundIds.CARD_IMPACT);
                var stinger = data.Rarity == HeroRarity.Legendary ? SoundIds.CARD_REVEAL_LEGEND
                    : data.Rarity == HeroRarity.Rare ? SoundIds.CARD_REVEAL_RARE : SoundIds.CARD_REVEAL_COMMON;
                if (!_skipping || stinger != SoundIds.CARD_REVEAL_COMMON) Sounds?.Play(stinger);
            }

            if (data.Kind == PrizeKind.Fragments && data.Bar != null) yield return FillBar(card);
            if (data.Kind == PrizeKind.Bag) yield return FillBag(card);
            if (data.NewHero)
            {
                yield return Celebrate(card);
                yield break;
            }

            if (data.Rarity is HeroRarity.Rare or HeroRarity.Legendary) Burst(at, ParticleKind.Spark, 22, Light(data.Rarity.Value), 300, 10, 900);
            else Burst(at, ParticleKind.Spark, 12, GOLD, 220, 8, 700);
            yield return Run(DOTween.Sequence().Append(card.Rect.DOScale(_centreScale * 1.06f, Ms(130))).Append(card.Rect.DOScale(_centreScale, Ms(130))));
            _phase = Phase.Up;
            Say(string.Empty);
            SyncSkip();
            if (_skipping) yield return Settle();
        }

        // A fragments card's bar fills from what was held to what is held now; a recruit's to the brim, and the seal.
        private IEnumerator FillBar(RevealCardView card)
        {
            var bar = card.Data.Bar;
            Sounds?.Play(SoundIds.BAR_FILL, _skipping ? 0.6f : 1);
            var share = bar.From;
            yield return Run(DOTween.To(() => share, v =>
            {
                share = v;
                card.SetBar(v, bar.FromText);
            }, bar.To, Ms(700 + 500 * Mathf.Min(1, bar.To - bar.From))).SetEase(Ease.InOutCubic));
            card.SetBar(bar.To, bar.ToText);
            if (!bar.Recruited)
            {
                Sfx(SoundIds.CARD_SETTLE);
                yield break;
            }

            Sounds?.Play(SoundIds.CARD_SPARKLE);
            var b = (RectTransform)card.Bar.transform;
            Burst(Fx(b), ParticleKind.Spark, 24, GOLD, 260, 9, 800, radius: b.rect.width * card.Rect.localScale.x / U / 3);
            yield return Stamp(card.Stamp, 1);
            card.SetMissing(false);
            Sounds?.Play(SoundIds.CHEST_LAND);
            yield return Run(DOTween.Sequence().Append(card.Rect.DOScale(_centreScale * 0.97f, Ms(100))).Append(card.Rect.DOScale(_centreScale, Ms(100))));
        }

        // The bag card's bars, every one at once; a recruit's flares and takes its seal.
        private IEnumerator FillBag(RevealCardView card)
        {
            Sounds?.Play(SoundIds.BAR_FILL, _skipping ? 0.6f : 1);
            var t = 0f;
            var rows = card.Rows.Where(r => r.gameObject.activeSelf && r.Data.Bar != null).ToList();
            yield return Run(DOTween.To(() => t, v =>
            {
                t = v;
                foreach (var r in rows) r.SetBar(Mathf.Lerp(r.Data.Bar.From, r.Data.Bar.To, v), r.Data.Bar.FromText);
            }, 1, Ms(900)).SetEase(Ease.InOutCubic));
            var any = false;
            foreach (var r in rows)
            {
                r.SetBar(r.Data.Bar.To, r.Data.Bar.ToText);
                if (!r.Data.Bar.Recruited) continue;
                any = true;
                Burst(Fx((RectTransform)r.transform), ParticleKind.Spark, 18, GOLD, 220, 8, 750);
                StartCoroutine(Stamp(r.Stamp, 1));
                r.SetMissing(false);
            }

            if (any)
            {
                Sounds?.Play(SoundIds.CARD_SPARKLE);
                Sounds?.Play(SoundIds.CHEST_LAND);
            }
            else Sfx(SoundIds.CARD_SETTLE);
        }

        // The wax comes down hard: from big and turned to its tilt.
        private IEnumerator Stamp(GameObject stamp, float scale)
        {
            stamp.SetActive(true);
            var rect = (RectTransform)stamp.transform;
            var group = stamp.GetComponent<CanvasGroup>();
            rect.localScale = Vector3.one * 3.2f * scale;
            rect.localRotation = Quaternion.Euler(0, 0, 30);
            group.alpha = 0;
            yield return Run(DOTween.Sequence()
                .Append(rect.DOScale(0.9f * scale, Ms(270)).SetEase(Ease.InCubic))
                .Join(rect.DOLocalRotate(new Vector3(0, 0, 12), Ms(270)))
                .Join(group.DOFade(1, Ms(270)))
                .Append(rect.DOScale(scale, Ms(110))));
        }

        // A WHOLE hero joins: the room darkens, a flash, a shake, rays, the plaque, cannons of confetti, a rain of it,
        // fireworks, and a fanfare.
        private IEnumerator Celebrate(RevealCardView card)
        {
            var line = _data.Heroes.FirstOrDefault(h => h.Card == _current);
            var legend = card.Data.Rarity == HeroRarity.Legendary;
            var light = Light(card.Data.Rarity ?? HeroRarity.Common);
            _veil.DOFade(1, 0.4f).SetUpdate(true).SetId(this);
            _chestGroup.DOFade(0.25f, 0.3f).SetUpdate(true);
            _raysImage.color = new Color(light[0].r, light[0].g, light[0].b, 0);
            _raysImage.DOFade(legend ? 0.6f : 0.45f, 0.4f).SetUpdate(true);
            _spin = _rays.DOLocalRotate(new Vector3(0, 0, -360), 40, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);
            if (line != null)
            {
                _kickerText.text = line.Kicker;
                _heroName.text = line.Name;
                _heroTitle.text = line.Title;
                _heroRarity.text = line.Rarity;
            }

            var kick = (RectTransform)_kicker.transform;
            var kickAt = _kickerRest;
            kick.anchoredPosition = kickAt + new Vector2(0, 45);
            kick.DOAnchorPos(kickAt, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
            _kicker.DOFade(1, 0.25f).SetUpdate(true);
            _heroLine.DOFade(1, 0.3f).SetDelay(0.15f).SetUpdate(true);
            Sounds?.Play(legend ? SoundIds.HERO_LEGEND : SoundIds.HERO_NEW);
            Sounds?.Play(SoundIds.HERO_POP);
            Later(260, () => Sounds?.Play(legend ? SoundIds.HERO_FANFARE_LEGEND : SoundIds.HERO_FANFARE));
            if (legend) Later(700, () => Sounds?.Play(SoundIds.HERO_APPLAUSE));
            _holdUntil = Time.unscaledTime + 1.3f * Pace;
            _flash.color = new Color(1, 1, 1, 0.85f);
            Own(_flash.DOFade(0, Ms(650)).SetEase(Ease.OutQuad));
            Own(_shake.DOShakeAnchorPos(Ms(380), new Vector2(Width * 0.012f, Height * 0.006f), 18));
            var size = new Vector2(_fx.rectTransform.rect.width, _fx.rectTransform.rect.height);
            Burst(Fx(card.Rect), ParticleKind.Spark, 50, light, 420, 13, 1200);
            Burst(new Vector2(0, size.y * 0.95f), ParticleKind.Confetti, legend ? 90 : 60, CONFETTI, 760, 11, 3000, -Mathf.PI / 3, 0.5f, 380);
            Burst(new Vector2(size.x, size.y * 0.95f), ParticleKind.Confetti, legend ? 90 : 60, CONFETTI, 760, 11, 3000, -Mathf.PI * 2 / 3, 0.5f, 380);
            for (var i = 0; i < (legend ? 14 : 9); i++)
                Later(200 + i * 220, () => Burst(new Vector2(UnityEngine.Random.value * size.x, -10), ParticleKind.Confetti, 8, CONFETTI, 60, 10, 3200,
                    Mathf.PI / 2, 1, 120));
            for (var i = 0; i < (legend ? 6 : 4); i++)
            {
                var n = i;
                Later(350 + i * 330, () =>
                {
                    var p = new Vector2(size.x * (0.15f + UnityEngine.Random.value * 0.7f), size.y * (0.12f + UnityEngine.Random.value * 0.4f));
                    Burst(p, ParticleKind.Spark, 28, n % 2 == 0 ? light : GOLD, 240, 8, 900, gravity: 90);
                    Sounds?.Play(SoundIds.CARD_SPARKLE, 0.5f);
                });
            }

            yield return Run(DOTween.Sequence()
                .Append(card.Rect.DOScale(_centreScale * 0.85f, 0.01f))
                .Append(card.Rect.DOScale(_centreScale * 1.18f, Ms(285)).SetEase(Ease.OutBack))
                .Append(card.Rect.DOScale(_centreScale, Ms(235))));
            _phase = Phase.Up;
            Say(_prompts.Continue);
            SyncSkip();
        }

        private void Later(float ms, Action fn) => _party.Add(DOVirtual.DelayedCall(ms * Pace / 1000, () => fn()).SetUpdate(true));

        private void EndParty()
        {
            foreach (var t in _party) t.Kill();
            _party.Clear();
            _spin?.Kill();
            if (_veil == null) return;
            _veil.DOFade(0, 0.3f).SetUpdate(true);
            _raysImage.DOFade(0, 0.3f).SetUpdate(true);
            _kicker.DOFade(0, 0.2f).SetUpdate(true);
            _heroLine.DOFade(0, 0.2f).SetUpdate(true);
            if (_phase != Phase.Done) _chestGroup.DOFade(1, 0.3f).SetUpdate(true);
        }

        // ---- 4. home: the card flies to its place, dimmed, and the next rises

        private IEnumerator FlyHome(RevealCardView card)
        {
            var i = _views.IndexOf(card);
            yield return Run(DOTween.Sequence()
                .Append(card.Rect.DOAnchorPos(_home[i], Ms(440)).SetEase(Ease.InOutCubic))
                .Join(card.Rect.DOScale(_homeScale[i], Ms(440)).SetEase(Ease.InOutCubic)));
            card.SetDim(0.4f);
            Sfx(SoundIds.CARD_SETTLE);
            Burst(Fx(card.Rect), ParticleKind.Spark, 5, GOLD, 90, 5, 450);
        }

        private IEnumerator Settle()
        {
            _phase = Phase.Busy;
            Say(string.Empty);
            List<RevealCardView> going;
            if (_crowd.Count > 0)
            {
                going = _crowd.Select(c => c.Card).ToList();
                _crowd.Clear();
            }
            else
            {
                if (_current < 0) yield break;
                EndParty();
                going = new List<RevealCardView> { _views[_current] };
                _current = -1;
            }

            Sfx(SoundIds.CARD_WHOOSH);
            var homes = going.Select(c => StartCoroutine(FlyHome(c))).ToList();
            if (_nextDeal < _deals.Count)
            {
                yield return Wait(120);
                yield return Draw();
                foreach (var h in homes) yield return h;
            }
            else
            {
                foreach (var h in homes) yield return h;
                yield return Finish();
            }
        }

        // The last card is home: the chest sinks, every card lights, Collect.
        private IEnumerator Finish()
        {
            _phase = Phase.Busy;
            _skipping = false;
            SyncSkip();
            Say(string.Empty);
            _breathe?.Kill();
            yield return Run(DOTween.Sequence()
                .Append(_chest.DOAnchorPos(_chest.anchoredPosition - new Vector2(0, CHEST * 0.35f), Ms(420)).SetEase(Ease.InQuad))
                .Join(_chest.DOScale(0.9f, Ms(420)))
                .Join(_chestGroup.DOFade(0, Ms(420))));
            _fx.Ambient(false);
            _title.DOFade(1, 0.45f).SetUpdate(true);
            _foot.DOFade(1, 0.45f).SetDelay(0.25f).SetUpdate(true);
            _foot.blocksRaycasts = true;
            Sounds?.Play(SoundIds.CHEST_SUMMARY);

            // The cards are already where they belong; only the block moves, to the middle between the plaque and the
            // button.
            var top = _home.Select((h, i) => h.y + _views[i].Rect.sizeDelta.y * _homeScale[i] / 2).Max();
            var bottom = _home.Select((h, i) => h.y - _views[i].Rect.sizeDelta.y * _homeScale[i] / 2 - (_views[i].Data.Bar != null ? 30 : 0)).Min();
            var titleBottom = -(TopInset + 44 * U + 260);
            var footTop = -(Height - BottomInset - 56 * U - 130);
            var dy = (titleBottom + footTop) / 2 - (top + bottom) / 2;
            for (var i = 0; i < _data.Cards.Count; i++)
            {
                var card = _views[i];
                var n = i;
                if (Mathf.Abs(dy) > 4) card.Rect.DOAnchorPosY(_home[n].y + dy, 0.42f).SetEase(Ease.OutCubic).SetUpdate(true);
                DOVirtual.DelayedCall(n * 0.07f, () =>
                {
                    card.SetDim(0);
                    card.Rect.DOPunchScale(Vector3.one * _homeScale[n] * 0.08f, 0.5f, 1, 0).SetUpdate(true);
                    Burst(Fx(card.Rect), ParticleKind.Spark, 6, GOLD, 120, 7, 600);
                }).SetUpdate(true);
            }

            yield return new WaitForSecondsRealtime(0.42f);
            _phase = Phase.Done;
        }

        // ---- taps

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null) return;
            if (_phase is Phase.Busy or Phase.Landing)
            {
                Hurry();
                return;
            }

            if (_phase == Phase.Up && Time.unscaledTime < _holdUntil) return;
            if (_phase == Phase.Down) _run = StartCoroutine(Flip());
            else if (_phase == Phase.Up) _run = StartCoroutine(Settle());
            else if (_phase == Phase.Done) CollectTapped?.Invoke();
        }

        private void OnSkip()
        {
            _skipping = true;
            SyncSkip();
            Hurry();
            var current = _current >= 0 ? _views[_current].Data : null;
            if (_phase == Phase.Down && current != null && !current.Charged) _run = StartCoroutine(Flip());
            else if (_phase == Phase.Up && (_crowd.Count > 0 || (current != null && !current.NewHero))) _run = StartCoroutine(Settle());
        }

        // ---- words the presenter sets once

        private (string Reveal, string Continue) _prompts = (string.Empty, string.Empty);

        public void SetWords(string tapToReveal, string tapToContinue, string skip, string collect, string rewards)
        {
            _prompts = (tapToReveal, tapToContinue);
            _skip.GetComponentInChildren<TMP_Text>().text = skip;
            _collect.Label = collect;
            _plaque.text = rewards;
        }

        private static Color[] Light(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Legendary => new[] { Hex(0xffe9a8), Hex(0xf2b233), Hex(0xfff6da) },
            HeroRarity.Rare => new[] { Hex(0xd9c4ff), Hex(0xa98be6), Color.white },
            _ => new[] { Hex(0xbfe3f5), Hex(0x7fc3e6), Color.white },
        };

        private static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
