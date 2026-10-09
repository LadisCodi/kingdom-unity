using Codigames.Kingdom.Research;
using UnityEngine;

namespace Codigames.Game.UI.Data.Research
{
    // A technology's card on its book's page: where it sits (page pixels, from the top-left) and how it stands.
    public readonly struct TechCardData
    {
        public TechCardData(string id, string name, Sprite icon, TechState state, string bar, float fraction, bool ready,
            bool actionable, bool planned, Vector2 position)
        {
            Id = id;
            Name = name;
            Icon = icon;
            State = state;
            Bar = bar;
            Fraction = fraction;
            Ready = ready;
            Actionable = actionable;
            Planned = planned;
            Position = position;
        }

        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public TechState State { get; }

        // Poured over needed, as the bar reads it.
        public string Bar { get; }
        public float Fraction { get; }

        // Its Knowledge is in: the one card asking to be finished.
        public bool Ready { get; }

        // A press worth making now: the orb.
        public bool Actionable { get; }

        public bool Planned { get; }
        public Vector2 Position { get; }
    }
}
