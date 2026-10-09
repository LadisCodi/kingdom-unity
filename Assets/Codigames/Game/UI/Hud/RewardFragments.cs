using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using UnityEngine;

namespace Codigames.Game.UI.Hud
{
    // How many pieces a reward flies as, as the web counts them: a small tap one a unit; otherwise one per minute
    // of what the city makes of it, three to twelve — so a big payout looks big against what the player makes.
    public class RewardFragments
    {
        private const string GOLD = "Gold";
        private const int FEWEST = 3;
        private const int MOST = 12;
        private const int SMALL_TAP = 5;
        private const int UNKNOWN_RATE = 5;

        private readonly CityState _city;
        private readonly Stores _stores;

        public RewardFragments(CityState city, Stores stores)
        {
            _city = city;
            _stores = stores;
        }

        public int For(string currency, double amount, bool tap)
        {
            if (tap && amount < SMALL_TAP) return Mathf.Max(1, (int)amount);

            var perMinute = 0.0;
            if (currency == GOLD)
            {
                foreach (var district in _city.Districts) perMinute += _stores.GoldPerMinute(district);
            }

            return perMinute <= 0 ? UNKNOWN_RATE : Mathf.Clamp(Mathf.RoundToInt((float)(amount / perMinute)), FEWEST, MOST);
        }
    }
}
