using Codigames.Kingdom.Store;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Store
{
    // The payer profiles' monthly allowances and the offers' daily draw (monetization.json).
    [CreateAssetMenu(fileName = "Payer", menuName = "Kingdom/Data/Payer Settings")]
    public class PayerSettingsAsset : DataSettings, IPayerSettings
    {
        [BoxGroup("Monthly budget"), SerializeField, MinValue(0), SuffixLabel("$")] private double _f2p;
        [BoxGroup("Monthly budget"), SerializeField, MinValue(0), SuffixLabel("$")] private double _minnow = 10;
        [BoxGroup("Monthly budget"), SerializeField, MinValue(0), SuffixLabel("$")] private double _dolphin = 50;
        [BoxGroup("Monthly budget"), SerializeField, MinValue(0), SuffixLabel("$")] private double _whale = 250;
        [BoxGroup("Monthly budget"), SerializeField, MinValue(0), SuffixLabel("$")] private double _superWhale = 2000;
        [BoxGroup("Offers"), SerializeField, MinValue(0)] private int _dailyOffers = 3;
        [BoxGroup("Offers"), SerializeField, MinValue(0), SuffixLabel("h"), Tooltip("Between two windows opening on their own.")]
        private double _offerSpacingHours = 20;

        public double MonthlyUsd(PayerProfile profile) => profile switch
        {
            PayerProfile.F2P => _f2p,
            PayerProfile.Minnow => _minnow,
            PayerProfile.Dolphin => _dolphin,
            PayerProfile.Whale => _whale,
            _ => _superWhale,
        };

        public int DailyOffers => _dailyOffers;
        public double OfferSpacingHours => _offerSpacingHours;
    }
}
