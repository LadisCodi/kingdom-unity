using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingCostData : IBuildingCost
    {
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "")]
        [SerializeField] private List<LevelCostData> _perLevel = new();
        [SerializeField, MinValue(0)] private double _instanceLinearGrowth = 2;
        [SerializeField, MinValue(1)] private double _instanceExponentialGrowth = 1.2;

        public IReadOnlyList<ILevelCost> PerLevel => _perLevel;
        public double InstanceLinearGrowth => _instanceLinearGrowth;
        public double InstanceExponentialGrowth => _instanceExponentialGrowth;

        public void Invalidate()
        {
            foreach (var level in _perLevel) level?.Invalidate();
        }
    }
}
