using System.Collections.Generic;

namespace Codigames.Kingdom.Notices.State
{
    // The news inbox, newest first, and the sites already announced — announced once, ever.
    public class NoticesState
    {
        public List<News> News { get; set; } = new();

        public HashSet<string> Found { get; set; } = new();

        // Whether the sites in view when notices came to this kingdom were taken as found, without a word.
        public bool Swept { get; set; }
    }
}
