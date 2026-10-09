using System.Collections.Generic;

namespace Codigames.Kingdom.Sites.State
{
    public class SitesState
    {
        // Abandoned buildings whose repair has started: from then on each is the building it was.
        public List<string> Repaired { get; set; } = new();

        // Landmarks claimed: each is the kingdom's for good.
        public List<string> Claimed { get; set; } = new();
    }
}
