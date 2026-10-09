using System.Collections.Generic;

namespace Codigames.Kingdom.Tutorial.State
{
    public class TutorialState
    {
        // What has been shown or opened once, by key ("door:build", "book:Sagas", a scene's id): never again.
        public HashSet<string> Seen { get; set; } = new();

        // A kingdom from before the doors and the tutorials: every door open, nothing introduced.
        public bool Veteran { get; set; }
    }
}
