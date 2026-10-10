using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Store;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Store
{
    // A product sold for (simulated) money, whole (store.json): its card, its price and Gems, what else it hands
    // over, its next-day part and, for an offer, its window.
    [CreateAssetMenu(fileName = "Product", menuName = "Kingdom/Data/Product")]
    public class ProductAsset : DefinitionAsset, IProductDefinition
    {
        [BoxGroup("Card"), SerializeField] private string _name;
        [BoxGroup("Card"), SerializeField, TextArea] private string _description;
        [BoxGroup("Card"), SerializeField, PreviewField(64)] private Sprite _icon;
        [BoxGroup("Card"), SerializeField, PreviewField(64), Tooltip("The cut-out on an offer's banner; empty: its hero's.")] private Sprite _art;
        [BoxGroup("Card"), SerializeField] private ProductShelf _shelf;

        [BoxGroup("Price"), SerializeField, MinValue(0), SuffixLabel("$")] private double _priceUsd;
        [BoxGroup("Price"), SerializeField, MinValue(0)] private int _gems;

        [BoxGroup("Hands over"), SerializeField] private List<Amount> _items = new();
        [BoxGroup("Hands over"), SerializeField] private string _hero;
        [BoxGroup("Hands over"), SerializeField, MinValue(0)] private int _builders;
        [BoxGroup("Hands over"), SerializeField, MinValue(0)] private int _explorers;
        [BoxGroup("Hands over"), SerializeField, MinValue(0)] private int _heroSlots;

        [BoxGroup("Next day"), SerializeField, MinValue(0)] private int _nextDayGems;
        [BoxGroup("Next day"), SerializeField, MinValue(0)] private int _nextDayHeroXp;
        [BoxGroup("Next day"), SerializeField, MinValue(0)] private int _nextDayFragments;
        [BoxGroup("Next day"), SerializeField] private List<Amount> _nextDayItems = new();

        [BoxGroup("Window"), SerializeField] private string _opensOn = "always";
        [BoxGroup("Window"), SerializeField] private string _door;
        [BoxGroup("Window"), SerializeField] private string _after;
        [BoxGroup("Window"), SerializeField, MinValue(0)] private int _townhall;
        [BoxGroup("Window"), SerializeField, MinValue(0), SuffixLabel("h"), Tooltip("0: until sold out.")] private double _hours;
        [BoxGroup("Window"), SerializeField, MinValue(0), Tooltip("Per window, or per day for a daily; 0: none.")] private int _limit;
        [BoxGroup("Window"), SerializeField, MinValue(0), SuffixLabel("h")] private double _cooldownHours;
        [BoxGroup("Window"), SerializeField, Tooltip("Shown full screen when a session starts.")] private bool _splash;
        [BoxGroup("Window"), SerializeField, Tooltip("Floats on the map as an icon.")] private bool _widget;

        private Dictionary<string, int> _itemLookup;
        private Dictionary<string, int> _nextDayLookup;

        public string DisplayName => _name;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite Art => _art;
        public ProductShelf Shelf => _shelf;
        public double PriceUsd => _priceUsd;
        public int Gems => _gems;
        public IReadOnlyDictionary<string, int> Items => _itemLookup ??= _items.ToDictionary(a => a.Id, a => (int)a.Value);
        public string Hero => string.IsNullOrEmpty(_hero) ? null : _hero;
        public int Builders => _builders;
        public int Explorers => _explorers;
        public int HeroSlots => _heroSlots;
        public int NextDayGems => _nextDayGems;
        public int NextDayHeroXp => _nextDayHeroXp;
        public int NextDayFragments => _nextDayFragments;
        public IReadOnlyDictionary<string, int> NextDayItems => _nextDayLookup ??= _nextDayItems.ToDictionary(a => a.Id, a => (int)a.Value);
        public string OpensOn => _opensOn;
        public string Door => string.IsNullOrEmpty(_door) ? null : _door;
        public string After => string.IsNullOrEmpty(_after) ? null : _after;
        public int Townhall => _townhall;
        public double Hours => _hours;
        public int Limit => _limit;
        public double CooldownHours => _cooldownHours;
        public bool Splash => _splash;
        public bool Widget => _widget;

        protected override void OnValidate()
        {
            base.OnValidate();
            _itemLookup = null;
            _nextDayLookup = null;
        }
    }
}
