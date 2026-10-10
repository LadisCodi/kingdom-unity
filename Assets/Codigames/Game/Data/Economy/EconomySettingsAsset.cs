using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Notices;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Economy
{
    [CreateAssetMenu(fileName = "Economy", menuName = "Kingdom/Data/Economy Settings")]
    public class EconomySettingsAsset : DataSettings, IEconomySettings, IWorkerSettings, IBagSettings, IRushSettings, INoticeSettings, IHarmonySettings, IAdjacencyRules, Kingdom.Army.IArmySettings
    {
        [SerializeField, MinValue(0), SuffixLabel("Gold / villager / min"), Tooltip("The rent every housed villager pays.")]
        private double _goldPerPopulationPerMinute = 30;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("A store is ready once it holds this many seconds of its building's making.")]
        private double _collectSeconds = 30;

        [SerializeField, MinValue(0.1), SuffixLabel("cells / s"), Tooltip("How fast a villager walks.")]
        private double _moveSpeedTilesPerSecond = 1;

        [SerializeField, MinValue(0), SuffixLabel("units / h"), Tooltip("What a chest pays an hour of a coin the city barely makes yet.")]
        private double _chestFloorPerHour = 60;

        [SerializeField, MinValue(1), SuffixLabel("s / Gem"), Tooltip("Skipping a wait costs a Gem this many seconds, never less than one.")]
        private double _secondsPerGem = 5;

        [SerializeField, MinValue(1), SuffixLabel("bubbles"), Tooltip("News bubbles shown before the rest fold under a +N.")]
        private int _noticesShown = 4;

        [SerializeField, MinValue(1), SuffixLabel("news"), Tooltip("News kept in the inbox; past it the oldest goes.")]
        private int _noticesKept = 30;

        [SerializeField, Tooltip("Harmony's surplus tiers, ascending: the city's supply over its demand reaching `At` raises the rent by `Bonus` (0.05 = +5%).")]
        private List<Tier> _harmonyTiers = new();

        [SerializeField, Tooltip("What buildings standing side by side do to each other: `District` receives, from `Neighbor` (a building or AnyDecoration, AnyProducer…).")]
        private List<Rule> _adjacency = new();

        [Serializable]
        private class Tier
        {
            [MinValue(1)] public double At = 1.1;
            [MinValue(0)] public double Bonus = 0.05;
        }

        [Serializable]
        private class Rule
        {
            public string District;
            public string Neighbor;
            public AdjacencyStat Stat;
            public double Magnitude;
        }

        // Built once and kept: every store's rent reads them, every frame. An edit in the inspector builds them anew.
        public IReadOnlyList<HarmonyTier> SurplusTiers => _surplusTiers ??= _harmonyTiers.Select(t => new HarmonyTier(t.At, t.Bonus)).ToList();
        public IReadOnlyList<AdjacencyRule> Rules => _rules ??= _adjacency.Select(r => new AdjacencyRule(r.District, r.Neighbor, r.Stat, r.Magnitude)).ToList();

        [NonSerialized] private List<HarmonyTier> _surplusTiers;
        [NonSerialized] private List<AdjacencyRule> _rules;

        private void OnValidate()
        {
            _surplusTiers = null;
            _rules = null;
        }

        [SerializeField, Range(0, 1), Tooltip("Of the soldiers who fall, the share that reaches a bed instead of dying.")]
        private double _woundedShare = 0.1;
        [SerializeField, Range(0, 1), Tooltip("Mending a batch against training as many: its price.")] private double _healCostShare = 0.3;
        [SerializeField, Range(0, 1), Tooltip("Mending a batch against training as many: its time.")] private double _healTimeShare = 0.25;

        public double WoundedShare => _woundedShare;
        public double HealCostShare => _healCostShare;
        public double HealTimeShare => _healTimeShare;

        public int Shown => _noticesShown;
        public int Kept => _noticesKept;
        public double GoldPerPopulationPerMinute => _goldPerPopulationPerMinute;
        public double SecondsPerGem => _secondsPerGem;
        public double ChestFloorPerHour => _chestFloorPerHour;
        public double MoveSpeedTilesPerSecond => _moveSpeedTilesPerSecond;
        public double CollectSeconds => _collectSeconds;
    }
}
