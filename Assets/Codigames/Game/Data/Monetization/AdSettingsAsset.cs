using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Monetization
{
    // The rewarded video (Docs/features/08-magic.md §6): how long the stand-in plays before its reward can be claimed.
    [CreateAssetMenu(fileName = "Ads", menuName = "Kingdom/Data/Ads")]
    public class AdSettingsAsset : DataSettings
    {
        [SerializeField, MinValue(1), SuffixLabel("s")] private double _watchSeconds = 5;
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("Between a refill's video claimed and the next offered.")] private double _cooldownMinSeconds = 600;
        [SerializeField, MinValue(0), SuffixLabel("s")] private double _cooldownMaxSeconds = 1200;
        [SerializeField, Range(0, 1), Tooltip("Below this share of the pool, the video is offered.")] private double _eligibleBelowFraction = 0.5;
        [SerializeField, MinValue(0)] private int _refillsPerDay = 5;

        public double WatchSeconds => _watchSeconds;
        public double CooldownMinSeconds => _cooldownMinSeconds;
        public double CooldownMaxSeconds => _cooldownMaxSeconds;
        public double EligibleBelowFraction => _eligibleBelowFraction;
        public int RefillsPerDay => _refillsPerDay;
    }
}
