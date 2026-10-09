using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.City;
using Codigames.Kingdom.Fog;
using Codigames.Kingdom.Notices.State;
using Codigames.Kingdom.Quests;
using Codigames.Kingdom.Tutorial;
using Codigames.Kingdom.Tutorial.State;

namespace Codigames.Kingdom.Notices
{
    // Files the news of what happens (Docs/features/26-notices.md §2.1): a building finished, at the moment it finished,
    // live and in replay alike; a site made out, unless a scene introduces it; the last quest claimed.
    public class NewsDesk
    {
        private readonly Inbox _inbox;
        private readonly FogOfWar _fog;
        private readonly SiteFinds _finds;
        private readonly IReadOnlyList<ISceneDefinition> _scenes;
        private readonly TutorialState _tutorial;
        private readonly KingdomState _kingdom;

        public NewsDesk(Inbox inbox, NoticesState state, Construction construction, FogOfWar fog, SiteFinds finds, QuestChain chain,
            IEnumerable<ISceneDefinition> scenes, TutorialState tutorial, KingdomState kingdom)
        {
            _inbox = inbox;
            _fog = fog;
            _finds = finds;
            _scenes = scenes.ToList();
            _tutorial = tutorial;
            _kingdom = kingdom;

            // What was in view before notices came to this kingdom is not news.
            if (!state.Swept)
            {
                finds.Sweep(InView);
                state.Swept = true;
            }

            construction.JobCompleted += (job, district) => inbox.Post(News.Built(district.Id, job.TargetLevel, job.CompletesAt));
            fog.Changed += _ => OnFogChanged();
            chain.Claimed += (claimed, next) =>
            {
                if (next == null) inbox.Post(News.ChainDone(_kingdom.LastAdvance));
            };
        }

        // A scene that introduces a site says it itself: a landmark or lair it is triggered by, or a ruin a line points at.
        public static bool SceneIntroduces(IEnumerable<ISceneDefinition> scenes, string site) => scenes.Any(s =>
            (s.Trigger.Kind is ConditionKind.LandmarkSeen or ConditionKind.LairFound && s.Trigger.Target == site)
            || s.Lines.Any(l => l.Point == "abandoned:" + site));

        private void OnFogChanged()
        {
            foreach (var site in _finds.Sweep(InView))
            {
                if (!_tutorial.Veteran && SceneIntroduces(_scenes, site)) continue;
                _inbox.Post(News.Sighted(site, _kingdom.LastAdvance));
            }
        }

        private bool InView(Modules.Core.Vector2Int cell) => _fog.VisibilityAt(cell) != Visibility.Undiscovered;
    }
}
