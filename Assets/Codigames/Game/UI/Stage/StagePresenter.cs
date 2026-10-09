using System;
using System.Collections.Generic;
using Codigames.Game.Data.Tutorial;
using Codigames.Game.Dev;
using Codigames.Game.Map;
using Codigames.Game.UI.Hud;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Audio;
using Codigames.Modules.Cameras;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2 = Codigames.Modules.Core.Vector2;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.UI.Stage
{
    // THE STAGE'S CLOCK (Docs/features/24-dialogue.md, 23-tutorials.md). Between scenes it asks the director four
    // times a second whether one is due, and points the quest's "show me" with the hand; while one plays it types the
    // line, voices whoever takes their turn, keeps the box clear of what the line is about, points at it, moves a
    // line on when its condition comes true, and takes the tap that moves a tap line on — anywhere on the screen, as
    // a visual novel does. A line that asks for an action is read first, then acted on: the box steps aside, the hand
    // comes, and the lock lets through only what the line asks for.
    public class StagePresenter : IStartable, ITickable, IDisposable
    {
        private const float CHECK_SECONDS = 0.25f;
        private const float LINE_CHECK_SECONDS = 0.1f;
        private const float MAX_STEP = 0.1f;
        // A target can still be arriving when its line starts (a sheet unrolling): the box's place is judged again
        // until then; after that, only to stop covering it.
        private const float PLACE_SETTLE_SECONDS = 0.7f;
        // A map target out of sight this long, the player's hands off the screen, brings the camera back to it.
        private const float REFOCUS_SECONDS = 1.5f;
        private const int TICK_EVERY = 3;
        private const string TEXT_TICK = "textTick";
        private const string VOICE = "voice:";
        private const string AUTO = "auto";
        private const string QUEST = "quest";
        private const string UI = "ui:";

        private readonly StageView _view;
        private readonly SceneDirector _director;
        private readonly IConditions _conditions;
        private readonly IScenePurse _purse;
        private readonly IStageContext _context;
        private readonly ICatalog<ISpeaker> _speakers;
        private readonly IStageSettings _settings;
        private readonly TapCount _taps;
        private readonly MapGestures _gestures;
        private readonly MapTargets _mapTargets;
        private readonly UiTargets _uiTargets;
        private readonly StageHint _hint;
        private readonly PlotGlow _glow;
        private readonly ProvinceMap _map;
        private readonly CameraController _camera;
        private readonly IClock _clock;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;
        private readonly DevSwitches _dev;

        private ScenePlay _playing;
        private float _typed;
        private string _text = "";
        private bool _acting;
        private string _voiced;
        private float _gapUntil;
        private float _graceUntil;
        private float _lastCheck;
        private int _tookTapAt = -1;
        private StageTarget? _target;
        private float? _hiddenSince;
        private float? _missingSince;
        private bool _lockReleased;
        private float _settleUntil;
        private float _lastActivity;
        private bool _sheetWasOpen;
        private bool _hinting;

        public StagePresenter(StageView view, SceneDirector director, IConditions conditions, IScenePurse purse, IStageContext context,
            ICatalog<ISpeaker> speakers, IStageSettings settings, TapCount taps, MapGestures gestures, MapTargets mapTargets, UiTargets uiTargets,
            StageHint hint, PlotGlow glow, ProvinceMap map, CameraController camera, IClock clock, Localizer localizer,
            ISoundService sounds, RewardFlight flight, RewardFragments fragments, DevSwitches dev)
        {
            _dev = dev;
            _view = view;
            _director = director;
            _conditions = conditions;
            _purse = purse;
            _context = context;
            _speakers = speakers;
            _settings = settings;
            _taps = taps;
            _gestures = gestures;
            _mapTargets = mapTargets;
            _uiTargets = uiTargets;
            _hint = hint;
            _glow = glow;
            _map = map;
            _camera = camera;
            _clock = clock;
            _localizer = localizer;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
        }

        // A scene is on the stage.
        public bool IsPlaying => _playing != null;

        // May a long press pick something up? Not while a line waits for a tap or holds anything but the map.
        public bool AllowsHold => _playing == null || (!WaitsForTap && !InGrace && (LockNow == LineLock.None || LockNow == LineLock.Map));

        private ISceneLine Line => _playing?.Line;

        // A line that waits for a tap takes one anywhere, and keeps it.
        private bool WaitsForTap => _playing != null && Line != null && (Line.Until.Kind == ConditionKind.Tap || !_acting);

        // A line that appeared on its own takes no input for a moment: the tap already begun was meant for the game.
        private bool InGrace => _playing != null && Time.unscaledTime < _graceUntil;

        // What the line lets through; a lock whose target went missing has let go.
        private LineLock LockNow => Line == null || _lockReleased ? LineLock.None : Line.Lock;

        public void Start()
        {
            LeanTouch.OnFingerTap += OnFingerTap;
            LeanTouch.OnFingerDown += OnFingerDown;
            _gestures.Tapped += OnMapTapped;
            _gestures.SetTapGate(AllowsMapTap);
        }

        public void Dispose()
        {
            LeanTouch.OnFingerTap -= OnFingerTap;
            LeanTouch.OnFingerDown -= OnFingerDown;
            _gestures.Tapped -= OnMapTapped;
            _gestures.SetTapGate(null);
        }

        public void Tick()
        {
            // The dev switch: the stage stands aside, the scene playing left unplayed for later.
            if (_dev.TutorialsOff)
            {
                StandAside();
                return;
            }

            var now = Time.unscaledTime;
            // Back on the map is where a scene expects the player: the last sheet closing ends the breath.
            var sheetOpen = _context.HasOpenSheet;
            if (_sheetWasOpen && !sheetOpen)
            {
                _gapUntil = 0;
                _lastCheck = 0;
            }

            _sheetWasOpen = sheetOpen;

            if (_playing == null)
            {
                PointHint(sheetOpen);
                if (now - _lastCheck < CHECK_SECONDS) return;
                _lastCheck = now;
                LookForScene(now);
                return;
            }

            // Marked played from outside (a dev skip): it leaves the screen.
            if (_director.IsPlayed(_playing.Scene))
            {
                End();
                return;
            }

            // An unlock splash over a line: the line steps out of the way and waits, where it was.
            var held = _context.HeldBack;
            _view.SetMuted(held);
            if (held) return;

            var line = Line;
            if (line == null) return;
            Type(Mathf.Min(MAX_STEP, Time.unscaledDeltaTime));
            Follow(line, now);

            if (now - _lastCheck < LINE_CHECK_SECONDS) return;
            _lastCheck = now;
            if (!_acting) Replace(line, now);
            if (_playing.LineHolds(line))
            {
                Grace();
                Next();
            }
            // Already done mid-scene: a later line's progress already met jumps the scene past it.
            else if (line.Until.Kind != ConditionKind.Tap)
            {
                var ahead = _playing.ProgressedTo(_playing.Index + 1);
                if (ahead > _playing.Index + 1)
                {
                    Grace();
                    _playing.Begin(ahead);
                    if (_playing.Finished) End();
                }
            }
        }

        private void LookForScene(float now)
        {
            var pick = _director.Pick(_context, now < _gapUntil);
            foreach (var scene in pick.Settled) _director.MarkPlayed(scene);
            if (pick.Scene != null) Begin(pick.Scene);
        }

        private void Begin(ISceneDefinition scene)
        {
            _voiced = null;
            _acting = false;
            _hint.Clear();
            StopHint();
            _playing = new ScenePlay(scene, _conditions, _purse, () => _taps.Taps);
            _playing.LineBegan += OnLineBegan;
            _playing.Stocked += OnStocked;
            _view.Open();
            Grace();
            _playing.Start();
            if (_playing.Finished) End();
        }

        private void OnLineBegan(ISceneLine line)
        {
            // Back from acting: the box returns, and whoever speaks walks back on.
            if (_acting)
            {
                _view.Leave(StageSide.Left);
                _view.Leave(StageSide.Right);
            }

            _acting = false;
            _view.SetActing(false);
            _missingSince = null;
            _hiddenSince = null;
            _lockReleased = false;

            // The camera flies to a map target before the line appears.
            _target = Resolve(line.Point, null);
            if (_target?.Map is { } plot) Glide(plot);

            // A line with nothing to say is the pointer alone: no box, nobody on stage.
            if (string.IsNullOrEmpty(line.Text))
            {
                _view.Leave(StageSide.Left);
                _view.Leave(StageSide.Right);
                _voiced = null;
                _text = "";
                Act();
                return;
            }

            // A speaker taking their turn says so, once, not on every line they speak in a row.
            if (line.Speaker != _voiced) Voice(line.Speaker, line.Expression);
            _voiced = line.Speaker;

            _speakers.TryGet(line.Speaker, out var speaker);
            _view.Cast(line.Side, line.Speaker, speaker?.Picture(line.Expression));
            _view.Light(line.Side);
            _text = _localizer.Tr(line.Text);
            _typed = 0;
            _view.ShowLine(speaker != null ? _localizer.Tr(speaker.Name) : line.Speaker, speaker?.Ribbon, line.Side, _text);
            _view.SetMore(false);
            _settleUntil = Time.unscaledTime + PLACE_SETTLE_SECONDS;
            _view.Place(line.Box == AUTO ? _view.BestPlace(JudgedRect()) : line.Box);
        }

        // The line types itself, a soft knock every third letter — never on a space.
        private void Type(float dt)
        {
            if (_typed >= _text.Length) return;
            var before = Mathf.FloorToInt(_typed);
            _typed = Mathf.Min(_text.Length, _typed + (float)_settings.TypeCharsPerSecond * dt);
            var shown = Mathf.FloorToInt(_typed);
            for (var i = before; i < shown; i++)
            {
                if (i % TICK_EVERY != 0 || char.IsWhiteSpace(_text[i])) continue;
                _sounds.Play(TEXT_TICK);
                break;
            }

            _view.SetTyped(shown);
            _view.SetMore(shown >= _text.Length);
        }

        // Keeps the target found and in sight, points at it, and holds the lock.
        private void Follow(ISceneLine line, float now)
        {
            var was = _target?.Map;
            _target = Resolve(line.Point, _target);
            // A map target that moved on (the forest just cleared) takes the camera with it.
            if (_target?.Map is { } plot && was.HasValue && !was.Value.Anchor.Equals(plot.Anchor)) Glide(plot);

            var rect = ScreenRect(_target);
            Refocus(rect, now);

            // A lock never strands the player: a target missing for a while lets go, and the line reads as a hint.
            var lineLock = line.Lock;
            if (_acting && (lineLock == LineLock.Target || lineLock == LineLock.Map) && rect == null && !string.IsNullOrEmpty(line.Point))
            {
                _missingSince ??= now;
                if (now - _missingSince.Value > _settings.LockFailsafeSeconds) _lockReleased = true;
            }
            else
            {
                _missingSince = null;
            }

            Draw(rect, _acting);

            if (WaitsForTap || InGrace) _view.SetCatching(true);
            else if (LockNow == LineLock.All) _view.SetCatching(true);
            else if (LockNow == LineLock.Target) _view.SetCatching(true, _target?.IsUi == true ? rect : null);
            else _view.SetCatching(false);
        }

        // The hand and the motes when it is the player's turn; the halo round a control, the glow on a plot.
        private void Draw(Rect? rect, bool pointing)
        {
            if (rect == null) _view.Pointer.Hide();
            else _view.Pointer.Show(rect.Value, _target?.IsUi == true, pointing);

            _glow.Show(rect != null ? _target?.Map : null);
        }

        // A map target out of sight (off the screen, or under the box) is brought back once the hands are off it.
        private void Refocus(Rect? rect, float now)
        {
            if (_target?.Map is not { } plot || rect == null || !OutOfSight(rect.Value))
            {
                _hiddenSince = null;
                return;
            }

            _hiddenSince ??= now;
            if (now - _hiddenSince.Value <= REFOCUS_SECONDS || now - _lastActivity <= REFOCUS_SECONDS) return;
            Glide(plot);
            _hiddenSince = null;
        }

        private bool OutOfSight(Rect rect)
        {
            var centre = rect.center;
            if (centre.x < 0 || centre.y < 0 || centre.x > Screen.width || centre.y > Screen.height) return true;
            return _view.IsUnderBox(centre);
        }

        // The box moves only to stop covering the target once the line has settled.
        private void Replace(ISceneLine line, float now)
        {
            if (line.Box != AUTO) return;
            var best = _view.BestPlace(JudgedRect());
            if (best == _view.CurrentPlace) return;
            if (now < _settleUntil)
            {
                _view.Place(best);
                return;
            }

            var rect = ScreenRect(_target);
            if (rect != null && _view.Covers(rect.Value)) _view.Place(best);
        }

        // Where the target will be: a map target is being flown to the middle of the screen.
        private Rect? JudgedRect()
        {
            var rect = ScreenRect(_target);
            if (rect == null || _target?.Map == null) return rect;
            var size = rect.Value.size;
            return new Rect(new Vector2(Screen.width, Screen.height) / 2f - size / 2f, size);
        }

        private void OnFingerDown(LeanFinger finger) => _lastActivity = Time.unscaledTime;

        private void OnFingerTap(LeanFinger finger)
        {
            if (!WaitsForTap || InGrace || _context.HeldBack) return;
            // The tap is the stage's: it reaches nothing behind it, even when it ends the scene.
            _tookTapAt = Time.frameCount;
            TapLine();
        }

        // A tap finishes the line typing, then moves a tap line on: two taps, never one, so no line is skipped unread.
        private void TapLine()
        {
            var line = Line;
            if (line == null) return;
            if (_typed < _text.Length)
            {
                _typed = _text.Length;
                _view.SetTyped(_text.Length);
                _view.SetMore(true);
                return;
            }

            if (line.Until.Kind == ConditionKind.Tap) Next();
            else Act();
        }

        // The line has been read: the box and the cast step aside, the hand comes, and the target is the player's.
        private void Act()
        {
            _acting = true;
            _missingSince = null;
            _view.SetActing(true);
        }

        private void Next()
        {
            var line = Line;
            if (line != null && line.Exit) _view.Leave(line.Side);
            _playing.Next();
            if (_playing.Finished) End();
        }

        // Off the screen without a trace: the scene is not marked played, so it plays again once tutorials are back.
        private void StandAside()
        {
            StopHint();
            if (_playing == null) return;
            _playing.LineBegan -= OnLineBegan;
            _playing.Stocked -= OnStocked;
            _playing = null;
            _target = null;
            _glow.Show(null);
            _view.Close();
        }

        private void End()
        {
            if (_playing == null) return;
            _director.MarkPlayed(_playing.Scene);
            _playing.LineBegan -= OnLineBegan;
            _playing.Stocked -= OnStocked;
            _playing = null;
            _target = null;
            _gapUntil = Time.unscaledTime + (float)_settings.SceneGapSeconds;
            _glow.Show(null);
            _view.Close();
        }

        // What a line hands over bursts out of whoever said it, and flies to the purse.
        private void OnStocked(ISceneLine line, IReadOnlyDictionary<string, double> added)
            => _flight.Fly(added, _view.SpeakerScreenPoint(line.Side), (c, a) => _fragments.For(c, a, false));

        // THE ONE GATE ON THE MAP: which taps a line lets through.
        private bool AllowsMapTap(ModuleVector2Int cell)
        {
            if (Time.frameCount == _tookTapAt) return false;
            if (_playing == null) return true;
            if (WaitsForTap || InGrace) return false;
            return LockNow switch
            {
                LineLock.All => false,
                LineLock.Target => _target?.Map is { } plot && plot.Contains(cell),
                _ => true,
            };
        }

        private void OnMapTapped(ModuleVector2Int cell)
        {
            if (_hint.Target is { } hinted && hinted.Contains(cell)) _hint.Clear();
        }

        // Between scenes, the quest's hinted plot wears the hand, the motes and the glow — no box — while the map is
        // in front of the player.
        private void PointHint(bool sheetOpen)
        {
            var hint = sheetOpen ? null : _hint.Current();
            if (hint == null)
            {
                StopHint();
                return;
            }

            _hinting = true;
            _view.SetHintOnly(true);
            var target = StageTarget.Plot(hint.Value);
            var rect = ScreenRect(target);
            if (rect == null) _view.Pointer.Hide();
            else _view.Pointer.Show(rect.Value, false, true);
            _glow.Show(hint);
        }

        private void StopHint()
        {
            if (!_hinting) return;
            _hinting = false;
            _view.SetHintOnly(false);
            _glow.Show(null);
        }

        // A line's point as a target: a control by its key, the quest tracker, the way back, or a plot of the map.
        private StageTarget? Resolve(string point, StageTarget? previous)
        {
            if (string.IsNullOrEmpty(point)) return null;
            if (point == QUEST) return StageTarget.Ui(QUEST);
            if (point == UiTargets.BACK) return StageTarget.Ui(UiTargets.BACK);
            if (point.StartsWith(UI)) return StageTarget.Ui(point.Substring(UI.Length));
            var plot = _mapTargets.Resolve(point, previous?.Map, _clock.NowMs);
            return plot.HasValue ? StageTarget.Plot(plot.Value) : null;
        }

        // Where a target is on the screen now, in pixels; null when it is nowhere to be seen.
        private Rect? ScreenRect(StageTarget? target)
        {
            if (target == null) return null;
            if (target.Value.IsUi)
            {
                var control = _uiTargets.Find(target.Value.UiKey);
                if (control == null) return null;
                var corners = new Vector3[4];
                control.GetWorldCorners(corners);
                var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            var camera = Camera.main;
            if (camera == null || target.Value.Map is not { } plot) return null;
            var points = ProvinceGeometry.Corners(_map, plot.Anchor, plot.Width, plot.Height);
            var rect = new Rect(camera.WorldToScreenPoint(points[0]), Vector2.zero);
            foreach (var point in points)
            {
                var screen = (Vector2)camera.WorldToScreenPoint(point);
                rect = Rect.MinMaxRect(Mathf.Min(rect.xMin, screen.x), Mathf.Min(rect.yMin, screen.y),
                    Mathf.Max(rect.xMax, screen.x), Mathf.Max(rect.yMax, screen.y));
            }

            return rect;
        }

        private void Glide(MapTarget plot)
        {
            var corners = ProvinceGeometry.Corners(_map, plot.Anchor, plot.Width, plot.Height);
            var centre = (corners[0] + corners[2]) / 2f;
            _camera.CenterOn(new ModuleVector2(centre.x, centre.y));
        }

        private void Voice(string speaker, string expression)
        {
            if (!string.IsNullOrEmpty(expression) && _sounds.Play($"{VOICE}{speaker}_{expression}")) return;
            _sounds.Play(VOICE + speaker);
        }

        private void Grace() => _graceUntil = Time.unscaledTime + (float)_settings.InputGraceSeconds;
    }
}
