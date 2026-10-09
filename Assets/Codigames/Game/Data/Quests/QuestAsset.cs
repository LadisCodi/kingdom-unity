using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Quests;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Quests
{
    // One step of the chain: its name (flavour; the line is generated from the goal), its goal and what it pays.
    [CreateAssetMenu(fileName = "Quest", menuName = "Kingdom/Data/Quest")]
    public class QuestAsset : DefinitionAsset, IQuestDefinition
    {
        [SerializeField] private string _name;
        [SerializeField, PreviewField(48), Tooltip("The goal's mark on the tracker.")] private Sprite _mark;

        [BoxGroup("Goal"), SerializeField] private GoalType _goalType;
        [BoxGroup("Goal"), SerializeField, Tooltip("A building or a group (AnyDecoration), a currency, a technology, a feature, a landmark kind; empty for none.")]
        private string _goalTarget;
        [BoxGroup("Goal"), SerializeField, MinValue(1)] private double _goalAmount = 1;
        [BoxGroup("Goal"), SerializeField, MinValue(0), ShowIf(nameof(_goalType), GoalType.UpgradeDistrict)] private int _goalLevel;

        [BoxGroup("Reward"), SerializeField, Tooltip("Gold, Food, Wood, Stone, Mana, Gems, Stardust.")] private List<Amount> _reward = new();
        [BoxGroup("Reward"), SerializeField, MinValue(0), SuffixLabel("Knowledge")] private double _rewardKnowledge;
        [BoxGroup("Reward"), SerializeField] private List<Amount> _rewardItems = new();

        [BoxGroup("Pacing"), SerializeField, Tooltip("Claims itself the moment it is done.")] private bool _autoClaim;
        [BoxGroup("Pacing"), SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("Tops the first house up with the Gold it asks, this long after it starts; 0 = never.")]
        private double _tutorialRentSeconds;

        private Dictionary<string, double> _rewardLookup;
        private Dictionary<string, double> _itemLookup;

        public string Name => _name;
        public Sprite Mark => _mark;
        public GoalType GoalType => _goalType;
        public string GoalTarget => string.IsNullOrEmpty(_goalTarget) ? null : _goalTarget;
        public double GoalAmount => _goalAmount;
        public int GoalLevel => _goalLevel;
        public IReadOnlyDictionary<string, double> Reward => _rewardLookup ??= _reward.ToDictionary(a => a.Id, a => a.Value);
        public double RewardKnowledge => _rewardKnowledge;
        public IReadOnlyDictionary<string, double> RewardItems => _itemLookup ??= _rewardItems.ToDictionary(a => a.Id, a => a.Value);
        public bool AutoClaim => _autoClaim;
        public double TutorialRentSeconds => _tutorialRentSeconds;

        protected override void OnValidate()
        {
            base.OnValidate();
            _rewardLookup = null;
            _itemLookup = null;
        }
    }
}
