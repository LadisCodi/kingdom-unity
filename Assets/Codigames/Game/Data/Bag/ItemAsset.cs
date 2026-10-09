using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Economy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Bag
{
    // An item of the Bag, whole: its name, picture and what using it does.
    [CreateAssetMenu(fileName = "Item", menuName = "Kingdom/Data/Item")]
    public class ItemAsset : DefinitionAsset, IItemDefinition
    {
        [SerializeField] private string _name;
        [SerializeField, PreviewField(64)] private Sprite _icon;
        [SerializeField] private ItemKind _kind;
        [SerializeField, Range(1, 5), Tooltip("How rare it is: the tile's frame.")] private int _tier = 1;
        [SerializeField, ShowIf(nameof(_kind), ItemKind.Chest), Tooltip("Gold, Food, Wood or Stone.")] private string _coin;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("A chest's worth of production, a speed-up's cut, a boost's length.")]
        private double _seconds;
        [SerializeField, ShowIf(nameof(_kind), ItemKind.Speedup)] private SpeedupKind _speeds;
        [SerializeField, ShowIf(nameof(_kind), ItemKind.Boost)] private BoostKind _boost;
        [SerializeField, Tooltip("A boost's percent, a flask's share of the pool in percent, a tome's Knowledge.")] private double _value;

        public string Name => _name;
        public Sprite Icon => _icon;
        public ItemKind Kind => _kind;
        public string Coin => _kind == ItemKind.Chest && !string.IsNullOrEmpty(_coin) ? _coin : null;
        public double Seconds => _seconds;
        public int Tier => _tier;
        public SpeedupKind? Speeds => _kind == ItemKind.Speedup ? _speeds : null;
        public BoostKind? Boost => _kind == ItemKind.Boost ? _boost : null;
        public double Value => _value;
    }
}
