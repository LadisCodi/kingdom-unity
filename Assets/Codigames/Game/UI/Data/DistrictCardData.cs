using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A district's card, ready to show: what it is, and either the work under way or its next level.
    public sealed class DistrictCardData
    {
        public DistrictCardData(string name, string ordinal, string level, Sprite art, string promise, bool working,
            string work, float progress, string next, IReadOnlyList<CostChipData> price, bool canUpgrade, string reason, string store, bool storeFull)
        {
            Store = store;
            StoreFull = storeFull;
            Name = name;
            Ordinal = ordinal;
            Level = level;
            Art = art;
            Promise = promise;
            Working = working;
            Work = work;
            Progress = progress;
            Next = next;
            Price = price;
            CanUpgrade = canUpgrade;
            Reason = reason;
        }

        public string Name { get; }
        public string Ordinal { get; }

        // "Lv 2".
        public string Level { get; }

        public Sprite Art { get; }
        public string Promise { get; }

        // A builder is on it: Work says what and how long is left, Progress how far along.
        public bool Working { get; }
        public string Work { get; }
        public float Progress { get; }

        // The next level and its wait ("Lv 3 · 35s"); empty at the highest.
        public string Next { get; }

        public IReadOnlyList<CostChipData> Price { get; }
        public bool CanUpgrade { get; }

        // Why it cannot be upgraded, when the price does not say it already.
        public string Reason { get; }

        // What its store holds against its capacity ("Storage 120/600"); empty for a building with none.
        public string Store { get; }
        public bool StoreFull { get; }
    }
}
