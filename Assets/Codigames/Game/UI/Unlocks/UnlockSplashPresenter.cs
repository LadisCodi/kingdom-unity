using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Doors;
using Codigames.Kingdom.Doors;
using Codigames.Modules.Audio;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Unlocks
{
    // Takes what has just opened — four times a second, the doors and the books — and names each one that has a
    // splash, one at a time, in the collection's order (the Tavern's Heroes before the book it brings). A scene waits
    // while one is due or on screen: the splash names the thing, the scene then talks about it.
    public class UnlockSplashPresenter : IStartable, ITickable, IDisposable
    {
        private const float CHECK_SECONDS = 0.25f;

        private readonly Openings _openings;
        private readonly ICatalog<IUnlock> _unlocks;
        private readonly UnlockSplash _view;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly Queue<IUnlock> _queue = new();
        private float _lastCheck;
        private IUnlock _showing;

        public UnlockSplashPresenter(Openings openings, ICatalog<IUnlock> unlocks, UnlockSplash view, Localizer localizer, ISoundService sounds)
        {
            _openings = openings;
            _unlocks = unlocks;
            _view = view;
            _localizer = localizer;
            _sounds = sounds;
        }

        // A splash is on screen, or waiting for its turn.
        public bool IsPending => _showing != null || _queue.Count > 0;

        public void Start()
        {
            _openings.Opened += OnOpened;
            _view.Tapped += OnTapped;
        }

        public void Dispose()
        {
            _openings.Opened -= OnOpened;
            _view.Tapped -= OnTapped;
        }

        public void Tick()
        {
            if (Time.unscaledTime - _lastCheck < CHECK_SECONDS) return;
            _lastCheck = Time.unscaledTime;
            _openings.Take();
            if (_showing == null && _queue.Count > 0) Show(_queue.Dequeue());
        }

        private void OnOpened(IReadOnlyList<DoorId> doors, IReadOnlyList<string> books)
        {
            var doorNames = new HashSet<string>(doors.Select(d => d.ToString().ToLowerInvariant()));
            foreach (var unlock in _unlocks.Items)
            {
                var opened = unlock.Kind == UnlockKind.Door ? doorNames.Contains(unlock.Target.ToLowerInvariant()) : books.Contains(unlock.Target);
                if (opened) _queue.Enqueue(unlock);
            }
        }

        private void Show(IUnlock unlock)
        {
            _showing = unlock;
            _view.Show(_localizer.Tr(unlock.Title), _localizer.Tr(unlock.Text), unlock.Icon, _localizer.Tr("Tap to continue"));
            _sounds.Play(SoundIds.UNLOCK);
        }

        private void OnTapped()
        {
            _showing = null;
            _view.Hide();
            if (_queue.Count > 0) Show(_queue.Dequeue());
        }
    }
}
