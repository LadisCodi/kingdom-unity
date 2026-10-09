using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Data
{
    // A workshop's panel on its card (the web's workshopSection): its good and recipe, how many are in store, its crew,
    // the queue's slots, when the next comes off the bench, and the button that queues one more.
    public sealed class WorkshopPanelData
    {
        public Sprite GoodIcon { get; set; }
        public string GoodName { get; set; }
        // The recipe, its icons inline.
        public string Recipe { get; set; }
        public string Held { get; set; }
        public string HeldLabel { get; set; }
        public string CrewLine { get; set; }
        // Nobody at the bench: nothing is being made.
        public bool CrewWarning { get; set; }
        public IReadOnlyList<WorkshopSlotData> Slots { get; set; }
        // When the next comes off the bench; null when nothing is being worked.
        public string Next { get; set; }
        public bool SpeedUp { get; set; }
        public IReadOnlyList<PriceTerm> Finish { get; set; }
        public bool CanFinish { get; set; }
        public string FinishLabel { get; set; }
        public string SpeedUpLabel { get; set; }
        public string MakeLabel { get; set; }
        public IReadOnlyList<PriceTerm> MakePrice { get; set; }
        public bool CanMake { get; set; }
        // "2/3 queued", or why it may not.
        public string MakeNote { get; set; }
    }

    // One place in the queue: empty, waiting, or being worked with its progress.
    public sealed class WorkshopSlotData
    {
        public Sprite Icon { get; set; }
        public bool Filled { get; set; }
        public bool Working { get; set; }
        public float Progress { get; set; }
    }
}
