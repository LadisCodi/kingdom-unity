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

        [Header("Crew")]
        [Tooltip("The harvest sources its crew works; empty for a building with no crew.")]
        [SerializeField] private List<string> _harvestSources = new();
        [SerializeField, MinValue(0), Tooltip("The Harmony it supplies standing: a decoration's.")] private double _harmonySupply;
        [Tooltip("The Harmony it demands at each level from 1: a total, not a step."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _harmonyCostPerLevel = new();
        [SerializeField, Tooltip("The feature it plants instead of standing (Crops); empty for a building.")] private string _plants;
        [SerializeField, Tooltip("The good a workshop makes; empty for a building that is not one.")] private string _produces;
        [Header("Army")]
        [SerializeField, Tooltip("The units a military hall trains.")] private List<string> _trains = new();
        [Tooltip("The army a hall allows, by level from 1: a total."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _armyCapPerLevel = new();
        [Tooltip("The wounded an Infirmary keeps, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _bedsPerLevel = new();
        [Tooltip("A Tavern's raise on every Hero XP paid, a percent by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _heroXpBonusPerLevel = new();
        [Tooltip("How many goods a workshop may hold queued, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _queueLengthPerLevel = new();
        [Tooltip("How many villagers may work for it, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _maxWorkersPerLevel = new();
        [Tooltip("How far round its footprint its crew reaches, in rings, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _influenceRadiusPerLevel = new();
        [Tooltip("How much faster its crew swings than the ground's rhythm, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _strikeSpeedPerLevel = new();
        [Tooltip("Units each delivery carries on top of the ground's, by level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _extraUnitsPerDeliveryPerLevel = new();

        public IReadOnlyList<double> GoldPerMinutePerLevel => _goldPerMinutePerLevel;
        public IReadOnlyList<string> HarvestSources => _harvestSources;
        public double HarmonySupply => _harmonySupply;
        public IReadOnlyList<double> HarmonyCostPerLevel => _harmonyCostPerLevel;
        public string Plants => string.IsNullOrEmpty(_plants) ? null : _plants;
        public string Produces => string.IsNullOrEmpty(_produces) ? null : _produces;
        public IReadOnlyList<string> Trains => _trains;
        public IReadOnlyList<int> ArmyCapPerLevel => _armyCapPerLevel;
        public IReadOnlyList<int> BedsPerLevel => _bedsPerLevel;
        public IReadOnlyList<double> HeroXpBonusPerLevel => _heroXpBonusPerLevel;
        public IReadOnlyList<int> QueueLengthPerLevel => _queueLengthPerLevel;
        public IReadOnlyList<int> MaxWorkersPerLevel => _maxWorkersPerLevel;
        public IReadOnlyList<int> InfluenceRadiusPerLevel => _influenceRadiusPerLevel;
        public IReadOnlyList<double> StrikeSpeedPerLevel => _strikeSpeedPerLevel;
        public IReadOnlyList<double> ExtraUnitsPerDeliveryPerLevel => _extraUnitsPerDeliveryPerLevel;
        public IReadOnlyList<double> StorageCapacityPerLevel => _storageCapacityPerLevel;
        public IReadOnlyList<int> PopulationCapacityPerLevel => _populationCapacityPerLevel;
        public IReadOnlyList<double> TaxBonusPerLevel => _taxBonusPerLevel;
    }
}
