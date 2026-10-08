using System;
using System.Collections.Generic;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // One level's price as authored: currency lines and goods lines.
    [Serializable]
    public class LevelCostData : ILevelCost
    {
        [TableList(AlwaysExpanded = true)]
        [SerializeField] private List<Amount> _currencies = new();
        [TableList(AlwaysExpanded = true)]
        [SerializeField] private List<Amount> _goods = new();

        [NonSerialized] private Dictionary<string, double> _currencyLookup;
        [NonSerialized] private Dictionary<string, double> _goodsLookup;

        public IReadOnlyDictionary<string, double> Currencies => _currencyLookup ??= ToLookup(_currencies);
        public IReadOnlyDictionary<string, double> Goods => _goodsLookup ??= ToLookup(_goods);

        // After an edit: the lookups are read again from the lines.
        public void Invalidate()
        {
            _currencyLookup = null;
            _goodsLookup = null;
        }

        private static Dictionary<string, double> ToLookup(List<Amount> lines)
        {
            var lookup = new Dictionary<string, double>();
            foreach (var line in lines)
            {
                if (line != null && !string.IsNullOrEmpty(line.Id)) lookup[line.Id] = line.Value;
            }

            return lookup;
        }
    }
}
