using System;
using Codigames.Game.Data.Monetization;
using Codigames.Game.UI.Menus;
using Codigames.Modules.Clock;
using Codigames.Modules.UI;

namespace Codigames.Game.Ads
{
    // What a rewarded video is watched for: its screen, how long it plays and the reward once it has. The video
    // itself is a stand-in until an SDK takes its place; whatever pays for it pays only through Claim.
    public sealed class AdWatch
    {
        public double StartedAt { get; set; }
        public double Seconds { get; set; }
        public Action Reward { get; set; }
    }

    // The rewarded video (the web's adScreen and startFreePullWatch): one at a time, above everything, no way out but
    // through — when it has played, Claim pays the reward it was watched for.
    public class RewardedAd
    {
        private readonly UIManager _ui;
        private readonly IClock _clock;
        private readonly AdSettingsAsset _settings;

        public RewardedAd(UIManager ui, IClock clock, AdSettingsAsset settings)
        {
            _ui = ui;
            _clock = clock;
            _settings = settings;
        }

        public bool Watching => _ui.IsShown<AdScreen>();

        public void Watch(Action reward)
        {
            if (Watching) return;
            _ = _ui.ShowMenu<AdScreen, AdWatch>(new AdWatch { StartedAt = _clock.NowMs, Seconds = _settings.WatchSeconds, Reward = reward });
        }
    }
}
