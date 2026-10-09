using System.Collections.Generic;

namespace Codigames.Game.UI.Data
{
    // A wait under way, as a card shows it: what is being done, its bar and the time left, and the Gems that skip it.
    public sealed class WorkData
    {
        public WorkData(string doing, float progress, string left, IReadOnlyList<PriceTerm> finish, bool canFinish, string count = null,
            string total = null)
        {
            Doing = doing;
            Progress = progress;
            Left = left;
            Finish = finish;
            CanFinish = canFinish;
            Count = count;
            Total = total;
        }

        // The Bag holds a speed-up that fits: Speed up takes Finish's place (the web's timerButton).
        public bool SpeedUp { get; set; }

        public string Doing { get; }
        public float Progress { get; }
        public string Left { get; }
        public IReadOnlyList<PriceTerm> Finish { get; }
        public bool CanFinish { get; }

        // A batch's size on its face ("x3"), and the whole batch's time; empty for a building's work.
        public string Count { get; }
        public string Total { get; }
    }
}
