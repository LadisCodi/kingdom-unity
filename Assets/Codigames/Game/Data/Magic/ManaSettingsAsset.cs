using Codigames.Kingdom.Magic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Magic
{
    [CreateAssetMenu(fileName = "Mana", menuName = "Kingdom/Data/Mana Settings")]
    public class ManaSettingsAsset : DataSettings, IManaSettings, Kingdom.Store.IManaRefillPrice
    {
        [SerializeField, MinValue(1), SuffixLabel("Mana")] private double _baseCap = 100;
        [SerializeField, MinValue(0.1), SuffixLabel("Mana / h")] private double _basePerHour = 12;
        [SerializeField, MinValue(0), SuffixLabel("Mana"), Tooltip("A landmark claimed, or the Watchtower repaired, adds this for good.")]
        private double _landmarkCap = 10;
        [SerializeField, Tooltip("Gems each Gem refill of the pool costs, in order: a flask's worth is read off the first.")]
        private System.Collections.Generic.List<double> _gemRefillCosts = new() { 400, 600, 800, 1000, 2000 };

        public double BaseCap => _baseCap;
        public double BasePerHour => _basePerHour;
        public double LandmarkCap => _landmarkCap;
        public double FirstRefillGems => _gemRefillCosts.Count > 0 ? _gemRefillCosts[0] : 0;
    }
}
