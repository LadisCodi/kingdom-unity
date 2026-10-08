using System;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingDurationData : IBuildingDuration
    {
        [SerializeField, MinValue(0), SuffixLabel("s")] private double _buildSeconds;
        [SerializeField] private double _buildCountGrowth = 1;
        [SerializeField] private double _buildDistanceGrowth = 1;
        [SerializeField, MinValue(0), SuffixLabel("s")] private double _upgradeSeconds;
        [SerializeField] private double _upgradeLevelGrowth = 1;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("0 = no late levels.")] private double _lateUpgradeSeconds;
        [SerializeField] private double _lateUpgradeLevelGrowth = 1;

        public double BuildSeconds => _buildSeconds;
        public double BuildCountGrowth => _buildCountGrowth;
        public double BuildDistanceGrowth => _buildDistanceGrowth;
        public double UpgradeSeconds => _upgradeSeconds;
        public double UpgradeLevelGrowth => _upgradeLevelGrowth;
        public double LateUpgradeSeconds => _lateUpgradeSeconds;
        public double LateUpgradeLevelGrowth => _lateUpgradeLevelGrowth;
    }
}
