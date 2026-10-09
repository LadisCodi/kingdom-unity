using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Quests.State;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Timeline;

namespace Codigames.Kingdom.Quests
{
    // The quest chain: one active quest at a time, in the chain's order. Absolute goals are read off the kingdom;
    // relative ones count taps, collects and reveals from the quest's activation. Claiming pays the reward and
    // activates the next. A tutorial quest may rush the first house's rent at its own moment, so the chain is on
    // the timeline.
    public class QuestChain : ITimedSystem, IChainPosition
    {
        private const string GOLD = "Gold";
        private const string KNOWLEDGE_YIELD = "knowledgeYield";

        private readonly QuestState _state;
        private readonly ICatalog<IQuestDefinition> _quests;
        private readonly IQuestGoals _goals;
        private readonly ITreasury _treasury;
        private readonly CityState _city;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly Stores _stores;
        private readonly GroundState _ground;
        private readonly IBonuses _bonuses;

        public QuestChain(QuestState state, ICatalog<IQuestDefinition> quests, IQuestGoals goals, ITreasury treasury, CityState city,
            ICatalog<IBuildingDefinition> buildings, Stores stores, Harvesting harvesting, FogOfWar fog, GroundState ground,
            IBonuses bonuses = null)
        {
            _state = state;
            _quests = quests;
            _goals = goals;
            _treasury = treasury;
            _city = city;
            _buildings = buildings;
            _stores = stores;
            _ground = ground;
            _bonuses = bonuses;

            harvesting.Tapped += OnTapped;
            stores.Collected += OnCollected;
            fog.PaidReveal += OnPaidReveal;
        }

        // A quest was claimed; the next one (null past the end) is active.
        public event Action<IQuestDefinition, IQuestDefinition> Claimed;

        public IQuestDefinition Active => _state.Index < _quests.Items.Count ? _quests.Items[_state.Index] : null;

        public int Index => _state.Index;

        public double Value(IQuestDefinition quest) => quest.GoalType.IsRelative() ? _state.Progress : _goals.Value(quest);

        public bool IsComplete(IQuestDefinition quest) => Value(quest) >= quest.GoalAmount;

        // Starts the active quest's own clock if it has one and it is not running: at a kingdom's first second, or
        // for an older save.
        public void Wake(double now) => StampRush(now);

        public QuestClaim Claim(double now)
        {
            var quest = Active;
            if (quest == null) return QuestClaim.NoQuest;
            if (!IsComplete(quest)) return QuestClaim.NotComplete;

            foreach (var line in quest.Reward.Where(l => l.Value > 0)) _treasury.Add(line.Key, line.Value);
            if (quest.RewardKnowledge > 0)
            {
                var lump = Math.Max(0, Math.Round(quest.RewardKnowledge * _bonuses.Multiplier(KNOWLEDGE_YIELD), MidpointRounding.AwayFromZero));
                _treasury.Add(KnowledgeBar.KNOWLEDGE, lump);
            }

            _state.Index++;
            _state.Progress = 0;
            StampRush(now);
            Claimed?.Invoke(quest, Active);
            return QuestClaim.Claimed;
        }

        // Claims the active quest if it claims itself and is done.
        public bool ClaimIfAuto(double now) => Active is { AutoClaim: true } quest && IsComplete(quest) && Claim(now) == QuestClaim.Claimed;

        public double? NextBoundary(double after) => _state.RushAt > after ? _state.RushAt : null;

        public void ApplyDue(double time)
        {
            if (_state.RushAt == null || _state.RushAt > time) return;

            _state.RushAt = null;
            var quest = Active;
            if (_state.RushIndex != _state.Index || quest == null || quest.GoalTarget != GOLD) return;

            var need = quest.GoalAmount - _state.Progress;
            var house = _city.Districts.FirstOrDefault(d => d.Built && _buildings.Get(d.DefinitionId).Production.PopulationCapacityPerLevel.Count > 0
                                                            && _stores.Capacity(d) > 0);
            if (house == null || need <= 0) return;

            _stores.Settle(house, time);
            var room = _stores.Capacity(house) - _stores.Held(house, time);
            var add = Math.Min(room, need);
            if (add > 0) _stores.Deposit(house, GOLD, add);
        }

        public void RunUntil(double time) { }

        private void StampRush(double now)
        {
            var quest = Active;
            if (quest == null || quest.TutorialRentSeconds <= 0 || _state.RushIndex == _state.Index) return;

            _state.RushIndex = _state.Index;
            _state.RushAt = now + quest.TutorialRentSeconds * 1000;
        }

        private void OnTapped(Vector2Int cell, TapResult result)
        {
            if (Active is not { } quest) return;

            if (quest.GoalType == GoalType.CollectTaps) _state.Progress++;
            else if (quest.GoalType == GoalType.CollectResource && quest.GoalTarget == result.Currency) _state.Progress += result.Paid;
        }

        private void OnCollected(DistrictState district, IReadOnlyDictionary<string, double> moved, double now)
        {
            if (Active is { GoalType: GoalType.CollectResource } quest && moved.TryGetValue(quest.GoalTarget ?? "", out var amount))
                _state.Progress += amount;
        }

        // Counted at the reveal, the only moment that knows the feature: a berry bush eaten later must not undo it.
        private void OnPaidReveal(IReadOnlyList<Vector2Int> revealed, IReadOnlyList<Vector2Int> fresh)
        {
            if (Active is not { GoalType: GoalType.DiscoverFeature } quest) return;

            _state.Progress += revealed.Count(c => _ground.Features.TryGetValue(c, out var feature) && feature == quest.GoalTarget);
        }
    }
}
