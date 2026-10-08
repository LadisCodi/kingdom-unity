using System;
using UnityEngine;

namespace Codigames.Game.Data.City
{
    // A building's look from a level on: art comes in tiers, and a level draws the highest tier at or below it.
    [Serializable]
    public class LevelArt
    {
        [SerializeField, Min(1)] private int _fromLevel = 1;
        [SerializeField, Sirenix.OdinInspector.PreviewField(60)] private Sprite _sprite;

        public int FromLevel => _fromLevel;
        public Sprite Sprite => _sprite;
    }
}
