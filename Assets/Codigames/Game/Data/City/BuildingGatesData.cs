using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingGatesData : IBuildingGates
    {
        [Tooltip("By Townhall level, from 1. Empty = unlimited.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _maxCountPerTownhallLevel = new();
        [Tooltip("Entry 0 is what reaching level 2 asks.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _requiredTownhallLevelPerLevel = new();
        [Tooltip("Entry 0 is what reaching level 2 asks.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _requiredPopulationPerLevel = new();

        public IReadOnlyList<int> MaxCountPerTownhallLevel => _maxCountPerTownhallLevel;
        public IReadOnlyList<int> RequiredTownhallLevelPerLevel => _requiredTownhallLevelPerLevel;
        public IReadOnlyList<int> RequiredPopulationPerLevel => _requiredPopulationPerLevel;
    }
}
