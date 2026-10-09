using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // A building, whole: its rules (what Kingdom reads) and what the player sees of it.
    [CreateAssetMenu(fileName = "Building", menuName = "Kingdom/Data/Building")]
    public class BuildingAsset : DefinitionAsset, IBuildingDefinition, IBuildingCard
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

        [TabGroup("Production"), HideLabel, InlineProperty]
        [SerializeField] private BuildingProductionData _production = new();

        [TabGroup("Fog"), HideLabel, InlineProperty]
        [SerializeField] private BuildingFogData _fog = new();

        [TabGroup("Presentation")]
        [SerializeField] private string _displayName;
        [TabGroup("Presentation"), Tooltip("The build card's one line.")]
        [SerializeField] private string _promise;
        [TabGroup("Presentation"), TextArea(2, 6)]
        [SerializeField] private string _description;
        [TabGroup("Presentation"), ValueDropdown(nameof(BuildTabs))]
        [SerializeField] private string _buildTab;
        [TabGroup("Presentation"), Tooltip("By tier: a level draws the highest tier at or below it.")]
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField] private List<LevelArt> _art = new();

        public int MaxLevel => _maxLevel;
        public int Width => _width;
        public int Height => _height;
        public bool Buildable => _buildable;
        public IBuildingCost Cost => _cost;
        public IBuildingDuration Duration => _duration;
        public IBuildingGates Gates => _gates;
        public IBuildingProduction Production => _production;
        public IBuildingFog Fog => _fog;

        public string DisplayName => _displayName;
        public string Promise => _promise;
        public string Description => _description;
        public string BuildTab => _buildTab;

        // The art a level draws: the highest tier at or below it, or the lowest tier when none is.
        public Sprite ArtFor(int level)
        {
            var tiers = _art.Where(t => t?.Sprite != null).OrderBy(t => t.FromLevel).ToList();
            if (tiers.Count == 0) return null;

            var reached = tiers.LastOrDefault(t => t.FromLevel <= level);
            return (reached ?? tiers[0]).Sprite;
        }

        private static IEnumerable<string> BuildTabs => new[] { "Economy", "Military", "Decoration" };

        public override IEnumerable<string> Problems() => BuildingRules.Problems(this);

        protected override void OnValidate()
        {
            base.OnValidate();
            _cost.Invalidate();
        }
    }
}
