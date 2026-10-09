using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingProductionData : IBuildingProduction
    {
        [Tooltip("Gold it makes of its own a minute, by level from 1, with nobody in it. Empty = none.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _goldPerMinutePerLevel = new();
        [Tooltip("Units its store holds, all currencies together, by level from 1. Empty = no store.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _storageCapacityPerLevel = new();
        [Tooltip("Villagers it houses, by level from 1.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _populationCapacityPerLevel = new();
        [Tooltip("How much more its residents pay, as a fraction of the base rent: a total at each level, from 1.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _taxBonusPerLevel = new();

        public IReadOnlyList<double> GoldPerMinutePerLevel => _goldPerMinutePerLevel;
        public IReadOnlyList<double> StorageCapacityPerLevel => _storageCapacityPerLevel;
        public IReadOnlyList<int> PopulationCapacityPerLevel => _populationCapacityPerLevel;
        public IReadOnlyList<double> TaxBonusPerLevel => _taxBonusPerLevel;
    }
}
