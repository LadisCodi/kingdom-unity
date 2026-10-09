using System.Collections.Generic;

namespace Codigames.Kingdom.Research.State
{
    public class ResearchState
    {
        // Knowledge poured into a technology not yet researched; it is never returned or moved.
        public Dictionary<string, double> Poured { get; set; } = new();

        // Researched, in the order they were.
        public List<string> Completed { get; set; } = new();

        // Bands whose reward has been paid, as "<tome>:<era>".
        public List<string> Rewarded { get; set; } = new();
    }
}
