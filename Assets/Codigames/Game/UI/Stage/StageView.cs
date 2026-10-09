using System.Collections.Generic;
using DG.Tweening;
using Codigames.Kingdom.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Stage
{
    // The stage (Docs/features/24-dialogue.md): a box of parchment on a carved board, a ribbon with the speaker's
    // name, a figure standing on each side, the line typing itself, and a quill that says a tap moves it on. View
    // only: the StagePresenter decides what plays.
    public class StageView : MonoBehaviour
    {
        private const float BOX_IN_SECONDS = 0.22f;
        private const float BOX_MOVE_SECONDS = 0.32f;
        private const float ACTING_FADE_SECONDS = 0.18f;
        private const float BOX_RISE_RPX = 30f;
        private const float QUILL_BOB_RPX = 24f;
        private const float QUILL_BOB_SECONDS = 0.6f;
        private const float SIDE_MARGIN = 36f;
        // How far a figure's feet stand down behind the board's top rim.
        private const float FEET = 78f;

        // Where an `auto` box may sit, in the order tried: a little below the middle, lower, higher with the cast still
        // under the header, and only then the very top.
        private static readonly string[] PLACES = { "low", "bottom", "high", "top" };

        [SerializeField] private CanvasGroup _stage;
        [SerializeField, Tooltip("Invisible, under the box: takes every tap while a line waits for one.")] private Image _catcher;
        [SerializeField] private RaycastHole _hole;
        [SerializeField] private StagePointer _pointer;
        [SerializeField] private StagePeek _peek;
        [SerializeField] private RectTransform _box;
        [SerializeField] private CanvasGroup _boxGroup;
        [SerializeField] private StageActor _left;
        [SerializeField] private StageActor _right;
        [SerializeField] private RectTransform _ribbon;
        [SerializeField] private Image _ribbonImage;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private RectTransform _quill;
        [SerializeField, Tooltip("The header's height under the safe area, in rpx: the top places sit below it.")] private float _hudHeight = 132;
        [SerializeField, Tooltip("The nav bar's height, in rpx: the bottom place sits above it.")] private float _navHeight = 156;

        private Tween _boxTween;
        private Tween _quillBob;
        private Tween _acting;
        private string _place;
        private bool _shown;

        public bool IsShown => _shown;

        public StagePointer Pointer => _pointer;

        public StagePeek Peek => _peek;

        public string CurrentPlace => _place;

        // The safe area the box is placed in.
        private RectTransform Area => (RectTransform)_box.parent;

        // The stage's own group stays at full alpha: a group at 0 stops its whole canvas drawing, the peek with it.
        // The box, the catcher and the pointer are hidden each on its own.
        private void Awake()
        {
            _stage.alpha = 1;
            HideBox();
            _catcher.raycastTarget = false;
            _left.Leave();
            _right.Leave();
            _quillBob = _quill.DOAnchorPosY(_quill.anchoredPosition.y + QUILL_BOB_RPX, QUILL_BOB_SECONDS).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void OnDestroy()
        {
            _boxTween?.Kill();
            _quillBob?.Kill();
            _acting?.Kill();
        }

        // The stage comes up for a scene: the box rises in.
        public void Open()
        {
            if (_shown) return;
            _shown = true;
            _place = null;
            _boxGroup.alpha = 0;
            _boxGroup.blocksRaycasts = true;
            _pointer.Hide();
            _boxGroup.DOFade(1, BOX_IN_SECONDS).SetUpdate(true);
        }

        // Between scenes: only the pointer, for the quest's "show me"; the box and its catcher stay away.
        public void SetHintOnly(bool on)
        {
            if (_shown) return;
            HideBox();
            if (!on) _pointer.Hide();
        }

        public void Close()
        {
            _shown = false;
            HideBox();
            SetCatching(false);
            _pointer.Hide();
            _left.Leave();
            _right.Leave();
        }

        // Out of the way while something sits over the line (an unlock splash), and back where it was after.
        public void SetMuted(bool muted)
        {
            _stage.alpha = muted ? 0 : 1;
            _stage.blocksRaycasts = !muted;
        }

        private void HideBox()
        {
            _acting?.Kill();
            _boxGroup.alpha = 0;
            _boxGroup.blocksRaycasts = false;
        }

        // Every tap is the stage's while a line waits for one, or while a lock holds; the map still pans under it. A
        // hole (in screen pixels) lets the one control a line asks for take its tap.
        public void SetCatching(bool catching, Rect? hole = null)
        {
            _catcher.raycastTarget = catching;
            _hole.Hole = catching ? hole : null;
        }

        // The first place, in order, where the box and the cast standing on it cover none of `target` (in screen
        // pixels) and the cast stands below the header; else the first that covers nothing; else the first.
        public string BestPlace(Rect? target)
        {
            string clear = null;
            foreach (var place in PLACES)
            {
                var (covers, castFits) = Judge(place, target);
                if (!covers && castFits) return place;
                clear ??= covers ? null : place;
            }

            return clear ?? PLACES[0];
        }

        // Does the box at its place now cover `target`?
        public bool Covers(Rect target) => _place != null && Judge(_place, target).Covers;

        // Is a point on the screen under the box?
        public bool IsUnderBox(Vector2 screen)
            => _shown && _boxGroup.alpha > 0.5f && RectTransformUtility.RectangleContainsScreenPoint(_box, screen, null);

        private (bool Covers, bool CastFits) Judge(string place, Rect? target)
        {
            var area = Area.rect;
            var top = area.yMax + Top(place);
            var box = new Rect(area.xMin + SIDE_MARGIN, top - _box.rect.height, area.width - SIDE_MARGIN * 2f, _box.rect.height);
            var parts = new List<Rect> { box };
            var castTop = top;
            foreach (var (actor, left) in new[] { (_left, true), (_right, false) })
            {
                if (!actor.IsOn) continue;
                var height = actor.StandingHeight - FEET;
                var width = box.width * 0.44f;
                parts.Add(new Rect(left ? box.xMin : box.xMax - width, top, width, height));
                castTop = Mathf.Max(castTop, top + height);
            }

            var castFits = castTop <= area.yMax - _hudHeight;
            if (target == null) return (false, castFits);

            var local = ToArea(target.Value);
            foreach (var part in parts)
                if (part.Overlaps(local)) return (true, castFits);
            return (false, castFits);
        }

        private Rect ToArea(Rect screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, screen.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, screen.max, null, out var max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // A line read and now acted on: the box and its cast step aside.
        public void SetActing(bool acting)
        {
            _acting?.Kill();
            _boxGroup.blocksRaycasts = !acting;
            _acting = _boxGroup.DOFade(acting ? 0 : 1, ACTING_FADE_SECONDS).SetUpdate(true);
        }

        // The speaker's line: their ribbon and name on their side, the text set whole (sized to fit) and hidden,
        // to be typed.
        public void ShowLine(string speaker, Sprite ribbon, StageSide side, string text)
        {
            _name.text = speaker;
            _ribbonImage.sprite = ribbon;
            var right = side == StageSide.Right;
            _ribbon.anchorMin = _ribbon.anchorMax = new Vector2(right ? 1 : 0, 1);
            _ribbon.pivot = new Vector2(right ? 1 : 0, _ribbon.pivot.y);
            _ribbon.anchoredPosition = new Vector2(Mathf.Abs(_ribbon.anchoredPosition.x) * (right ? -1 : 1), _ribbon.anchoredPosition.y);

            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate();
        }

        public void SetTyped(int characters) => _text.maxVisibleCharacters = characters;

        public void SetMore(bool more) => _quill.gameObject.SetActive(more);

        public void Cast(StageSide side, string speaker, Sprite picture)
        {
            Actor(side).Cast(speaker, picture);
            if (_place != null) Actor(side).SetRoom(Judge(_place, null).CastFits);
        }

        public void Leave(StageSide side) => Actor(side).Leave();

        public string OnStage(StageSide side) => Actor(side).Speaker;

        // The middle of whoever stands on a side, on the screen; the box's middle when nobody does.
        public Vector2 SpeakerScreenPoint(StageSide side)
        {
            var actor = Actor(side);
            var rect = actor.IsOn ? (RectTransform)actor.transform : _box;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        // The side speaking is lit; whoever stands on the other is dimmed.
        public void Light(StageSide side)
        {
            _left.Light(side == StageSide.Left);
            _right.Light(side == StageSide.Right);
        }

        // The box at a place: low (a little below the middle), bottom, high, top, middle. A box already on screen
        // moves there, overshooting a touch; a new one rises in.
        public void Place(string place)
        {
            if (place == _place) return;
            var room = Judge(place, null).CastFits;
            _left.SetRoom(room);
            _right.SetRoom(room);
            var first = _place == null;
            _place = place;
            var y = Top(place);
            _boxTween?.Kill();
            if (first)
            {
                _box.anchoredPosition = new Vector2(_box.anchoredPosition.x, y - BOX_RISE_RPX);
                _boxTween = _box.DOAnchorPosY(y, BOX_IN_SECONDS).SetEase(Ease.OutCubic).SetUpdate(true);
                return;
            }

            _boxTween = _box.DOAnchorPosY(y, BOX_MOVE_SECONDS).SetEase(Ease.OutBack, 1.56f).SetUpdate(true);
        }

        // Where the box's top edge sits for a place, from the top of the safe area (negative, downwards).
        private float Top(string place)
        {
            var height = Area.rect.height;
            var box = _box.rect.height;
            return place switch
            {
                "bottom" => -(height - _navHeight - 36 - box),
                "high" => -(_hudHeight + 642),
                "top" => -(_hudHeight + 30),
                "middle" => -(height - box) / 2,
                _ => -height * 0.56f,
            };
        }

        private StageActor Actor(StageSide side) => side == StageSide.Left ? _left : _right;
    }
}
