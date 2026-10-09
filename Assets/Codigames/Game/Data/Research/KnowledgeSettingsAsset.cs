using Codigames.Kingdom.Research;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    [CreateAssetMenu(fileName = "Knowledge", menuName = "Kingdom/Data/Knowledge Settings")]
    public class KnowledgeSettingsAsset : DataSettings, IKnowledgeSettings
    {
        [BoxGroup("The bar"), SerializeField, MinValue(0.1), SuffixLabel("Knowledge / h")] private double _perHour = 1;
        [BoxGroup("The bar"), SerializeField, MinValue(1), SuffixLabel("Knowledge")] private double _cap = 10;

        [BoxGroup("Buying it"), SerializeField, MinValue(1), SuffixLabel("Gold"), Tooltip("The nth point ever bought with Gold costs base × n^exponent.")]
        private double _goldPriceBase = 400;
        [BoxGroup("Buying it"), SerializeField, MinValue(1)] private double _goldPriceExponent = 2;
        [BoxGroup("Buying it"), SerializeField, MinValue(1), SuffixLabel("Gems")] private double _gemsPerPoint = 200;
        [BoxGroup("Lumps"), SerializeField, MinValue(0), SuffixLabel("Knowledge")] private double _landmarkClaimLump = 3;

        public double PerHour => _perHour;
        public double Cap => _cap;
        public double GoldPriceBase => _goldPriceBase;
        public double GoldPriceExponent => _goldPriceExponent;
        public double GemsPerPoint => _gemsPerPoint;
        public double LandmarkClaimLump => _landmarkClaimLump;
    }
}
