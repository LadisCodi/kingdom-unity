using System.Collections.Generic;
using Codigames.Kingdom.Relics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Relics
{
    // The relics' rules (relics.json): a city relic's level cycle and windows, the fragments, and the Shrine ladder.
    [CreateAssetMenu(fileName = "Relics", menuName = "Kingdom/Data/Relic Settings")]
    public class RelicSettingsAsset : DataSettings, IRelicSettings
    {
        [BoxGroup("City levels"), SerializeField, Tooltip("What each level-up raises, round and round.")]
        private List<RelicAxis> _cycle = new() { RelicAxis.Window, RelicAxis.Radius, RelicAxis.Effect };
        [BoxGroup("City levels"), SerializeField, Tooltip("The activation's window at each window step.")]
        private List<int> _windowMinutes = new() { 30, 60, 120, 240, 480 };

        [BoxGroup("Fragments"), SerializeField, MinValue(1), Tooltip("A fragment rolled is the keystone one time in this many.")]
        private int _keystoneOneIn = 8;
        [BoxGroup("Fragments"), SerializeField, SuffixLabel("Stardust")] private double _levelStardustBase = 100;
        [BoxGroup("Fragments"), SerializeField] private double _levelStardustGrowth = 1.5;
        [BoxGroup("Fragments"), SerializeField, SuffixLabel("Gems")] private int _fragmentPackGems = 900;
        [BoxGroup("Fragments"), SerializeField, SuffixLabel("fragments")] private int _fragmentPackSize = 5;
        [BoxGroup("Fragments"), SerializeField, Tooltip("Every this many treasures picked up turns up a fragment; 0: never.")]
        private int _treasureEvery = 6;
        [BoxGroup("Fragments"), SerializeField, Tooltip("What a lair claimed pays, by its tier from 1.")]
        private List<int> _perLairTier = new() { 1, 1, 2, 2, 3 };

        [BoxGroup("Shrines"), SerializeField, MinValue(0), Tooltip("Shrines built for their materials after the ruin.")]
        private int _shrineMaterialBuilds = 1;
        [BoxGroup("Shrines"), SerializeField, Tooltip("The Gems of every Shrine after those, one each.")]
        private List<int> _shrinePremiumGems = new() { 3000, 5000, 8000 };

        public IReadOnlyList<RelicAxis> Cycle => _cycle;
        public IReadOnlyList<int> WindowMinutes => _windowMinutes;
        public int KeystoneOneIn => _keystoneOneIn;
        public double LevelStardustBase => _levelStardustBase;
        public double LevelStardustGrowth => _levelStardustGrowth;
        public int FragmentPackGems => _fragmentPackGems;
        public int FragmentPackSize => _fragmentPackSize;
        public int TreasureEvery => _treasureEvery;
        public IReadOnlyList<int> PerLairTier => _perLairTier;
        public int ShrineMaterialBuilds => _shrineMaterialBuilds;
        public IReadOnlyList<int> ShrinePremiumGems => _shrinePremiumGems;
    }
}
