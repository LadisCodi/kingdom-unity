using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Research;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // A technology, whole: where it sits on its book's page, what it costs, what it needs and what it opens or
    // moves, and how it is shown.
    [CreateAssetMenu(fileName = "Technology", menuName = "Kingdom/Data/Technology")]
    public class TechnologyAsset : DefinitionAsset, ITechnology, ITechnologyCard
    {
        [TabGroup("Page")]
        [SerializeField] private string _tome = "Kingdom";
        [TabGroup("Page"), Tooltip("Off: not on a page, so not in the game.")]
        [SerializeField] private bool _placed = true;
        [TabGroup("Page"), MinValue(1), ShowIf(nameof(_placed))]
        [SerializeField] private int _era = 1;
        [TabGroup("Page"), MinValue(0), ShowIf(nameof(_placed))]
        [SerializeField] private int _row;
        [TabGroup("Page"), Range(0, 2), ShowIf(nameof(_placed))]
        [SerializeField] private int _column;
        [TabGroup("Page"), Tooltip("All in the row above.")]
        [SerializeField] private List<string> _requires = new();

        [TabGroup("Cost"), MinValue(0), SuffixLabel("Knowledge")]
        [SerializeField] private double _knowledge = 1;
        [TabGroup("Cost"), Tooltip("Paid when its Knowledge is in: Gold, and on some cards Wood, Stone or Food.")]
        [SerializeField] private List<Amount> _price = new();
        [TabGroup("Cost"), Tooltip("Refined goods and precious materials, paid with the Gold.")]
        [SerializeField] private List<Amount> _goods = new();
        [TabGroup("Cost"), MinValue(0), Tooltip("Precious materials of any kind the world holds.")]
        [SerializeField] private int _anyPrecious;

        [TabGroup("Effect")]
        [SerializeField] private TechKind _kind;
        [TabGroup("Effect"), ShowIf(nameof(_kind), TechKind.Unlock)]
        [SerializeField] private List<UnlockData> _unlocks = new();
        [TabGroup("Effect"), ShowIf(nameof(_kind), TechKind.Bonus)]
        [SerializeField] private List<EffectData> _effects = new();
        [TabGroup("Effect"), Tooltip("On the tree and researchable, but nothing reads it yet.")]
        [SerializeField] private bool _planned;

        [TabGroup("Presentation")]
        [SerializeField] private string _displayName;
        [TabGroup("Presentation"), PreviewField(48)]
        [SerializeField] private Sprite _icon;
        [TabGroup("Presentation"), TextArea(2, 6), ShowIf(nameof(_kind), TechKind.Mechanic)]
        [SerializeField] private string _description;

        private Dictionary<string, double> _priceLookup;
        private Dictionary<string, double> _goodsLookup;
        private List<TechUnlock> _unlockList;
        private List<TechEffect> _effectList;

        public string Tome => _tome;
        public int Era => _era;
        public int Row => _row;
        public int Column => _column;
        public bool IsPlaced => _placed;
        public IReadOnlyList<string> Requires => _requires;
        public double Knowledge => _knowledge;
        public IReadOnlyDictionary<string, double> Price => _priceLookup ??= _price.ToDictionary(a => a.Id, a => a.Value);
        public IReadOnlyList<Amount> Goods => _goods;
        public IReadOnlyDictionary<string, double> GoodsPrice => _goodsLookup ??= _goods.ToDictionary(a => a.Id, a => a.Value);
        public int AnyPrecious => _anyPrecious;
        public TechKind Kind => _kind;
        public IReadOnlyList<TechUnlock> Unlocks => _unlockList ??= _unlocks.Select(u => u.ToUnlock()).ToList();
        public IReadOnlyList<TechEffect> Effects => _effectList ??= _effects.Select(e => e.ToEffect()).ToList();
        public bool Planned => _planned;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;

        public override IEnumerable<string> Problems()
        {
            if (_icon == null) yield return "It has no icon.";
            if (_requires.Contains(Id)) yield return "It requires itself.";
            if (_effects.Any(e => e.ToEffect().Value < 0)) yield return "A bonus only climbs: a value is never negative.";
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _priceLookup = null;
            _goodsLookup = null;
            _unlockList = null;
            _effectList = null;
        }
    }
}
