using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Relics;

namespace Codigames.Game.Relics
{
    // THE RELICS THAT FELL ASLEEP since the player last looked (the web's asleepNotices, M85): their windows closed,
    // live or while away, and nobody has woken them or opened their sheet since. The standing notice reads it. It is
    // the presenter's memory, not the save's, so a reload forgets it.
    public class AsleepRelics : IDisposable
    {
        private readonly Shrines _shrines;
        private readonly List<string> _asleep = new();

        public AsleepRelics(Shrines shrines)
        {
            _shrines = shrines;
            _shrines.WindowClosed += OnClosed;
        }

        // A relic now asleep in its Shrine, the first one first.
        public IReadOnlyList<string> All
        {
            get
            {
                _asleep.RemoveAll(r => _shrines.HostOf(r) == null || _shrines.IsAwake(r));
                return _asleep;
            }
        }

        public event Action Changed;

        // Woken, or looked at: it has nothing more to say.
        public void Seen(string relic)
        {
            if (_asleep.Remove(relic)) Changed?.Invoke();
        }

        public void SeenAll()
        {
            if (_asleep.Count == 0) return;
            _asleep.Clear();
            Changed?.Invoke();
        }

        public void Dispose() => _shrines.WindowClosed -= OnClosed;

        private void OnClosed(string relic)
        {
            if (_asleep.Contains(relic)) return;
            _asleep.Add(relic);
            Changed?.Invoke();
        }
    }
}
