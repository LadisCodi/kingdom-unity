using System;
using Codigames.Kingdom.Harvest;
using VContainer.Unity;

namespace Codigames.Game.UI.Stage
{
    // Taps on the ground this session: the odometer a `taps` line reads relative to where it began.
    public class TapCount : IStartable, IDisposable
    {
        private readonly Harvesting _harvesting;

        public TapCount(Harvesting harvesting) => _harvesting = harvesting;

        public int Taps { get; private set; }

        public void Start() => _harvesting.Tapped += OnTapped;

        public void Dispose() => _harvesting.Tapped -= OnTapped;

        private void OnTapped(Codigames.Modules.Core.Vector2Int cell, TapResult result) => Taps++;
    }
}
