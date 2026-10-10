using Codigames.Game.Data.Monetization;
using UnityEditor;

namespace Codigames.Game.Editor.WebImport
{
    public static partial class WebImporter
    {
        // monetization.json's ads: how long the rewarded video plays.
        private static void ImportMonetization()
        {
            var ads = Read<MonetizationData>("Game/monetization.json").Ads;
            var settings = new SerializedObject(LoadOrCreate<AdSettingsAsset>("Settings", "Ads"));
            settings.FindProperty("_watchSeconds").doubleValue = ads.WatchSeconds;
            settings.FindProperty("_cooldownMinSeconds").doubleValue = ads.CooldownMinSeconds;
            settings.FindProperty("_cooldownMaxSeconds").doubleValue = ads.CooldownMaxSeconds;
            settings.FindProperty("_eligibleBelowFraction").doubleValue = ads.EligibleBelowFraction;
            settings.FindProperty("_refillsPerDay").intValue = (int)ads.ManaRefillsPerDay;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings.targetObject);
        }
    }
}
