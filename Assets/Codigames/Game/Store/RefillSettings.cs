using System.Collections.Generic;
using Codigames.Game.Data.Magic;
using Codigames.Game.Data.Monetization;
using Codigames.Kingdom.Magic;

namespace Codigames.Game.Store
{
    // The refills' numbers, from where each is authored: the video's in the ads' settings, the Gem ladder in the Mana's.
    public class RefillSettings : IRefillSettings
    {
        private readonly AdSettingsAsset _ads;
        private readonly ManaSettingsAsset _mana;

        public RefillSettings(AdSettingsAsset ads, ManaSettingsAsset mana)
        {
            _ads = ads;
            _mana = mana;
        }

        public double CooldownMinSeconds => _ads.CooldownMinSeconds;
        public double CooldownMaxSeconds => _ads.CooldownMaxSeconds;
        public double EligibleBelowFraction => _ads.EligibleBelowFraction;
        public int RefillsPerDay => _ads.RefillsPerDay;
        public IReadOnlyList<double> GemRefillCosts => _mana.GemRefillCosts;
    }
}
