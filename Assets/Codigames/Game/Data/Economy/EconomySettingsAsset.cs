using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Economy", menuName = "Kingdom/Data/Economy Settings")]
    public class EconomySettingsAsset : DataSettings, IEconomySettings, IWorkerSettings, IBagSettings, IRushSettings
    {
        [SerializeField, MinValue(0), SuffixLabel("Gold / villager / min"), Tooltip("The rent every housed villager pays.")]
        private double _goldPerPopulationPerMinute = 30;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("A store is ready once it holds this many seconds of its building's making.")]
        private double _collectSeconds = 30;

        [SerializeField, MinValue(0.1), SuffixLabel("cells / s"), Tooltip("How fast a villager walks.")]
        private double _moveSpeedTilesPerSecond = 1;

        [SerializeField, MinValue(0), SuffixLabel("units / h"), Tooltip("What a chest pays an hour of a coin the city barely makes yet.")]
        private double _chestFloorPerHour = 60;

        [SerializeField, MinValue(1), SuffixLabel("s / Gem"), Tooltip("Skipping a wait costs a Gem this many seconds, never less than one.")]
        private double _secondsPerGem = 5;

        public double GoldPerPopulationPerMinute => _goldPerPopulationPerMinute;
        public double SecondsPerGem => _secondsPerGem;
        public double ChestFloorPerHour => _chestFloorPerHour;
        public double MoveSpeedTilesPerSecond => _moveSpeedTilesPerSecond;
        public double CollectSeconds => _collectSeconds;
    }
}
