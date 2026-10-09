using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Lairs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Lairs
{
    // The lairs' garrisons by tier, the raids' pace and size, and what a path pays (Docs/features/18-garrisons-and-raids.md).
    [CreateAssetMenu(fileName = "Lairs", menuName = "Kingdom/Data/Lair Settings")]
    public class LairSettingsAsset : DataSettings, ILairSettings
    {
        [Serializable]
        public class GarrisonData
        {
            [MinValue(1)] public int Fights = 3;
            [MinValue(0), SuffixLabel("s of the city's making")] public double TakeSeconds = 300;
            [MinValue(0)] public double HeroXp = 500;
            public List<Amount> RewardItems = new();
        }

        [BoxGroup("Garrisons"), SerializeField, ListDrawerSettings(ShowIndexLabels = true), Tooltip("Tier 1 first.")]
        private List<GarrisonData> _garrisons = new();

        [BoxGroup("Raids"), SerializeField, Range(0, 1), Tooltip("A raid takes at most this share of what the stores hold.")]
        private double _takeFractionMax = 0.5;
        [BoxGroup("Raids"), SerializeField, MinValue(1)] private int _raidsPerDay = 3;
        [BoxGroup("Raids"), SerializeField, Range(0, 24), SuffixLabel("h, local")] private double _windowStartHour = 9;
        [BoxGroup("Raids"), SerializeField, Range(0, 24), SuffixLabel("h, local")] private double _windowEndHour = 23;

        [BoxGroup("The path"), SerializeField, Range(0, 1), Tooltip("The first fight's power, as a share of the guard's.")]
        private double _firstFightPower = 0.5;
        [BoxGroup("The path"), SerializeField, MinValue(0), SuffixLabel("Knowledge")] private double _firstClearKnowledge = 3;

        private List<Garrison> _lookup;

        public IReadOnlyList<Garrison> Garrisons => _lookup ??= _garrisons.Select(g => new Garrison(g.Fights, g.TakeSeconds, g.HeroXp,
            g.RewardItems.ToDictionary(a => a.Id, a => (int)a.Value))).ToList();
        public double TakeFractionMax => _takeFractionMax;
        public int RaidsPerDay => _raidsPerDay;
        public double WindowStartHour => _windowStartHour;
        public double WindowEndHour => _windowEndHour;
        public double FirstFightPower => _firstFightPower;
        public double FirstClearKnowledge => _firstClearKnowledge;

        public override IEnumerable<string> Problems()
        {
            if (_garrisons.Count == 0) yield return "No garrisons: every lair needs its tier's.";
        }

        private void OnValidate() => _lookup = null;
    }
}
