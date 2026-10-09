using System;
using Codigames.Game.Data.Tutorial;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Quests;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Tutorial;
using Codigames.Kingdom.Tutorial.State;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Stage
{
    // HELP FOR A PLAYER WHO SEEMS STUCK (Docs/features/23-tutorials.md): idle on an unfinished quest, with the map in
    // front of them and no scene playing, the quest scroll wiggles; idle longer, early in the game, Isolde leans in
    // from the edge to offer a hand — once in a while, never nagging — and a tap on her shows the way.
    public class IdleHelp : IStartable, ITickable, IDisposable
    {
        private const float CHECK_SECONDS = 0.25f;
        private const string ADVISOR = "advisor";

        private readonly QuestChain _chain;
        private readonly ICatalog<IQuestDefinition> _quests;
        private readonly IStageContext _context;
        private readonly TutorialState _tutorial;
        private readonly IStageSettings _settings;
        private readonly StagePresenter _stage;
        private readonly StageView _view;
        private readonly IMenuViewFactory _views;
        private readonly QuestFocus _focus;
        private readonly ICatalog<ISpeaker> _speakers;
        private readonly Localizer _localizer;
        private readonly Dev.DevSwitches _dev;

        private float _lastActivity;
        private float _lastCheck;
        private float _peekUntil;
        private float _restUntil;

        public IdleHelp(QuestChain chain, ICatalog<IQuestDefinition> quests, IStageContext context, TutorialState tutorial,
            IStageSettings settings, StagePresenter stage, StageView view, IMenuViewFactory views, QuestFocus focus,
            ICatalog<ISpeaker> speakers, Localizer localizer, Dev.DevSwitches dev)
        {
            _dev = dev;
            _chain = chain;
            _quests = quests;
            _context = context;
            _tutorial = tutorial;
            _settings = settings;
            _stage = stage;
            _view = view;
            _views = views;
            _focus = focus;
            _speakers = speakers;
            _localizer = localizer;
        }

        public void Start()
        {
            _lastActivity = Time.unscaledTime;
            LeanTouch.OnFingerDown += OnFingerDown;
            _view.Peek.Tapped += OnPeekTapped;
        }

        public void Dispose()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            _view.Peek.Tapped -= OnPeekTapped;
        }

        public void Tick()
        {
            var now = Time.unscaledTime;
            if (now - _lastCheck < CHECK_SECONDS) return;
            _lastCheck = now;

            var quest = _chain.Active;
            var idle = now - _lastActivity;
            var stuck = !_dev.TutorialsOff && !_stage.IsPlaying && quest != null && !_chain.IsComplete(quest) && !_context.HasOpenSheet && !_tutorial.Veteran;
            _views.Resolve<QuestPill>()?.SetNudging(stuck && idle >= _settings.IdleWiggleSeconds);

            if (stuck && _peekUntil == 0 && WindowOpen && idle >= _settings.IdleAdvisorSeconds && now >= _restUntil)
            {
                _peekUntil = now + (float)_settings.AdvisorShowSeconds;
                _restUntil = now + (float)_settings.AdvisorRestSeconds;
                _speakers.TryGet(ADVISOR, out var advisor);
                _view.Peek.Show(advisor?.Picture(""), _localizer.Tr("Need a hand?"));
            }

            if (_peekUntil != 0 && (now >= _peekUntil || !stuck)) HidePeek();
        }

        // Isolde offers help only early on: until the quest the settings name is behind the player.
        private bool WindowOpen
        {
            get
            {
                var items = _quests.Items;
                for (var i = 0; i < items.Count; i++)
                    if (items[i].Id == _settings.UntilQuest) return _chain.Index <= i;
                return true;
            }
        }

        private void OnFingerDown(LeanFinger finger) => _lastActivity = Time.unscaledTime;

        private void OnPeekTapped()
        {
            HidePeek();
            _lastActivity = Time.unscaledTime;
            if (_chain.Active != null) _focus.Show(_chain.Active);
        }

        private void HidePeek()
        {
            _peekUntil = 0;
            _view.Peek.Hide();
        }
    }
}
