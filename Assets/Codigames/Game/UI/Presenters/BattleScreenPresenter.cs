using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Battles;
using Codigames.Game.Data.Sites;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Battles;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using DG.Tweening;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The fight, played back (Docs/features/combat.md §13). The fight is already over: the rewards are paid and the
    // fallen are off the roster. This replays the log at the tick it was written in — the swings start ahead of their
    // blows so the blow lands on its tick, a heavy blow holds the clock, the last one runs in slow motion — so an
    // interrupted replay costs nothing. Nothing here works an outcome out: every number painted came off the log.
    public class BattleScreenPresenter : AbstractDataMenuPresenter<BattleScreen, BattlePlayback>, IClosableMenuPresenter, ITickable
    {
        private static readonly double[] SPEEDS = { 1, 2, 4 };
        private const float LUNGE_LEAD = 130;
        private const float LUNGE_BACK = 170;
        private const float ARROW_FLIGHT = 300;
        private const float BOLT_FLIGHT = 240;
        private const float CUE_LEAD = ARROW_FLIGHT + LUNGE_LEAD;
        private const float CATCH_UP_MS = 600;
        private const float HOLD_MS = 70;
        private const float HOLD_WIPE_MS = 110;
        private const float HOLD_GAP = 500;
        private const float HEAVY = 0.08f;
        private const float INTRO_MS = 800;
        private const float SLOW_LEAD = 300;
        private const double SLOW_FACTOR = 0.3;
        private const double SLOW_MS = 1000;
        private const double PLAQUE_DELAY_MS = 600;
        private const float BAR_SHAKE = 0.04f;
        private const float CHARGE_MS = 320;
        private const float CARE_FLIGHT = 260;
        // A skill is cast this long before it lands: the arrows of a Volley in the air, an Ambush on its way across.
        private const float SKILL_LUNGE = 180;
        // The rallies are named after the armies have marched on, this far apart (real ms).
        private const float RALLY_GAP_MS = 520;

        private static readonly Color FLOAT = BattleFx.Hex(0xfff6dc);
        private static readonly Color FLOAT_ADV = BattleFx.Hex(0xffb13b);
        private static readonly Color FLOAT_DIS = BattleFx.Hex(0xd9c9a8);
        private static readonly Color FLOAT_HEAL = BattleFx.Hex(0x8fe26c);
        private static readonly Color FLOAT_SHIELD = BattleFx.Hex(0x8fc8ff);

        private sealed class Slot
        {
            public BattleSlotView View;
            public SlotRef Ref;
            public string Type;
            public bool Hero;
            public int Power;
            public int Troops;
            public double Pool;
            public double Max;
            // What its shield has left.
            public double Shield;
            public Vector2 Home;
            public Vector2 Pos;
            public readonly List<(int Tick, Vector2 At)> Track = new();
            public Sequence Lunge;
        }

        private readonly UIManager _ui;
        private readonly Combat _combat;
        private readonly PortraitArt _portraits;
        private readonly Codigames.Game.Data.Heroes.HeroCollection _heroes;
        private readonly Codigames.Game.Heroes.HeroWords _words;
        private readonly ProvinceSitesAsset _sites;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly MusicDirector _music;
        private readonly PlaybackPreferences _preferences;
        private readonly Dictionary<SlotRef, Slot> _slots = new();
        private readonly Dictionary<string, float> _lastSound = new();
        private readonly double[] _power = new double[2];
        private readonly double[] _opening = new double[2];
        private readonly double[] _shown = new double[2];

        private double _speed = 1;
        private BattleLog _log;
        private int _next;
        private int _cue;
        private List<(float At, BattleEvent Event)> _swings = new();
        private List<(float At, Action Run)> _moments = new();
        private float _lastT;
        private float _fxT;
        // Epoch milliseconds: a double, since a float holds an epoch only to the nearest two minutes.
        private double _lastReal;
        private float _lastHold = float.NegativeInfinity;
        private float _barShookAt = float.NegativeInfinity;
        private bool _finalBlow;
        private bool _slowed;
        private double? _resultAt;
        private bool _plaqueUp;
        private bool _exitUp;
        private bool _rewardsUp;
        private int _rallies;

        public BattleScreenPresenter(IMenuViewFactory views, UIManager ui, Combat combat, PortraitArt portraits, ProvinceSitesAsset sites,
            IClock clock, NumberFormat numbers, Localizer localizer, ISoundService sounds, MusicDirector music, PlaybackPreferences preferences,
            Codigames.Game.Data.Heroes.HeroCollection heroes, Codigames.Game.Heroes.HeroWords words) : base(views)
        {
            _heroes = heroes;
            _words = words;
            _preferences = preferences;
            _ui = ui;
            _combat = combat;
            _portraits = portraits;
            _sites = sites;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
            _sounds = sounds;
            _music = music;
        }

        public void RequestClose()
        {
            // Back closes only once the way out is offered: the replay is the screen's, but it is short.
            if (_exitUp) _ = _ui.HideMenu<BattleScreen>();
        }

        protected override void BindInternal(BattleScreen view)
        {
            _log = Data.Log;
            _speed = Data.Speed;
            _slots.Clear();
            _next = 1;
            _cue = 1;
            _swings = new List<(float, BattleEvent)>();
            _moments = new List<(float, Action)>();
            _lastT = 0;
            _fxT = 0;
            _lastHold = float.NegativeInfinity;
            _slowed = false;
            _resultAt = null;
            _plaqueUp = false;
            _exitUp = false;
            _rewardsUp = false;
            _rallies = 0;
            _ui.MenuHidden += OnMenuHidden;
            _finalBlow = _log.Events[_log.Events.Count - 1].Kind == BattleEventKind.End && _log.Reason == EndReason.Wiped;

            var start = _log.Events[0];
            var specs = new List<(Sprite, Vector2, float, bool, bool, string, int, bool, Vector2)>();
            var order = new List<(Slot, bool Ours, int Line)>();
            foreach (var (side, list) in new[] { (Side.Ours, start.Ours), (Side.Theirs, start.Theirs) })
            {
                foreach (var (board, at) in list)
                {
                    var (bust, shift, scale) = Face(board, side);
                    var home = new Vector2(at.X, at.Y);
                    var slot = new Slot
                    {
                        Ref = new SlotRef(side, board.Id), Type = board.Type, Hero = board.IsHero, Power = board.Power, Troops = board.Count,
                        Pool = board.HpPool, Max = Math.Max(1, board.HpUnit * board.Count), Home = home, Pos = home,
                    };
                    slot.Track.Add((0, home));
                    _slots[slot.Ref] = slot;
                    specs.Add((bust, shift, scale, side == Side.Ours, board.IsHero, board.IsHero ? string.Empty : "x" + _numbers.Exact(board.Count),
                        board.Troop == null ? 1 : Troops.RankOf(board.Troop), board.Row == Row.Back && !board.IsHero, home));
                    order.Add((slot, side == Side.Ours, board.IsHero ? 2 : board.Row == Row.Front ? 0 : 1));
                }
            }

            foreach (var e in _log.Events.Where(e => e.Kind == BattleEventKind.Move))
                if (_slots.TryGetValue(e.At, out var s)) s.Track.Add((e.Tick, new Vector2(e.X, e.Y)));

            view.Format = n => _numbers.Exact(n);
            var views = view.Build(specs, Data.Title, Data.Subtitle);
            for (var i = 0; i < order.Count; i++) order[i].Item1.View = views[i];
            foreach (var slot in _slots.Values)
            {
                slot.View.SetLife((float)(slot.Pool / slot.Max), true);
                _power[(int)slot.Ref.Side] += slot.Power * slot.Troops;
            }

            Array.Copy(_power, _opening, 2);
            Array.Copy(_power, _shown, 2);
            PaintBar();
            view.SetSpeed("×" + _numbers.Exact(_speed));
            view.SetSkip(_localizer.Tr("Skip"));
            view.March(order.Select(o => (o.Item1.View, o.Ours, o.Line)).ToList());
            _music.Set(MusicMoment.Battle, true);
            _sounds.Play(SoundIds.BATTLE_START);
            // The armies march on before the first blow: the replay holds while they slide in.
            Data.Hold(INTRO_MS, _clock.NowMs);
            _lastReal = _clock.NowMs;
            Pump();
        }

        protected override void UnbindInternal(BattleScreen view)
        {
            _ui.MenuHidden -= OnMenuHidden;
            _music.Set(MusicMoment.Battle, false);
            foreach (var slot in _slots.Values) slot.Lunge?.Kill();
        }

        // The spoils have been collected.
        private void OnMenuHidden(IMenuPresenter menu)
        {
            if (menu.MenuType == typeof(RevealScreen)) Data?.TakeRewards();
        }

        protected override void SubscribeToViewEventsInternal(BattleScreen view)
        {
            view.SpeedTapped += OnSpeed;
            view.SkipTapped += OnSkip;
            view.ExitTapped += OnExit;
        }

        protected override void UnsubscribeFromViewEventsInternal(BattleScreen view)
        {
            view.SpeedTapped -= OnSpeed;
            view.SkipTapped -= OnSkip;
            view.ExitTapped -= OnExit;
        }

        public void Tick()
        {
            if (IsShown && Data != null) Pump();
        }

        private void OnSpeed()
        {
            var next = SPEEDS[(Array.IndexOf(SPEEDS, _speed) + 1) % SPEEDS.Length];
            _speed = next;
            _preferences.Speed = next;
            Data.SetSpeed(next, _clock.NowMs);
            View.SetSpeed("×" + _numbers.Exact(next));
            _sounds.Play(SoundIds.BUTTON_PRESS);
        }

        private void OnSkip()
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Data.Skip(_clock.NowMs);
        }

        private void OnExit()
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _ = _ui.HideMenu<BattleScreen>();
        }

        private float Pace => (float)Data.Speed;

        // Walks the playback to now: the swings that start, the blows that land, the phase.
        private void Pump()
        {
            var now = _clock.NowMs;
            var t = (float)Data.Ms(now);
            var quiet = t - _lastT > CATCH_UP_MS || Data.Phase != PlaybackPhase.Playing;
            if (quiet)
            {
                _swings.Clear();
                _moments.Clear();
                View.Fx.Clear();
            }
            else
            {
                while (_cue < _log.Events.Count)
                {
                    var e = _log.Events[_cue];
                    if (e.Kind != BattleEventKind.Start && e.Kind != BattleEventKind.End && e.Tick * Data.TickMs - CUE_LEAD > t) break;
                    if (e.Kind == BattleEventKind.Attack && e.Skill == null) _swings.Add((SwingAt(e), e));
                    if (e.Kind == BattleEventKind.Skill && e.Tick > 0)
                    {
                        var at = e.Tick * Data.TickMs;
                        var ev = e;
                        var targets = TargetsOf(_cue);
                        _moments.Add((at - CHARGE_MS, () => Charge(ev)));
                        _moments.Add((at - CastLead(ev.Skill), () => Cast(ev, targets)));
                    }

                    _cue++;
                }

                foreach (var swing in _swings.Where(s => s.At <= t).ToList())
                {
                    _swings.Remove(swing);
                    Swing(swing.Event, t - swing.At);
                }

                foreach (var moment in _moments.Where(m => m.At <= t).ToList())
                {
                    _moments.Remove(moment);
                    moment.Run();
                }
            }

            var endMs = (float)Data.EndMs;
            if (_finalBlow && !_slowed && !quiet && t >= endMs - SLOW_LEAD)
            {
                _slowed = true;
                Data.Slow(SLOW_FACTOR, SLOW_MS, now);
                _sounds.Play(SoundIds.FINAL_BLOW);
            }

            var tick = (int)Math.Floor(t / Data.TickMs);
            while (_next < _log.Events.Count)
            {
                var e = _log.Events[_next];
                if (e.Kind is BattleEventKind.Start or BattleEventKind.End)
                {
                    _next++;
                    continue;
                }

                if (e.Tick > tick) break;
                Apply(e, quiet, e.Tick * Data.TickMs);
                _next++;
            }

            _cue = Math.Max(_cue, _next);
            _lastT = t;
            _fxT = Data.Phase == PlaybackPhase.Playing ? t : _fxT + (float)(now - _lastReal) * Pace;
            Place(t);
            RollBar((float)(now - _lastReal));
            View.Fx.Draw(_fxT);
            _lastReal = now;

            Data.Advance(now);
            if (Data.Phase != PlaybackPhase.Playing)
            {
                View.HideKnobs();
                _resultAt ??= now;
            }

            if (_resultAt != null && now - _resultAt >= PLAQUE_DELAY_MS && !_plaqueUp)
            {
                _plaqueUp = true;
                var won = _log.Winner == Side.Ours;
                View.ShowPlaque(won, won ? _localizer.Tr("Victory") : _localizer.Tr("Defeat"));
                // The fight's tune gives way to the verdict's.
                _music.Set(MusicMoment.Battle, false);
                _sounds.Play(won ? SoundIds.VICTORY : SoundIds.DEFEAT);
                var mid = (Vector2)View.Fx.rectTransform.rect.center;
                View.Fx.Shock(mid, _fxT + 180, 70, won ? BattleFx.Hex(0xffd36a) : new Color(60 / 255f, 36 / 255f, 18 / 255f, 0.8f));
                if (won)
                {
                    View.Fx.Sparks(mid, Mathf.PI / 2, _fxT + 180, 14);
                    View.Fx.Chip(mid, _fxT + 220, 16, BattleFx.Chips.Gold);
                    View.Fx.Sparks(mid, Mathf.PI / 2, _fxT + 420, 10);
                }
                else
                {
                    View.Fx.Chip(mid, _fxT + 180, 8, BattleFx.Chips.Wood);
                }

                // The bar tells the truth at the end: a wiped side is worth nothing.
                _power[won ? 1 : 0] = 0;
                PaintBar();
            }

            // The spoils deal on the reveal, over the board; the way out comes once they are collected.
            if (Data.Phase == PlaybackPhase.Rewards && _plaqueUp && !_rewardsUp)
            {
                _rewardsUp = true;
                _ = _ui.ShowMenu<RevealScreen, Kingdom.Heroes.Reveal>(new Kingdom.Heroes.Reveal
                {
                    Chest = Kingdom.Heroes.RevealChest.Spoils, Caption = _localizer.Tr("Spoils"), Prizes = Data.Prizes,
                });
            }

            if (Data.Phase == PlaybackPhase.Done && _plaqueUp && !_exitUp)
            {
                _exitUp = true;
                View.ShowExit(_localizer.Tr("Leave the field"));
            }
        }

        // Every slot where the log has it at the fight's clock: on its last point, or on its way to the next.
        private void Place(float ms)
        {
            var ft = ms / Data.TickMs;
            foreach (var slot in _slots.Values)
            {
                var track = slot.Track;
                var i = 0;
                while (i + 1 < track.Count && track[i + 1].Tick <= ft) i++;
                var a = track[i].At;
                var pos = a;
                if (i + 1 < track.Count && ft > track[i + 1].Tick - 1)
                {
                    var k = ft - (track[i + 1].Tick - 1);
                    pos = a + (track[i + 1].At - a) * k;
                }

                slot.View.SetWalking(pos != slot.Pos);
                slot.Pos = pos;
                View.Place(slot.View, pos);
            }
        }

        private Vector2 At(Slot slot) => View.OnFx(slot.Pos);

        private bool Ranged(Slot slot) => _combat.TargetingOf(slot.Type) == Targeting.Ranged;

        private float Flight(Slot slot) => slot.Hero ? BOLT_FLIGHT : ARROW_FLIGHT;

        private float SwingAt(BattleEvent e)
        {
            var at = e.Tick * Data.TickMs;
            return _slots.TryGetValue(e.From, out var from) && Ranged(from) ? at - Flight(from) - LUNGE_LEAD : at - LUNGE_LEAD;
        }

        // The swing, `late` ms of the fight after it should have started: melee lunges at its target, a shooter draws back
        // and looses.
        private void Swing(BattleEvent e, float late)
        {
            if (!_slots.TryGetValue(e.From, out var from) || !_slots.TryGetValue(e.At, out var to) || from.View.IsDead) return;
            var fromAt = At(from);
            var toAt = At(to);
            var d = toAt - fromAt;
            var dist = Mathf.Max(0.001f, d.magnitude);
            var u = d / dist;
            var t = e.Tick * Data.TickMs;
            var px = BattleScreen.PX;
            if (Ranged(from))
            {
                DOVirtual.DelayedCall(Mathf.Max(0, LUNGE_LEAD - late) / 1000f / Pace, () => Play(from.Hero ? SoundIds.BOLT_CAST : SoundIds.ARROW_LOOSE, "loose"), true);
                var shafts = from.Hero ? 1 : e.Hits >= 12 ? 3 : e.Hits >= 5 ? 2 : 1;
                for (var i = 0; i < shafts; i++)
                {
                    var off = (i - (shafts - 1) / 2f) * 9 * px;
                    View.Fx.Shoot(from.Hero, fromAt + new Vector2(off, 0), toAt + new Vector2(off * 0.6f, 0), t - Flight(from) - i * 35, t - i * 25,
                        (e.From.Id % 2 == 0 ? 1 : -1) * (14 + i * 4));
                }
            }

            var cavalry = from.Type == "Cavalry";
            var reach = Ranged(from) ? 3 * px : Mathf.Min(dist * (cavalry ? 0.4f : 0.28f), (cavalry ? 32 : 20) * px);
            if (Lunge(from, u, reach, (Ranged(from) ? 6 : 3) * px, LUNGE_LEAD, late) && cavalry)
            {
                View.Fx.Dust(fromAt, t - LUNGE_LEAD);
                Play(SoundIds.CAVALRY_CHARGE, "gallop");
            }
        }

        // Draw back along `u`, go `reach` toward it — peaking `lead` ms of the fight after it starts, on the blow — and come
        // home. False while the slot is still in its last one.
        private bool Lunge(Slot from, Vector2 u, float reach, float back, float lead, float late)
        {
            if (from.Lunge != null && from.Lunge.IsActive() && from.Lunge.IsPlaying()) return false;
            var total = (lead + LUNGE_BACK) / 1000f / Pace;
            var peak = lead / (lead + LUNGE_BACK);
            var body = from.View.Body;
            // y is up on the screen and down on the field: the direction was taken on the screen.
            from.Lunge = DOTween.Sequence().SetUpdate(true)
                .Append(body.DOBlendableLocalMoveBy(-u * back, total * peak * 0.4f).SetEase(Ease.OutQuad))
                .Append(body.DOBlendableLocalMoveBy(u * (back + reach), total * peak * 0.6f).SetEase(Ease.InQuad))
                .Append(body.DOBlendableLocalMoveBy(-u * reach, total * (1 - peak)).SetEase(Ease.OutQuad));
            if (late > 0) from.Lunge.Goto(Mathf.Min(total, late / 1000f / Pace), true);
            return true;
        }

        // The target flinching away from a blow along `u`; `heavy` 0…1.
        private void Flinch(Slot to, Vector2 u, float heavy)
        {
            var k = (3 + 6 * heavy) * BattleScreen.PX;
            var seconds = 0.18f / Pace;
            DOTween.Sequence().SetUpdate(true)
                .Append(to.View.Body.DOBlendableLocalMoveBy(u * k, seconds * 0.25f))
                .Append(to.View.Body.DOBlendableLocalMoveBy(-u * k * 1.35f, seconds * 0.35f))
                .Append(to.View.Body.DOBlendableLocalMoveBy(u * k * 0.35f, seconds * 0.4f));
        }

        private void Hold(float t, float ms)
        {
            if (t - _lastHold < HOLD_GAP) return;
            _lastHold = t;
            Data.Hold(ms, _clock.NowMs);
        }

        private void Impact(BattleEvent e, Slot from, Slot to, float t)
        {
            var d = At(to) - At(from);
            var angle = Mathf.Atan2(d.y, d.x);
            var heavy = Mathf.Min(1, (float)(e.Dealt / to.Max) / (HEAVY * 2));
            Flinch(to, d.normalized, heavy);
            if (Ranged(from))
            {
                View.Fx.Sparks(At(to), angle, t, 3);
            }
            else
            {
                View.Fx.Strike(from.Type == "Lancer", At(to), angle, t, from.Type == "Cavalry" ? 1.25f : from.Hero ? 1.1f : 1);
                View.Fx.Sparks(At(to), angle, t, 4 + Mathf.RoundToInt(heavy * 4));
            }

            if (e.Edge > 0 && e.Dealt >= to.Max * HEAVY) Hold(t, HOLD_MS);
        }

        // Every slot a skill at `index` reaches: the events that follow it on its tick, up to the next skill or blow.
        private List<Slot> TargetsOf(int index)
        {
            var head = _log.Events[index];
            var found = new List<Slot>();
            for (var i = index + 1; i < _log.Events.Count; i++)
            {
                var e = _log.Events[i];
                if (e.Kind is BattleEventKind.End or BattleEventKind.Skill || e.Tick != head.Tick) break;
                if (e.Kind == BattleEventKind.Attack && e.Skill != head.Skill) break;
                if (e.Kind is BattleEventKind.Attack or BattleEventKind.Healed or BattleEventKind.Shielded or BattleEventKind.Dazed
                    && _slots.TryGetValue(e.At, out var s) && !found.Contains(s))
                    found.Add(s);
            }

            return found;
        }

        // A timed skill charges: its caster glows in its tint.
        private void Charge(BattleEvent e)
        {
            if (!_slots.TryGetValue(e.From, out var from) || from.View.IsDead) return;
            var tint = Tint(e.Skill);
            from.View.Glow(tint, (CHARGE_MS + 220) / 1000f / Pace);
            View.Fx.Motes(At(from), e.Tick * Data.TickMs - CHARGE_MS, 5, tint);
            Announce(e.From.Side, e.Skill);
            Play(SoundIds.SKILL_CHARGE, "skill");
        }

        // The skill's name on its cloth across the line between the armies.
        private void Announce(Side side, string skill)
        {
            View.Ribbon(_words.SkillName(skill), Cloth(skill), side == Side.Ours);
            Play(SoundIds.RIBBON, "ribbon");
        }

        private static float CastLead(string skill) => skill switch
        {
            "Volley" => 260,
            "Cleave" or "Crush" => SKILL_LUNGE,
            "Ambush" => 240,
            "Sharpshot" => 40,
            _ => CARE_FLIGHT,
        };

        // THE RALLIES, named one by one after the armies march on: the ribbon, its call, then every slot on that side
        // flares in its colour.
        private void Rally(BattleEvent e, int index)
        {
            DOVirtual.DelayedCall((INTRO_MS + 120 + index * RALLY_GAP_MS) / 1000f, () =>
            {
                if (View == null || Data == null || Data.Phase != PlaybackPhase.Playing) return;
                Announce(e.From.Side, e.Skill);
                var call = e.Skill switch { "WarCry" => SoundIds.WAR_CRY, "Bulwark" => SoundIds.BULWARK, "Vigour" => SoundIds.VIGOUR, _ => null };
                if (call != null) Play(call, "rally");
                var tint = Tint(e.Skill);
                if (_slots.TryGetValue(e.From, out var caster)) caster.View.Glow(tint, 0.6f);
                foreach (var s in _slots.Values.Where(s => s.Ref.Side == e.From.Side && !s.View.IsDead))
                {
                    s.View.Glow(tint, 0.7f);
                    View.Fx.Motes(At(s), _fxT, 3, tint);
                }
            }, true);
        }

        // …then is cast, timed to land on its tick: a bolt of its light to each of them, or the caster on its way.
        private void Cast(BattleEvent e, List<Slot> targets)
        {
            if (!_slots.TryGetValue(e.From, out var from) || targets.Count == 0) return;
            var t = e.Tick * Data.TickMs;
            var tint = Tint(e.Skill);
            var unit = BattleScreen.PX;
            switch (e.Skill)
            {
                case "Volley":
                    Play(SoundIds.VOLLEY, "skillCast");
                    // Arrows fall out of the sky onto every one of them.
                    for (var i = 0; i < targets.Count; i++)
                    {
                        var to = At(targets[i]);
                        for (var k = 0; k < 2; k++)
                        {
                            var off = (k == 0 ? -8 : 9) * unit;
                            View.Fx.Shoot(false, to + new Vector2(off * 1.5f, 150 * unit), to + new Vector2(off, 0), t - 220 - k * 40 - i * 10, t - k * 30, 0);
                        }
                    }

                    return;
                case "Cleave":
                case "Crush":
                {
                    var mid = targets.Aggregate(Vector2.zero, (sum, s) => sum + At(s)) / targets.Count;
                    var d = mid - At(from);
                    Lunge(from, d.normalized, Mathf.Min(d.magnitude * 0.45f, 40 * unit), 6 * unit, SKILL_LUNGE, 0);
                    return;
                }
                case "Ambush":
                {
                    // The caster crosses the board to the back rank and comes home.
                    var d = At(targets[0]) - At(from);
                    from.View.transform.SetAsLastSibling();
                    from.Lunge?.Kill();
                    Lunge(from, d.normalized, d.magnitude * 0.8f, 4 * unit, CastLead("Ambush"), 0);
                    View.Fx.Dust(At(from), t - CastLead("Ambush"));
                    Play(SoundIds.AMBUSH, "skillCast");
                    return;
                }
                case "Sharpshot":
                    View.Fx.Beam(At(from), At(targets[0]), t - CastLead("Sharpshot"), tint);
                    Play(SoundIds.SHARPSHOT, "skillCast");
                    return;
                default:
                    // A heal, a shield, a daze: a bolt of its light to each of them.
                    if (e.Skill == "Wave") View.Fx.Shock(At(from), t - CARE_FLIGHT, 46, tint);
                    Play(SoundIds.BOLT_CAST, "loose");
                    foreach (var to in targets) View.Fx.Shoot(true, At(from), At(to), t - CARE_FLIGHT, t, 16, tint);
                    return;
            }
        }

        // A skill's strike landing: weightier than a swing, and its own.
        private void SkillImpact(BattleEvent e, Slot from, Slot to, float t)
        {
            var d = At(to) - At(from);
            var angle = Mathf.Atan2(d.y, d.x);
            Flinch(to, d.normalized, e.Skill == "Crush" ? 1 : 0.6f);
            switch (e.Skill)
            {
                case "Volley":
                    View.Fx.Sparks(At(to), Mathf.PI / 2, t, 4);
                    break;
                case "Cleave":
                    View.Fx.Strike(false, At(to), angle, t, 1.5f);
                    View.Fx.Sparks(At(to), angle, t, 5);
                    break;
                case "Crush":
                    View.Fx.Strike(false, At(to), angle, t, 1.7f);
                    View.Fx.Shock(At(to), t, 50);
                    View.Fx.Sparks(At(to), angle, t, 14);
                    View.ShakeBoard();
                    Hold(t, HOLD_WIPE_MS);
                    break;
                case "Ambush":
                    View.Fx.Strike(false, At(to), angle, t, 1.4f);
                    View.Fx.Sparks(At(to), angle, t, 8);
                    break;
                default:
                    View.Fx.Sparks(At(to), angle, t, 8);
                    View.Fx.Shock(At(to), t, 24, Tint(e.Skill));
                    break;
            }

            if (e.Edge > 0 && e.Dealt >= to.Max * HEAVY) Hold(t, HOLD_MS);
        }

        // The cloth a skill's ribbon is dyed: by kind, except the rallies that say what they raise.
        private static Color Cloth(string skill) => skill switch
        {
            "Bulwark" => BattleFx.Hex(0x56606c),
            "Vigour" => BattleFx.Hex(0x4b7f36),
            _ => Skills.KindOf(skill) switch
            {
                SkillKind.Strike => BattleFx.Hex(0xa8452a),
                SkillKind.Heal => BattleFx.Hex(0x4b7f36),
                SkillKind.Shield => BattleFx.Hex(0x3d6a96),
                SkillKind.Daze => BattleFx.Hex(0x64458f),
                SkillKind.Rally => BattleFx.Hex(0x93321f),
                _ => BattleFx.Hex(0x8a6420),
            },
        };

        private static Color Tint(string skill) => skill switch
        {
            "Bulwark" => BattleFx.Hex(0xd6dde6),
            "Vigour" => BattleFx.Hex(0x8fe26c),
            _ => Skills.KindOf(skill) switch
            {
                SkillKind.Strike => BattleFx.Hex(0xffb13b),
                SkillKind.Heal => BattleFx.Hex(0x8fe26c),
                SkillKind.Shield => BattleFx.Hex(0x8fc8ff),
                SkillKind.Daze => BattleFx.Hex(0xc9a2ff),
                SkillKind.Rally => BattleFx.Hex(0xff7a5c),
                _ => BattleFx.Hex(0xffd36a),
            },
        };

        // One event on the board. `quiet` is a board catching up: the state lands, nothing flies.
        private void Apply(BattleEvent e, bool quiet, float t)
        {
            if (e.Kind == BattleEventKind.Skill)
            {
                // A rally is named once the armies have marched on; a timed skill was charged and cast ahead of its tick.
                if (e.Tick == 0 && !quiet) Rally(e, _rallies++);
                return;
            }

            if (!_slots.TryGetValue(e.At, out var slot)) return;
            switch (e.Kind)
            {
                case BattleEventKind.Healed:
                    if (!quiet)
                    {
                        View.Float(slot.View, At(slot), "heal", (int)Math.Round(e.Healed), FLOAT_HEAL, 20 * BattleScreen.PX);
                        View.Fx.Motes(At(slot), t, 7, FLOAT_HEAL);
                        Play(SoundIds.HEAL, "heal");
                    }

                    slot.Pool = e.HpPool;
                    slot.View.SetLife((float)(slot.Pool / slot.Max));
                    if (e.Alive != slot.Troops && !slot.Hero)
                    {
                        _power[(int)e.At.Side] += (e.Alive - slot.Troops) * slot.Power;
                        slot.Troops = e.Alive;
                        slot.View.SetCount("x" + _numbers.Exact(e.Alive));
                        PaintBar();
                    }

                    return;
                case BattleEventKind.Attack:
                {
                    _slots.TryGetValue(e.From, out var from);
                    slot.Pool = Math.Max(0, slot.Pool - e.Dealt);
                    slot.View.SetLife((float)(slot.Pool / slot.Max));
                    if (quiet) return;
                    slot.View.Flash();
                    if (e.Dealt > 0)
                    {
                        var (color, size) = e.Edge > 0 ? (FLOAT_ADV, 27f) : e.Edge < 0 ? (FLOAT_DIS, 15f) : (FLOAT, 20f);
                        View.Float(slot.View, At(slot), e.Edge > 0 ? "adv" : e.Edge < 0 ? "dis" : "hit", e.Dealt, color, size * BattleScreen.PX);
                    }

                    if (e.Absorbed > 0)
                    {
                        View.Float(slot.View, At(slot), "shield", e.Absorbed, FLOAT_SHIELD, 15 * BattleScreen.PX);
                        Soak(slot, e.Absorbed, t);
                    }

                    if (from == null) return;
                    if (e.Skill != null) SkillImpact(e, from, slot, t);
                    else Impact(e, from, slot, t);
                    var heavy = Mathf.Min(1, (float)(e.Dealt / slot.Max) / (HEAVY * 2));
                    var skilled = e.Skill switch { "Cleave" => SoundIds.CLEAVE, "Crush" => SoundIds.CRUSH, _ => null };
                    if (skilled != null) Play(skilled, "skillHit");
                    else Play(HitSound(from), "battle", 0.8f + 0.4f * heavy);
                    return;
                }
                case BattleEventKind.TroopsLost:
                {
                    var lostTroops = slot.Troops - e.Alive;
                    var lost = lostTroops * slot.Power;
                    _power[(int)e.At.Side] -= lost;
                    slot.Troops = e.Alive;
                    slot.Pool = e.HpPool;
                    slot.View.SetLife((float)(slot.Pool / slot.Max));
                    if (!quiet) ShakeBar(e.At.Side, lost);
                    if (!slot.Hero)
                    {
                        slot.View.SetCount("x" + _numbers.Exact(e.Alive));
                        if (!quiet)
                        {
                            // A helmet or two rolls off the ring for the men who fell.
                            View.Fx.Helmets(At(slot), t, Math.Min(3, (int)Math.Ceiling(lostTroops / 4.0)));
                            slot.View.PunchCount();
                        }
                    }

                    PaintBar();
                    return;
                }
                case BattleEventKind.SlotWiped:
                    slot.View.Die(e.At.Id + (e.At.Side == Side.Ours ? 0 : 100), quiet);
                    slot.Pool = 0;
                    if (quiet) return;
                    View.Fx.Chip(At(slot), t, slot.Hero ? 14 : 9, slot.Hero ? BattleFx.Chips.Gold : BattleFx.Chips.Wood);
                    View.Fx.Dust(At(slot), t);
                    View.Fx.Shock(At(slot), t + 140, slot.Hero ? 44 : 34);
                    Hold(t, HOLD_WIPE_MS);
                    if (e.Tick == _log.Ticks && _finalBlow) View.FlashWhite();
                    Play(SoundIds.SQUAD_DOWN, "death");
                    DOVirtual.DelayedCall(0.14f, () => Play(SoundIds.SKULL_STAMP, "stamp"), true);
                    return;
                case BattleEventKind.Shielded:
                    slot.Shield = e.Amount;
                    if (quiet) return;
                    Play(SoundIds.SHIELD_UP, "shieldUp");
                    slot.View.ShowShield();
                    return;
                case BattleEventKind.Dazed:
                    if (quiet) return;
                    Play(SoundIds.DAZE, "daze");
                    // The fight's ticks, at the playback's speed, in real time.
                    slot.View.Daze(e.Amount * Data.TickMs / 1000f / Pace);
                    return;
            }
        }

        // A shield takes a blow: it wobbles, or — spent — shatters.
        private void Soak(Slot slot, int absorbed, float t)
        {
            slot.Shield = Math.Max(0, slot.Shield - absorbed);
            if (!slot.View.Shielded) return;
            if (slot.Shield > 0)
            {
                Play(SoundIds.SHIELD_SOAK, "soak");
                slot.View.WobbleShield();
                return;
            }

            slot.View.HideShield();
            Play(SoundIds.SHIELD_BREAK, "shieldBreak");
            View.Fx.Chip(At(slot), t, 10, BattleFx.Chips.Sky);
            View.Fx.Shock(At(slot), t, 38, BattleFx.Hex(0x8fc8ff));
        }

        private void ShakeBar(Side side, int lost)
        {
            var now = Time.unscaledTime;
            if (lost < _opening[(int)side] * BAR_SHAKE || now - _barShookAt < 0.25f) return;
            _barShookAt = now;
            View.ShakeBar(side == Side.Ours);
        }

        private void PaintBar()
        {
            var total = Math.Max(1, _power[0] + _power[1]);
            View.SetBarFill((float)(_power[0] / total));
        }

        // The two numbers roll down to what they are.
        private void RollBar(float ms)
        {
            for (var i = 0; i < 2; i++)
            {
                var gap = _power[i] - _shown[i];
                _shown[i] = Math.Abs(gap) < 1 ? _power[i] : _shown[i] + gap * Math.Min(1, ms / 160);
            }

            View.SetBarText(_numbers.Exact(Math.Round(_shown[0])), _numbers.Exact(Math.Round(_shown[1])));
        }

        private string HitSound(Slot from) => Ranged(from) ? SoundIds.ARROW_HIT
            : from.Type == "Lancer" ? SoundIds.LANCE_HIT
            : from.Type == "Cavalry" ? SoundIds.CAVALRY_HIT : SoundIds.SWORD_HIT;

        // A sound, at most once a beat in its group: a front line takes a dozen blows a second.
        private void Play(string id, string group, float volume = 1)
        {
            var now = Time.unscaledTime;
            if (_lastSound.TryGetValue(group, out var last) && now - last < 0.06f) return;
            _lastSound[group] = now;
            _sounds.Play(id, volume);
        }

        // A slot's face: the troop's bust — or, on a side that fields creatures, the creature's; a hero's own portrait.
        private (Sprite, Vector2, float) Face(BoardSlot board, Side side)
        {
            if (board.IsHero && board.FighterId != null && _heroes.TryGet(board.FighterId, out var hero) && hero is Codigames.Game.Data.Heroes.HeroAsset asset)
                return (asset.Portrait, Vector2.zero, 1);
            if (board.Troop == null) return (null, Vector2.zero, 1);
            if (side == Side.Theirs && Data.Creatures && _sites.CreatureOf(board.Type) is { } creature) return (creature, Vector2.zero, 1);
            var bust = _portraits.Of(board.Troop);
            return (bust?.Sprite, bust?.Shift ?? Vector2.zero, bust?.Scale ?? 1);
        }
    }
}
