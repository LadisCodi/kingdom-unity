using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Heroes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Heroes
{
    // A banner, whole (Docs/features/10-heroes.md §6): its key and what a key costs in Gems, the hero chance by heroes
    // owned, the pity marks, the rarity weights, what a duplicate pays, its free calls, and its loot table.
    [CreateAssetMenu(fileName = "Banner", menuName = "Kingdom/Data/Banner")]
    public class BannerAsset : DefinitionAsset, IBannerDefinition
    {
        // A loot row's rarity: Any reaches the whole banner's bag.
        public enum LootRarity
        {
            Any,
            Common,
            Rare,
            Legendary,
        }

        [Serializable]
        public class LootRow
        {
            [HorizontalGroup, HideLabel] public LootReward Reward;
            [HorizontalGroup, HideLabel, ShowIf(nameof(IsFragments))] public LootRarity Rarity;
            [HorizontalGroup, HideLabel, ShowIf(nameof(IsItem))] public string Item;
            [HorizontalGroup(60), HideLabel, MinValue(1)] public int Amount = 1;
            [HorizontalGroup(60), HideLabel, MinValue(0), SuffixLabel("w")] public double Weight = 1;

            private bool IsFragments => Reward == LootReward.Fragments;
            private bool IsItem => Reward == LootReward.Item;
        }

        [BoxGroup("Price"), SerializeField, Tooltip("The item a call spends.")] private string _key;
        [BoxGroup("Price"), SerializeField, SuffixLabel("Gems")] private double _keyGemCost;
        [BoxGroup("Price"), SerializeField, MinValue(0)] private int _freePerDay;
        [BoxGroup("Price"), SerializeField, SuffixLabel("s")] private double _freeCooldownSeconds;

        [BoxGroup("Chance"), SerializeField, Tooltip("The chance a call brings a hero, by heroes owned; the last for ever after.")]
        private List<double> _heroChanceByOwned = new();
        [BoxGroup("Chance"), SerializeField, MinValue(1)] private int _softPityAt = 40;
        [BoxGroup("Chance"), SerializeField, MinValue(1)] private int _hardPityAt = 60;
        [BoxGroup("Chance"), SerializeField, MinValue(0), Tooltip("0: no Legendary guarantee.")] private int _legendaryPityAt;
        [BoxGroup("Chance"), SerializeField, MinValue(0)] private double _weightCommon;
        [BoxGroup("Chance"), SerializeField, MinValue(0)] private double _weightRare;
        [BoxGroup("Chance"), SerializeField, MinValue(0)] private double _weightLegendary;

        [BoxGroup("Prizes"), SerializeField, SuffixLabel("fragments")] private int _duplicateFragments = 10;
        [BoxGroup("Prizes"), SerializeField, Range(0, 1), Tooltip("How often the hero-goods slot pays a second Fragment.")]
        private double _extraHeroSlotChance = 0.2;
        [BoxGroup("Prizes"), SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<LootRow> _loot = new();

        [BoxGroup("Card"), SerializeField, Tooltip("The card shows a hero rather than a chest.")] private bool _showsHero;
        [BoxGroup("Card"), SerializeField, Tooltip("A season hero this banner leans toward; empty for none.")] private string _featuredHero;

        private BannerLoot[] _rows;

        public string Key => _key;
        public double KeyGemCost => _keyGemCost;
        public IReadOnlyList<double> HeroChanceByOwned => _heroChanceByOwned;
        public int SoftPityAt => _softPityAt;
        public int HardPityAt => _hardPityAt;
        public int LegendaryPityAt => _legendaryPityAt;
        public double Weight(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Common => _weightCommon,
            HeroRarity.Rare => _weightRare,
            _ => _weightLegendary,
        };
        public int DuplicateFragments => _duplicateFragments;
        public int FreePerDay => _freePerDay;
        public double FreeCooldownSeconds => _freeCooldownSeconds;
        public bool ShowsHero => _showsHero;
        public string FeaturedHero => string.IsNullOrEmpty(_featuredHero) ? null : _featuredHero;
        public double ExtraHeroSlotChance => _extraHeroSlotChance;
        public IReadOnlyList<BannerLoot> Loot => _rows ??= _loot.Select(r => new BannerLoot(r.Reward,
            r.Rarity == LootRarity.Any ? null : (HeroRarity)(r.Rarity - 1), r.Item, r.Amount, r.Weight)).ToArray();

        protected override void OnValidate()
        {
            base.OnValidate();
            _rows = null;
        }
    }
}
