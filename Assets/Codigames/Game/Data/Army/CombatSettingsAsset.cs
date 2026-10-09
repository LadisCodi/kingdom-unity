using Codigames.Kingdom.Battles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Army
{
    // The fight's dials (Docs/features/combat.md §17) and the party's board.
    [CreateAssetMenu(fileName = "Combat", menuName = "Kingdom/Data/Combat Settings")]
    public class CombatSettingsAsset : DataSettings, ICombatSettings
    {
        [BoxGroup("The clock"), SerializeField, MinValue(1), SuffixLabel("ms")] private int _tickMs = 100;
        [BoxGroup("The clock"), SerializeField, MinValue(1), SuffixLabel("ticks"), Tooltip("Out of clock, the defender wins.")]
        private int _timeoutTicks = 1800;

        [BoxGroup("Attack and Defence"), SerializeField, SuffixLabel("‰ a point")] private int _attackStepPerMille = 50;
        [BoxGroup("Attack and Defence"), SerializeField, SuffixLabel("‰")] private int _attackCapPerMille = 1500;
        [BoxGroup("Attack and Defence"), SerializeField, SuffixLabel("‰ a point")] private int _defenceStepPerMille = 25;
        [BoxGroup("Attack and Defence"), SerializeField, SuffixLabel("‰")] private int _defenceCapPerMille = 750;

        [BoxGroup("The type chart"), SerializeField] private int _typeAdvantageNum = 3;
        [BoxGroup("The type chart"), SerializeField] private int _typeAdvantageDen = 2;
        [BoxGroup("The type chart"), SerializeField] private int _typeDisadvantageNum = 3;
        [BoxGroup("The type chart"), SerializeField] private int _typeDisadvantageDen = 4;

        [BoxGroup("The field"), SerializeField, SuffixLabel("field units")] private int _fieldGap = 360;
        [BoxGroup("The field"), SerializeField, SuffixLabel("field units")] private int _fieldRowPitch = 100;
        [BoxGroup("The field"), SerializeField, SuffixLabel("field units")] private int _fieldColPitch = 100;

        [BoxGroup("The generator"), SerializeField, MinValue(1)] private int _genSlotsMin = 2;
        [BoxGroup("The generator"), SerializeField, MinValue(1)] private int _genSlotsMax = 6;
        [BoxGroup("The generator"), SerializeField, SuffixLabel("power")] private int _genVillainThreshold = 400;
        [BoxGroup("The generator"), SerializeField, Range(0, 1)] private double _genVillainShare = 0.3;
        [BoxGroup("The generator"), SerializeField, MinValue(0)] private int _genVillainSlots = 3;

        [BoxGroup("The party"), SerializeField, MinValue(1)] private int _troopSlots = 6;
        [BoxGroup("The party"), SerializeField, MinValue(0)] private double _heroPowerPerDmg = 1.67;
        [BoxGroup("The party"), SerializeField, MinValue(0), SuffixLabel("Mana"), Tooltip("What every attack charges on the way in.")]
        private double _fightMana = 20;

        public int TickMs => _tickMs;
        public int AttackStepPerMille => _attackStepPerMille;
        public int AttackCapPerMille => _attackCapPerMille;
        public int DefenceStepPerMille => _defenceStepPerMille;
        public int DefenceCapPerMille => _defenceCapPerMille;
        public int TimeoutTicks => _timeoutTicks;
        public int FieldGap => _fieldGap;
        public int FieldRowPitch => _fieldRowPitch;
        public int FieldColPitch => _fieldColPitch;
        public int TypeAdvantageNum => _typeAdvantageNum;
        public int TypeAdvantageDen => _typeAdvantageDen;
        public int TypeDisadvantageNum => _typeDisadvantageNum;
        public int TypeDisadvantageDen => _typeDisadvantageDen;
        public int GenSlotsMin => _genSlotsMin;
        public int GenSlotsMax => _genSlotsMax;
        public int GenVillainThreshold => _genVillainThreshold;
        public double GenVillainShare => _genVillainShare;
        public int GenVillainSlots => _genVillainSlots;
        public double HeroPowerPerDmg => _heroPowerPerDmg;
        public double FightMana => _fightMana;
        public int TroopSlots => _troopSlots;
    }
}
