using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Monetization
{
    // The rewarded video (Docs/features/08-magic.md §6): how long the stand-in plays before its reward can be claimed.
    [CreateAssetMenu(fileName = "Ads", menuName = "Kingdom/Data/Ads")]
    public class AdSettingsAsset : DataSettings
    {
        [SerializeField, MinValue(1), SuffixLabel("s")] private double _watchSeconds = 5;

        public double WatchSeconds => _watchSeconds;
    }
}
