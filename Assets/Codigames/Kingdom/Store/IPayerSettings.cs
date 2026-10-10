namespace Codigames.Kingdom.Store
{
    // Who the playtester plays as: a monthly allowance that every purchase spends (F2P's is nothing).
    public enum PayerProfile
    {
        F2P,
        Minnow,
        Dolphin,
        Whale,
        SuperWhale,
    }

    // The profiles' monthly allowances, in dollars, and the offers' daily draw.
    public interface IPayerSettings
    {
        double MonthlyUsd(PayerProfile profile);
        int DailyOffers { get; }
        double OfferSpacingHours { get; }
    }
}
