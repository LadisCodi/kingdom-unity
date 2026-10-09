using System;
using Codigames.Kingdom.City;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    [Serializable]
    public class BuildingRepairData : IBuildingRepair
    {
        [SerializeField, MinValue(0), SuffixLabel("s"), Tooltip("0: as long as a build.")] private double _seconds;
        [SerializeField, Tooltip("The Bag item an abandoned one is missing; empty when it misses nothing.")] private string _item = "";
        [SerializeField, PreviewField(48), Tooltip("Its level 1, in ruin, where the fog left it.")] private Sprite _ruinArt;

        public double Seconds => _seconds;
        public string Item => _item;
        public Sprite RuinArt => _ruinArt;
    }
}
