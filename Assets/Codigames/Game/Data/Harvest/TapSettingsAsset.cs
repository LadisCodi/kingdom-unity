using Codigames.Kingdom.Harvest;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.Harvest
{
    [CreateAssetMenu(fileName = "Tap", menuName = "Kingdom/Data/Tap Settings")]
    public class TapSettingsAsset : DataSettings, ITapSettings
    {
        [SerializeField, MinValue(1), SuffixLabel("s"), Tooltip("Seconds of work one tap is worth.")] private double _workSeconds = 10;
        [SerializeField, MinValue(0), SuffixLabel("Mana")] private double _manaCost = 1;

        public double WorkSeconds => _workSeconds;
        public double ManaCost => _manaCost;
    }
}
