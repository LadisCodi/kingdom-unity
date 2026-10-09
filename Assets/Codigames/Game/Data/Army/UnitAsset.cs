using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Army;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Army
{
    // A unit, whole: its name and portrait, what a squad of it is in a fight, and its ranks — I first.
    [CreateAssetMenu(fileName = "Unit", menuName = "Kingdom/Data/Unit")]
    public class UnitAsset : DefinitionAsset, IUnitDefinition
    {
        [SerializeField] private string _name;
        [SerializeField, TextArea] private string _description;
        [SerializeField, Tooltip("Melee, Distance, Mounted.")] private List<string> _tags = new();
        [SerializeField, MinValue(1)] private int _squadSize = 100;
        [SerializeField, MinValue(1)] private int _frontage = 15;
        [SerializeField, MinValue(1), SuffixLabel("ticks")] private int _cooldown = 10;
        [SerializeField, MinValue(0)] private int _speed = 10;
        [SerializeField, MinValue(0)] private int _range = 90;
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true), Tooltip("Rank I first, then its evolutions.")]
        private List<Rank> _ranks = new();

        [NonSerialized] private List<UnitRank> _rankList;

        [Serializable]
        private class Rank
        {
            [PreviewField(48)] public Sprite Portrait;
            public int Atk;
            public int Dmg;
            public int Def;
            public int Hp;
            public int Power;
            public List<Amount> Cost = new();
            [SuffixLabel("s")] public double TrainSeconds;
            [Tooltip("The hall's level that trains it.")] public int MinHallLevel = 1;
        }

        public string Name => _name;
        public string Description => _description;
        public IReadOnlyList<string> Tags => _tags;
        public int SquadSize => _squadSize;
        public int Frontage => _frontage;
        public int Cooldown => _cooldown;
        public int Speed => _speed;
        public int Range => _range;

        public IReadOnlyList<UnitRank> Ranks => _rankList ??= _ranks.Select(r => new UnitRank(r.Atk, r.Dmg, r.Def, r.Hp, r.Power,
            r.Cost.ToDictionary(a => a.Id, a => a.Value), r.TrainSeconds, r.MinHallLevel)).ToList();

        // A rank's portrait, rank I's when it has none of its own.
        public Sprite PortraitAt(int rank)
        {
            for (var i = Math.Min(rank, _ranks.Count) - 1; i >= 0; i--)
                if (_ranks[i].Portrait != null) return _ranks[i].Portrait;
            return null;
        }

        public override IEnumerable<string> Problems()
        {
            if (_ranks.Count == 0) yield return "A unit needs rank I.";
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _rankList = null;
        }
    }
}
