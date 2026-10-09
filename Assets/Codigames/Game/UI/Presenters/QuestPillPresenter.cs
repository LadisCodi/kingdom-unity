using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Quests;
using Codigames.Game.UI.Hud;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Quests;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Research;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // Keeps the tracker to the chain: the active quest's name, line, mark and progress; a ding the moment its goal is
    // met; the scroll rolling up on a claim and the next unrolling; hidden while a menu covers the map. A tap claims
    // a quest that is done, or points at its goal. Persistent.
    public class QuestPillPresenter : AbstractMenuPresenter<QuestPill>, ITickable
    {
        private const float REFRESH_SECONDS = 0.2f;
        private const int BETWEEN_MS = 500;
        private const float FILL_SECONDS = 0.9f;

        private readonly QuestChain _chain;
        private readonly QuestProse _prose;
        private readonly QuestFocus _focus;
        private readonly UIManager _ui;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly RewardFlight _flight;
        private readonly RewardFragments _fragments;

        private int _shown = -1;
        private bool _wasComplete;
        private bool _busy;
        private float _sinceRefresh;

        public QuestPillPresenter(IMenuViewFactory views, QuestChain chain, QuestProse prose, QuestFocus focus, UIManager ui, ICurrencyIcons icons,
            NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds, RewardFlight flight, RewardFragments fragments)
            : base(views)
        {
            _chain = chain;
            _prose = prose;
            _focus = focus;
            _ui = ui;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
        }

        protected override void BindInternal(QuestPill view)
        {
            _ui.MenuWillShow += OnMenus;
            _ui.MenuHidden += OnMenus;
            Refresh();
        }

        protected override void UnbindInternal(QuestPill view)
        {
            _ui.MenuWillShow -= OnMenus;
            _ui.MenuHidden -= OnMenus;
        }

        protected override void SubscribeToViewEventsInternal(QuestPill view) => view.Tapped += OnTapped;

        protected override void UnsubscribeFromViewEventsInternal(QuestPill view) => view.Tapped -= OnTapped;

        public void Tick()
        {
            if (!IsShown) return;

            // A quest that claims itself does so the moment it is done.
            if (!_busy && _chain.ClaimIfAuto(_clock.NowMs)) _sounds.Play(SoundIds.QUEST);

            _sinceRefresh += Time.unscaledDeltaTime;
            if (_sinceRefresh < REFRESH_SECONDS) return;

            _sinceRefresh = 0;
            Refresh();
        }

        private void OnMenus(IMenuPresenter menu) => View.SetHidden(_ui.HasOverlayOpen || _chain.Active == null);

        private void OnTapped()
        {
            if (_busy || _chain.Active is not { } quest) return;

            if (!_chain.IsComplete(quest))
            {
                _focus.Show(quest);
                return;
            }

            var haul = new Dictionary<string, double>(quest.Reward);
            if (_chain.Claim(_clock.NowMs) != QuestClaim.Claimed) return;

            _sounds.Play(_chain.Active == null ? SoundIds.CHAIN_FINISHED : SoundIds.QUEST);
            if (quest.RewardKnowledge > 0) haul[KnowledgeBar.KNOWLEDGE] = quest.RewardKnowledge;
            var from = RectTransformUtility.WorldToScreenPoint(null, View.Scroll.position);
            _flight.Fly(haul, from, (c, a) => _fragments.For(c, a, false));
            Refresh();
        }

        private async void Refresh()
        {
            if (_busy) return;

            var quest = _chain.Active;
            View.SetHidden(_ui.HasOverlayOpen || quest == null);
            if (quest == null)
            {
                if (_shown >= 0) await RollAway();
                return;
            }

            if (_chain.Index != _shown)
            {
                if (_shown >= 0) await HandOver();
                else Fill(quest);
                return;
            }

            Live(quest);
        }

        private void Fill(IQuestDefinition quest)
        {
            _shown = _chain.Index;
            _wasComplete = _chain.IsComplete(quest);
            View.Show(_localizer.Tr(quest.Name), _prose.Line(quest), (quest as QuestAsset)?.Mark);
            Live(quest);
        }

        private void Live(IQuestDefinition quest)
        {
            var value = System.Math.Min(_chain.Value(quest), quest.GoalAmount);
            var complete = _chain.IsComplete(quest);
            View.SetProgress((float)(value / quest.GoalAmount), $"{_numbers.Exact(value)}/{_numbers.Exact(quest.GoalAmount)}");

            // The moment the goal is met, a ding — before any claim.
            if (complete && !_wasComplete) _sounds.Play(SoundIds.QUEST_COMPLETE);
            _wasComplete = complete;
            View.SetDone(complete, Rewards(quest));
        }

        private IReadOnlyList<(Sprite, string)> Rewards(IQuestDefinition quest)
        {
            var rewards = quest.Reward.Where(r => r.Value > 0).Select(r => (_icons.IconOf(r.Key), _numbers.Exact(r.Value))).ToList();
            if (quest.RewardKnowledge > 0) rewards.Add((_icons.IconOf(KnowledgeBar.KNOWLEDGE), _numbers.Exact(quest.RewardKnowledge)));
            return rewards;
        }

        // The claimed scroll rolls up; after a beat the next unrolls.
        private async System.Threading.Tasks.Task HandOver()
        {
            _busy = true;
            try
            {
                _sounds.Play(SoundIds.SCROLL_CLOSE);
                await View.RollUp();
                await System.Threading.Tasks.Task.Delay(BETWEEN_MS);
                var next = _chain.Active;
                if (next == null)
                {
                    _shown = -1;
                    View.SetHidden(true);
                    return;
                }

                Fill(next);
                _sounds.Play(SoundIds.SCROLL_OPEN);

                // One that arrives already done unrolls on its running face, empty, and fills before it turns to Claim.
                if (!_chain.IsComplete(next))
                {
                    await View.Unroll();
                    return;
                }

                View.SetDone(false, Rewards(next));
                View.SetProgress(0, $"0/{_numbers.Exact(next.GoalAmount)}");
                await View.Unroll();
                await View.FillUp(next.GoalAmount, v => $"{_numbers.Exact(v)}/{_numbers.Exact(next.GoalAmount)}", FILL_SECONDS);
                _sounds.Play(SoundIds.QUEST_COMPLETE);
                _wasComplete = true;
                View.SetDone(true, Rewards(next));
            }
            finally
            {
                View.Settle();
                _busy = false;
            }
        }

        private async System.Threading.Tasks.Task RollAway()
        {
            _busy = true;
            try
            {
                await View.RollUp();
                _shown = -1;
                View.SetHidden(true);
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
