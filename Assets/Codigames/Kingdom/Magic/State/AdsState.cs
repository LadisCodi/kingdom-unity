namespace Codigames.Kingdom.Magic.State
{
    // The rewarded video's offer of a Mana refill, and the day's refills: an offer up or not, the claims ever made (the
    // next cooldown's key), when the next can come; the UTC day the counts are of, the videos watched and the pools bought.
    public class AdsState
    {
        public bool Pending { get; set; }
        public int Claims { get; set; }
        public double ReadyAt { get; set; }
        public long RefillDay { get; set; } = -1;
        public int Watched { get; set; }
        public int Bought { get; set; }
    }
}
