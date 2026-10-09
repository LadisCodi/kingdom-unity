using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Fog;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Fog
{
    [CreateAssetMenu(fileName = "Treasure", menuName = "Kingdom/Data/Treasure Settings")]
    public class TreasureSettingsAsset : DataSettings, ITreasureSettings
    {
        [BoxGroup("The pace"), SerializeField, MinValue(1), SuffixLabel("cells paid for")] private int _everyReveals = 5;

        [BoxGroup("What one pays"), SerializeField, MinValue(0), SuffixLabel("s of production")] private double _workSeconds = 120;
        [BoxGroup("What one pays"), SerializeField] private List<Amount> _floor = new();
        [BoxGroup("What one pays"), SerializeField, Tooltip("How likely each coin is; Stone only once it is on the plank.")]
        private List<Amount> _weights = new();
        [BoxGroup("What one pays"), SerializeField, MinValue(0), SuffixLabel("Knowledge")] private double _knowledge = 1;

        [BoxGroup("The first"), SerializeField] private string _firstCoin = "Gold";
        [BoxGroup("The first"), SerializeField, MinValue(1)] private double _firstAmount = 20;

        private Dictionary<string, double> _floorLookup;
        private Dictionary<string, double> _weightLookup;

        public int EveryReveals => _everyReveals;
        public double WorkSeconds => _workSeconds;
        public IReadOnlyDictionary<string, double> Floor => _floorLookup ??= _floor.ToDictionary(a => a.Id, a => a.Value);
        public IReadOnlyDictionary<string, double> Weights => _weightLookup ??= _weights.ToDictionary(a => a.Id, a => a.Value);
        public double Knowledge => _knowledge;
        public string FirstCoin => _firstCoin;
        public double FirstAmount => _firstAmount;

        private void OnValidate()
        {
            _floorLookup = null;
            _weightLookup = null;
        }
    }
}
