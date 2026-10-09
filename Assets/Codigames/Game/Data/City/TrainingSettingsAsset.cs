using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [CreateAssetMenu(fileName = "Training", menuName = "Kingdom/Data/Villager Training Settings")]
    public class TrainingSettingsAsset : DataSettings, ITrainingSettings
    {
        [BoxGroup("Cost"), Tooltip("Food, by the villager's place in the town (population plus queued), from the first.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private List<double> _costFirst = new();
        [BoxGroup("Cost"), Tooltip("Past the list, each villager costs this many times the one before.")]
        [SerializeField, MinValue(1)] private double _costGrowth = 1.1;

        [BoxGroup("Time"), SuffixLabel("s")]
        [SerializeField, MinValue(1)] private double _seconds = 20;
        [BoxGroup("Time"), Tooltip("How much longer each place in the town makes a villager's wait.")]
        [SerializeField, MinValue(1)] private double _secondsGrowth = 1.07;

        public IReadOnlyList<double> CostFirst => _costFirst;
        public double CostGrowth => _costGrowth;
        public double Seconds => _seconds;
        public double SecondsGrowth => _secondsGrowth;
    }
}
