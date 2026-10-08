using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // A building, whole: its rules (what Kingdom reads) and what the player sees of it.
    [CreateAssetMenu(fileName = "Building", menuName = "Kingdom/Data/Building")]
    public class BuildingAsset : DefinitionAsset, IBuildingDefinition
    {
        [TabGroup("Rules")]
        [SerializeField, Range(1, BuildingRules.MAX_LEVEL)] private int _maxLevel = 1;
        [TabGroup("Rules")]
        [SerializeField, MinValue(1)] private int _width = 1;
        [TabGroup("Rules")]
        [SerializeField, MinValue(1)] private int _height = 1;
        [TabGroup("Rules"), Tooltip("Off for what the city starts with.")]
        [SerializeField] private bool _buildable = true;

        [TabGroup("Cost"), HideLabel, InlineProperty]
        [SerializeField] private BuildingCostData _cost = new();

        [TabGroup("Duration"), HideLabel, InlineProperty]
        [SerializeField] private BuildingDurationData _duration = new();

        [TabGroup("Gates"), HideLabel, InlineProperty]
        [SerializeField] private BuildingGatesData _gates = new();

        [TabGroup("Presentation")]
        [SerializeField] private string _displayName;
        [TabGroup("Presentation"), Tooltip("The build card's one line.")]
        [SerializeField] private string _promise;
        [TabGroup("Presentation"), TextArea(2, 6)]
        [SerializeField] private string _description;
        [TabGroup("Presentation"), ValueDropdown(nameof(BuildTabs))]
        [SerializeField] private string _buildTab;

        public int MaxLevel => _maxLevel;
        public int Width => _width;
        public int Height => _height;
        public bool Buildable => _buildable;
        public IBuildingCost Cost => _cost;
        public IBuildingDuration Duration => _duration;
        public IBuildingGates Gates => _gates;

        public string DisplayName => _displayName;
        public string Promise => _promise;
        public string Description => _description;
        public string BuildTab => _buildTab;

        private static IEnumerable<string> BuildTabs => new[] { "Economy", "Military", "Decoration" };

        public override IEnumerable<string> Problems() => BuildingRules.Problems(this);

        protected override void OnValidate()
        {
            base.OnValidate();
            _cost.Invalidate();
        }
    }
}
