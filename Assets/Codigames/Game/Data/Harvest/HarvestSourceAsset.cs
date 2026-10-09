using Codigames.Kingdom.Harvest;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "HarvestSource", menuName = "Kingdom/Data/Harvest Source")]
    public class HarvestSourceAsset : DefinitionAsset, IHarvestSource
    {
        [SerializeField, Required, Tooltip("What it pays: bushes, game and shoals all pay Food.")] private string _currency;

        [BoxGroup("The strike"), SuffixLabel("units")]
        [SerializeField, MinValue(0.01)] private double _unitsPerStrike = 1;
        [BoxGroup("The strike"), SuffixLabel("s")]
        [SerializeField, MinValue(0.01)] private double _secondsPerStrike = 10;

        [BoxGroup("The depot"), SuffixLabel("units"), Tooltip("0 = bedrock: never runs out.")]
        [SerializeField, MinValue(0)] private double _stock = 10;
        [BoxGroup("The depot"), SuffixLabel("s"), Tooltip("Grows back full in place after this; 0 = finite.")]
        [SerializeField, MinValue(0)] private double _recoverySeconds;
        [BoxGroup("The depot"), SuffixLabel("s"), Tooltip("Finite: comes back next to where it stood after this; 0 = never.")]
        [SerializeField, MinValue(0)] private double _respawnSeconds;
        [BoxGroup("The depot"), SuffixLabel("s"), Tooltip("Planted or moved, it grows this long before it can be taken from; 0 = none.")]
        [SerializeField, MinValue(0)] private double _growSeconds;

        public string Currency => _currency;
        public double UnitsPerStrike => _unitsPerStrike;
        public double SecondsPerStrike => _secondsPerStrike;
        public double Stock => _stock;
        public double RecoverySeconds => _recoverySeconds;
        public double RespawnSeconds => _respawnSeconds;
        public double GrowSeconds => _growSeconds;
    }
}
