using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // A notice for the player. The group says which of the other fields it carries.
    public sealed class News
    {
        public string Key { get; set; }
        public double At { get; set; }
        public string Group { get; set; }
        public string District { get; set; }
        public double? Level { get; set; }
        public string Unit { get; set; }
        // A good id on a goods notice; whether the news is good on a world notice.
        public object Good { get; set; }
        public double? Count { get; set; }
        public string Lair { get; set; }
        public Dictionary<string, double> Took { get; set; }
        public string Site { get; set; }
        public double? Hex { get; set; }
        public string What { get; set; }
        public double? Troops { get; set; }
        public double? Fallen { get; set; }
        public string Text { get; set; }
        public bool? Open { get; set; }
        public double? ClosesAt { get; set; }
        public double? Place { get; set; }
        public double? Of { get; set; }
        public double? Floor { get; set; }
        public string Entry { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
    }
}
