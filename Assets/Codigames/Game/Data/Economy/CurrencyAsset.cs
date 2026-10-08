using Codigames.Kingdom.Economy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Currency", menuName = "Kingdom/Data/Currency")]
    public class CurrencyAsset : DefinitionAsset, ICurrencyDefinition, IPlankCurrency
    {
        [BoxGroup("Rules")]
        [SerializeField] private CurrencyScope _scope;
        [BoxGroup("Rules")]
        [SerializeField, MinValue(0)] private double _start;
        [BoxGroup("Rules"), Tooltip("Off = no cap.")]
        [SerializeField] private bool _capped;
        [BoxGroup("Rules"), ShowIf(nameof(_capped)), MinValue(0)]
        [SerializeField] private double _cap;

        [BoxGroup("Presentation"), PreviewField(48), Required]
        [SerializeField] private Sprite _icon;
        [BoxGroup("Presentation"), Tooltip("Where it reads on the header's plank.")]
        [SerializeField] private PlankPlace _place;
        [BoxGroup("Presentation"), Tooltip("The store sells it: its slot on the plank carries a + that opens the store.")]
        [SerializeField] private bool _sold;

        public CurrencyScope Scope => _scope;
        public double Start => _start;
        public double? Cap => _capped ? _cap : (double?)null;
        public Sprite Icon => _icon;
        public PlankPlace Place => _place;
        public bool Sold => _sold;
    }
}
