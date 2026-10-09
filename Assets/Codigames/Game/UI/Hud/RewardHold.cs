using System;
using System.Collections.Generic;

namespace Codigames.Game.UI.Hud
{
    // What the header is not showing yet: a reward already in the treasury but still flying to its slot. The
    // header counts each fragment in as it lands.
    public class RewardHold
    {
        private readonly Dictionary<string, double> _held = new();

        public event Action Changed;

        public double HeldOf(string currency) => _held.TryGetValue(currency, out var held) ? held : 0;

        public void Hold(string currency, double amount)
        {
            _held[currency] = HeldOf(currency) + amount;
            Changed?.Invoke();
        }

        public void Release(string currency, double amount)
        {
            var left = Math.Max(0, HeldOf(currency) - amount);
            if (left <= 0) _held.Remove(currency);
            else _held[currency] = left;
            Changed?.Invoke();
        }
    }
}
