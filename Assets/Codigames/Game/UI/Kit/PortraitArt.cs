using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // A unit's bust for its round portrait, and where it sits in its 256-px file (the web's bustFraming.json): moved
    // by a few pixels and scaled about its bottom centre so the face reads centred in the round.
    [CreateAssetMenu(fileName = "PortraitArt", menuName = "Kingdom/UI/Portrait Art")]
    public class PortraitArt : ScriptableObject
    {
        [Serializable]
        public class Bust
        {
            [SerializeField] private string _unit;
            [SerializeField, PreviewField(48)] private Sprite _sprite;
            [SerializeField] private Vector2 _shift;
            [SerializeField] private float _scale = 1;

            public string Unit => _unit;
            public Sprite Sprite => _sprite;
            public Vector2 Shift => _shift;
            public float Scale => _scale;
        }

        [SerializeField, ListDrawerSettings(ListElementLabelName = "_unit")] private List<Bust> _busts = new();

        public Bust Of(string unit)
        {
            foreach (var bust in _busts)
                if (bust.Unit == unit) return bust;
            return null;
        }
    }
}
