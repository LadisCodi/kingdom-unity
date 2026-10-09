using Codigames.Kingdom.Economy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Economy", menuName = "Kingdom/Data/Economy Settings")]
    public class EconomySettingsAsset : DataSettings, IEconomySettings
    {
        [SerializeField, MinValue(0), SuffixLabel("Gold / villager / min"), Tooltip("The rent every housed villager pays.")]
        private double _goldPerPopulationPerMinute = 30;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("A store is ready once it holds this many seconds of its building's making.")]
        private double _collectSeconds = 30;

        public double GoldPerPopulationPerMinute => _goldPerPopulationPerMinute;
        public double CollectSeconds => _collectSeconds;
    }
}
