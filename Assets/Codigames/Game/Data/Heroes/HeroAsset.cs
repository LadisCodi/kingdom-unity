using Codigames.Kingdom.Heroes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Heroes
{
    // A hero, whole: its name and title, its portrait, card art and fragment, its rarity and type, the stat block and
    // what each level adds, the passive on its type's troops, its skill, and a Legendary's boon.
    [CreateAssetMenu(fileName = "Hero", menuName = "Kingdom/Data/Hero")]
    public class HeroAsset : DefinitionAsset, IHeroDefinition
    {
        [SerializeField] private string _name;
        [SerializeField] private string _title;
        [BoxGroup("Art"), SerializeField, PreviewField(64)] private Sprite _art;
        [BoxGroup("Art"), SerializeField, PreviewField(48)] private Sprite _portrait;
        [BoxGroup("Art"), SerializeField, PreviewField(48)] private Sprite _fragment;
        [SerializeField] private HeroRarity _rarity;
        [SerializeField, MinValue(0), Tooltip("Its place in its rarity's bag: the order new heroes arrive in. 0 = after the ranked ones, shuffled per kingdom.")] private int _bagRank;
        [SerializeField, Tooltip("The unit type it fights as.")] private string _unitType = "Warrior";

        [BoxGroup("Skill"), SerializeField] private string _skill;
        [BoxGroup("Skill"), SerializeField, Tooltip("At rank 1: a percent, a flat or seconds.")] private double _skillValue;
        [BoxGroup("Skill"), SerializeField, SuffixLabel("s")] private double _skillEvery;

        [BoxGroup("Body at level 1"), SerializeField] private double _atk;
        [BoxGroup("Body at level 1"), SerializeField] private double _dmg;
        [BoxGroup("Body at level 1"), SerializeField] private double _def;
        [BoxGroup("Body at level 1"), SerializeField] private double _hp;
        [BoxGroup("Body at level 1"), SerializeField, SuffixLabel("ticks")] private int _cooldown = 12;
        [BoxGroup("Each level adds"), SerializeField] private double _atkPerLevel;
        [BoxGroup("Each level adds"), SerializeField] private double _dmgPerLevel;
        [BoxGroup("Each level adds"), SerializeField] private double _defPerLevel;
        [BoxGroup("Each level adds"), SerializeField] private double _hpPerLevel;

        [BoxGroup("Its type's troops"), SerializeField] private double _troopDmgMult = 1;
        [BoxGroup("Its type's troops"), SerializeField] private double _troopHpMult = 1;
        [BoxGroup("Its type's troops"), SerializeField] private int _troopDefBonus;

        [BoxGroup("Boon"), SerializeField, Tooltip("A Legendary's kingdom multiplier while owned; empty below Legendary.")] private string _boonStat;
        [BoxGroup("Boon"), SerializeField, ShowIf(nameof(HasBoon))] private double _boonValue = 1;

        private bool HasBoon => !string.IsNullOrEmpty(_boonStat);

        public string Name => _name;
        public string Title => _title;
        public Sprite Art => _art;
        public Sprite Portrait => _portrait;
        public Sprite Fragment => _fragment;
        public HeroRarity Rarity => _rarity;
        public int? BagRank => _bagRank > 0 ? _bagRank : null;
        public string UnitType => _unitType;
        public string Skill => _skill;
        public double SkillValue => _skillValue;
        public double SkillEvery => _skillEvery;
        public double Atk => _atk;
        public double Dmg => _dmg;
        public double Def => _def;
        public double Hp => _hp;
        public int Cooldown => _cooldown;
        public double AtkPerLevel => _atkPerLevel;
        public double DmgPerLevel => _dmgPerLevel;
        public double DefPerLevel => _defPerLevel;
        public double HpPerLevel => _hpPerLevel;
        public double TroopDmgMult => _troopDmgMult;
        public double TroopHpMult => _troopHpMult;
        public int TroopDefBonus => _troopDefBonus;
        public string BoonStat => HasBoon ? _boonStat : null;
        public double BoonValue => _boonValue;
    }
}
