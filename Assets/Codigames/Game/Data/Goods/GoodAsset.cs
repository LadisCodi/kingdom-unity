using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Goods;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Goods
{
    // A refined good or a precious material, whole: its name, picture, recipe and work.
    [CreateAssetMenu(fileName = "Good", menuName = "Kingdom/Data/Good")]
    public class GoodAsset : DefinitionAsset, IGoodDefinition
    {
        [SerializeField] private string _name;
        [SerializeField, PreviewField(64)] private Sprite _icon;
        [SerializeField, Range(1, 3)] private int _tier = 1;
        [SerializeField, Tooltip("A precious material: found in the world, never made.")] private bool _precious;
        [SerializeField, HideIf(nameof(_precious)), Tooltip("The coins one is made of.")] private List<Amount> _input = new();
        [SerializeField, HideIf(nameof(_precious)), MinValue(0)] private double _inputMana;
        [SerializeField, HideIf(nameof(_precious)), Tooltip("Another good it is made of; empty for none.")] private string _inputGood;
        [SerializeField, HideIf(nameof(_precious)), MinValue(0)] private int _inputGoodAmount;
        [SerializeField, HideIf(nameof(_precious)), MinValue(0), SuffixLabel("s"), Tooltip("How long one villager works at one.")]
        private double _workSeconds;

        [System.NonSerialized] private Dictionary<string, double> _inputLookup;

        public string Name => _name;
        public Sprite Icon => _icon;
        public int Tier => _tier;
        public bool Precious => _precious;
        public IReadOnlyDictionary<string, double> Input => _inputLookup ??= _input.ToDictionary(a => a.Id, a => a.Value);
        public double InputMana => _inputMana;
        public string InputGood => string.IsNullOrEmpty(_inputGood) ? null : _inputGood;
        public int InputGoodAmount => _inputGoodAmount;
        public double WorkSeconds => _workSeconds;

        public override IEnumerable<string> Problems()
        {
            if (_precious && (_input.Count > 0 || _workSeconds > 0)) yield return "A precious material is not made: no inputs, no work.";
            if (!_precious && _workSeconds <= 0) yield return "A good that is made needs its work time.";
            if (_inputGood == Id) yield return "A good cannot be made of itself.";
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _inputLookup = null;
        }
    }
}
