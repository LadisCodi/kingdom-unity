using System;
using System.Collections.Generic;
using Codigames.Kingdom.Notices.State;
using Codigames.Kingdom.Sites;
using Codigames.Kingdom.Sites.State;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Notices
{
    // The sites the player has made out (Docs/features/26-notices.md §2.1): a landmark the moment its cell is out of the
    // dark, an abandoned building likewise unless it is already repaired. Each is found once, ever.
    public class SiteFinds
    {
        private readonly NoticesState _state;
        private readonly IProvinceSites _sites;
        private readonly SitesState _sitesState;

        public SiteFinds(NoticesState state, IProvinceSites sites, SitesState sitesState)
        {
            _state = state;
            _sites = sites;
            _sitesState = sitesState;
        }

        // The sites newly in view, now taken as found.
        public IReadOnlyList<string> Sweep(Func<Vector2Int, bool> inView)
        {
            var found = new List<string>();
            foreach (var landmark in _sites.Landmarks)
                if (inView(landmark.Anchor) && _state.Found.Add(landmark.Id)) found.Add(landmark.Id);
            foreach (var ruin in _sites.Abandoned)
            {
                if (_sitesState.Repaired.Contains(ruin.Id)) continue;
                if (inView(ruin.Anchor) && _state.Found.Add(ruin.Id)) found.Add(ruin.Id);
            }

            return found;
        }
    }
}
