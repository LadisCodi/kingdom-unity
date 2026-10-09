using System;
using System.Collections.Generic;
using Codigames.Game.Data.Tutorial;
using Codigames.Game.Map;
using Codigames.Game.UI.Hud;
using Codigames.Kingdom.Tutorial;
using Codigames.Modules.Audio;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.UI.Stage
{
    // THE STAGE'S CLOCK (Docs/features/24-dialogue.md, 23-tutorials.md). Between scenes it asks the director four
    // times a second whether one is due; while one plays it types the line, voices whoever takes their turn,
    // moves a line on when its condition comes true, and takes the tap that moves a tap line on — anywhere on the
    // screen, as a visual novel does. A line that asks for an action is read first, then acted on: the box steps
    // aside and the game is the player's.
    public class StagePresenter : IStartable, ITickable, IDisposable
    {
        private const float CHECK_SECONDS = 0.25f;
        private const float LINE_CHECK_SECONDS = 0.1f;
        private const float MAX_STEP = 0.1f;
        private const int TICK_EVERY = 3;
        private const string TEXT_TICK = "textTick";
        private const string VOICE = "voice:";
        private const string AUTO = "auto";
        private const string LOW = "low";

        private readonly StageView _view;
        private readonly SceneDirector _director;
        private readonly IConditions _conditions;
        private readonly IScenePurse _purse;
        private readonly IStageContext _context;
        private readonly ICatalog<ISpeaker> _speakers;
        private readonly IStageSettings _settings;
        private readonly TapCount _taps;
        private readonly MapGestures _gestures;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;

        private ScenePlay _playing;
        private float _typed;
        private string _text = "";
        private bool _acting;
        private string _voiced;
        private float _gapUntil;
        private float _graceUntil;
        private float _lastCheck;
        private int _tookTapAt = -1;

        public StagePresenter(StageView view, SceneDirector director, IConditions conditions, IScenePurse purse, IStageContext context,
            ICatalog<ISpeaker> speakers, IStageSettings settings, TapCount taps, MapGestures gestures, Localizer localizer, ISoundService sounds,
            RewardFlight flight, RewardFragments fragments)
        {
            _view = view;
            _director = director;
            _conditions = conditions;
            _purse = purse;
            _context = context;
            _speakers = speakers;
            _settings = settings;
            _taps = taps;
            _gestures = gestures;
            _localizer = localizer;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
        }

        private ISceneLine Line => _playing?.Line;

        // A line that waits for a tap takes one anywhere, and keeps it.
        private bool WaitsForTap => _playing != null && Line != null && (Line.Until.Kind == ConditionKind.Tap || !_acting);

        // A line that appeared on its own takes no input for a moment: the tap already begun was meant for the game.
        private bool InGrace => _playing != null && Time.unscaledTime < _graceUntil;

        public void Start()
        {
            LeanTouch.OnFingerTap += OnFingerTap;
            _gestures.SetTapGate(AllowsMapTap);
        }

        public void Dispose()
        {
            LeanTouch.OnFingerTap -= OnFingerTap;
            _gestures.SetTapGate(null);
        }

        public void Tick()
        {
            var now = Time.unscaledTime;
            if (_playing == null)
            {
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

            Type(Mathf.Min(MAX_STEP, Time.unscaledDeltaTime));
            _view.SetCatching(WaitsForTap || InGrace);

            if (now - _lastCheck < LINE_CHECK_SECONDS) return;
            _lastCheck = now;
            var line = Line;
            if (line == null) return;
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

            // A line with nothing to say is the pointer alone: no box, nobody on stage.
            if (string.IsNullOrEmpty(line.Text))
            {
                _view.Leave(StageSide.Left);
                _view.Leave(StageSide.Right);
                _voiced = null;
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
            _view.Place(line.Box == AUTO ? LOW : line.Box);
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

        private void OnFingerTap(LeanFinger finger)
        {
            if (!WaitsForTap || InGrace) return;
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

        // The line has been read: the box and the cast step aside, and the game is the player's.
        private void Act()
        {
            _acting = true;
            _view.SetActing(true);
        }

        private void Next()
        {
            var line = Line;
            if (line != null && line.Exit) _view.Leave(line.Side);
            _playing.Next();
            if (_playing.Finished) End();
        }

        private void End()
        {
            if (_playing == null) return;
            _director.MarkPlayed(_playing.Scene);
            _playing.LineBegan -= OnLineBegan;
            _playing.Stocked -= OnStocked;
            _playing = null;
            _gapUntil = Time.unscaledTime + (float)_settings.SceneGapSeconds;
            _view.Close();
        }

        // What a line hands over bursts out of whoever said it, and flies to the purse.
        private void OnStocked(ISceneLine line, IReadOnlyDictionary<string, double> added)
            => _flight.Fly(added, _view.SpeakerScreenPoint(line.Side), (c, a) => _fragments.For(c, a, false));

        // While a line waits for a tap, or is locked against everything, the map takes no tap.
        private bool AllowsMapTap(ModuleVector2Int cell)
        {
            if (Time.frameCount == _tookTapAt) return false;
            if (_playing == null) return true;
            if (WaitsForTap || InGrace) return false;
            return Line == null || Line.Lock != LineLock.All;
        }

        private void Voice(string speaker, string expression)
        {
            if (!string.IsNullOrEmpty(expression) && _sounds.Play($"{VOICE}{speaker}_{expression}")) return;
            _sounds.Play(VOICE + speaker);
        }

        private void Grace() => _graceUntil = Time.unscaledTime + (float)_settings.InputGraceSeconds;
    }
}
