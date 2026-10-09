using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Relics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Relics
{
    // A relic, whole (Docs/proposals/relic-restoration.md): its name, picture and six fragment pictures, what having it
    // does, the door its first fragment is found at, the numbers it moves and by how much, a city relic's activation,
    // and a world relic's spell.
    [CreateAssetMenu(fileName = "Relic", menuName = "Kingdom/Data/Relic")]
    public class RelicAsset : DefinitionAsset, IRelicDefinition
    {
        [Serializable]
        public class StatRow
        {
            [HorizontalGroup, HideLabel] public string Stat;
            [HorizontalGroup(80), HideLabel, Tooltip("Its value adds to the number rather than multiplying it.")] public bool Add;
        }

        [BoxGroup("Card"), SerializeField] private string _name;
        [BoxGroup("Card"), SerializeField, PreviewField(64)] private Sprite _icon;
        [BoxGroup("Card"), SerializeField, Tooltip("Five pieces and the keystone, in slot order.")] private List<Sprite> _fragments = new();
        [BoxGroup("Card"), SerializeField, TextArea, Tooltip("One line about what having it does.")] private string _text;
        [BoxGroup("Card"), SerializeField, Tooltip("Why its number does nothing yet; empty when it works.")] private string _pending;

        [BoxGroup("Kind"), SerializeField] private RelicKind _kind;
        [BoxGroup("Kind"), SerializeField, Tooltip("The lair (a city relic) or the world source its first fragment is found at.")] private string _door;

        [BoxGroup("Effect"), SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<StatRow> _stats = new();
        [BoxGroup("Effect"), SerializeField, Tooltip("The value at level 1.")] private double _passiveBase = 1;
        [BoxGroup("Effect"), SerializeField, Tooltip("What each effect step adds.")] private double _passivePerLevel;

        [BoxGroup("Activation"), SerializeField, ShowIf(nameof(IsCity)), SuffixLabel("Mana")] private int _activationMana;
        [BoxGroup("Activation"), SerializeField, ShowIf(nameof(IsCity)), SuffixLabel("cells")] private int _activationRadius;

        [BoxGroup("Spell"), SerializeField, HideIf(nameof(IsCity))] private string _spellName;
        [BoxGroup("Spell"), SerializeField, HideIf(nameof(IsCity)), TextArea] private string _spellText;

        private RelicStat[] _rows;

        private bool IsCity => _kind == RelicKind.City;

        public string Name => _name;
        public Sprite Icon => _icon;
        public Sprite Fragment(int slot) => slot >= 0 && slot < _fragments.Count ? _fragments[slot] : _icon;
        public string Text => _text;
        public string PendingText => string.IsNullOrEmpty(_pending) ? null : _pending;
        public string SpellName => string.IsNullOrEmpty(_spellName) ? null : _spellName;
        public string SpellText => _spellText;

        public RelicKind Kind => _kind;
        public string Door => _door;
        public IReadOnlyList<RelicStat> Stats => _rows ??= _stats.Select(s => new RelicStat(s.Stat, s.Add)).ToArray();
        public double PassiveBase => _passiveBase;
        public double PassivePerLevel => _passivePerLevel;
        public int ActivationMana => IsCity ? _activationMana : 0;
        public int ActivationRadius => IsCity ? _activationRadius : 0;
        public bool Pending => PendingText != null;

        protected override void OnValidate()
        {
            base.OnValidate();
            _rows = null;
        }
    }
}
