using Codigames.Kingdom.Economy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Currency", menuName = "Kingdom/Data/Currency")]
    public class CurrencyAsset : DefinitionAsset, ICurrencyDefinition
    {
        [BoxGroup("Rules")]
        [SerializeField] private CurrencyScope _scope;
        [BoxGroup("Rules")]
        [SerializeField, MinValue(0)] private double _start;
        [BoxGroup("Rules"), Tooltip("Off = no cap.")]
        [SerializeField] private bool _capped;
        [BoxGroup("Rules"), ShowIf(nameof(_capped)), MinValue(0)]
        [SerializeField] private double _cap;

        [BoxGroup("Presentation"), Tooltip("Shown on the header's resource plank.")]
        [SerializeField] private bool _primary;

        public CurrencyScope Scope => _scope;
        public double Start => _start;
        public double? Cap => _capped ? _cap : (double?)null;
        public bool Primary => _primary;
    }
}
