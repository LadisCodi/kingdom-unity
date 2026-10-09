using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingFogData : IBuildingFog
    {
        [SerializeField, MinValue(0), Tooltip("Rings round its footprint it reveals when finished.")]
        private int _revealRadius;
        [Tooltip("By level, from 1, when the level changes it; empty = the radius above at every level.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _revealRadiusPerLevel = new();
        [SerializeField, MinValue(0), Tooltip("Rings round its footprint it brings into view.")]
        private int _discoverRadius;

        public int RevealRadius => _revealRadius;
        public IReadOnlyList<int> RevealRadiusPerLevel => _revealRadiusPerLevel;
        public int DiscoverRadius => _discoverRadius;
    }
}
