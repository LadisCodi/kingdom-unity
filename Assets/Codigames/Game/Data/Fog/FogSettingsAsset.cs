using System.Collections.Generic;
using Codigames.Kingdom.Fog;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Fog
{
    [CreateAssetMenu(fileName = "Fog", menuName = "Kingdom/Data/Fog Settings")]
    public class FogSettingsAsset : DataSettings, IFogSettings
    {
        [BoxGroup("Price"), Tooltip("A cell's Gold by its ring from the Townhall, from ring 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _costPerRing = new();
        [BoxGroup("Price"), Tooltip("Past the last ring, the price grows this much a ring.")]
        [SerializeField, MinValue(1)] private double _fallbackGrowth = 1.37;
        [BoxGroup("Price"), SuffixLabel("Gold")]
        [SerializeField, MinValue(0)] private double _minCost = 1;
        [BoxGroup("Price"), Tooltip("The map gets dearer as it is revealed: this growth once per this many cells.")]
        [SerializeField, MinValue(0)] private int _countStep = 10;
        [BoxGroup("Price")]
        [SerializeField, MinValue(1)] private double _countGrowth = 1.05;

        [BoxGroup("Clearing"), Tooltip("Taps that clear a cell, each charging its share.")]
        [SerializeField, MinValue(1)] private int _tapsToReveal = 5;
        [BoxGroup("Clearing"), Tooltip("Rings from the Townhall a cell may be paid for, by its level from 1."), ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<int> _reachPerTownhallLevel = new();
        [SerializeField, MinValue(0), SuffixLabel("cells"), Tooltip("What claiming a landmark discovers round it.")]
        private int _claimDiscoverRadius = 5;

        public IReadOnlyList<double> CostPerRing => _costPerRing;
        public double FallbackGrowth => _fallbackGrowth;
        public int TapsToReveal => _tapsToReveal;
        public double MinCost => _minCost;
        public int CountStep => _countStep;
        public double CountGrowth => _countGrowth;
        public IReadOnlyList<int> ReachPerTownhallLevel => _reachPerTownhallLevel;
        public int ClaimDiscoverRadius => _claimDiscoverRadius;
    }
}
