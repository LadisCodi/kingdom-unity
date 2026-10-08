using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [CreateAssetMenu(fileName = "Construction", menuName = "Kingdom/Data/Construction Settings")]
    public class ConstructionSettingsAsset : DataSettings, IConstructionSettings
    {
        [SerializeField, Required] private BuildingAsset _townhall;
        [SerializeField, MinValue(2)] private int _lateUpgradeFromLevel = 6;

        [BoxGroup("Builders")]
        [SerializeField, MinValue(1)] private int _startBuilders = 1;
        [BoxGroup("Builders")]
        [SerializeField, MinValue(1)] private int _maxBuilders = 4;
        [BoxGroup("Builders"), SuffixLabel("Gems")]
        [SerializeField, MinValue(0)] private double _builderGemCostBase = 2500;
        [BoxGroup("Builders")]
        [SerializeField, MinValue(1)] private double _builderGemCostGrowth = 2;

        public IBuildingDefinition Townhall => _townhall;
        public int LateUpgradeFromLevel => _lateUpgradeFromLevel;
        public int StartBuilders => _startBuilders;
        public int MaxBuilders => _maxBuilders;
        public double BuilderGemCostBase => _builderGemCostBase;
        public double BuilderGemCostGrowth => _builderGemCostGrowth;

        public override IEnumerable<string> Problems() => ConstructionRules.Problems(this);
    }
}
