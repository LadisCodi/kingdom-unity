using System;
using Codigames.Kingdom;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Lifecycle;
using Codigames.Modules.Saves;
using Codigames.Modules.Timeline;
using Newtonsoft.Json.Linq;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Session
{
    // Writes the kingdom: soon after anything the player did changed it (at most once every few seconds), and at
    // once when the app may be stopped.
    public class KingdomSaver : IStartable, ITickable, IDisposable
    {
        private const float MIN_SECONDS_BETWEEN_WRITES = 2f;

        private readonly SaveSlot<KingdomState, JObject> _slot;
        private readonly KingdomState _state;
        private readonly Timeline _timeline;
        private readonly ITreasury _treasury;
        private readonly Construction _construction;
        private readonly IAppLifecycle _lifecycle;

        private bool _dirty;
        private float _lastWrite = float.NegativeInfinity;

        public KingdomSaver(SaveSlot<KingdomState, JObject> slot, KingdomState state, Timeline timeline, ITreasury treasury,
            Construction construction, IAppLifecycle lifecycle)
        {
            _slot = slot;
            _state = state;
            _timeline = timeline;
            _treasury = treasury;
            _construction = construction;
            _lifecycle = lifecycle;
        }

        public void Start()
        {
            _treasury.Changed += OnTreasuryChanged;
            _construction.DistrictPlaced += OnDistrict;
            _construction.DistrictMoved += OnDistrict;
            _construction.JobStarted += OnJob;
            _construction.JobCompleted += OnJobCompleted;
            _lifecycle.Suspending += Write;
        }

        public void Dispose()
        {
            _treasury.Changed -= OnTreasuryChanged;
            _construction.DistrictPlaced -= OnDistrict;
            _construction.DistrictMoved -= OnDistrict;
            _construction.JobStarted -= OnJob;
            _construction.JobCompleted -= OnJobCompleted;
            _lifecycle.Suspending -= Write;
        }

        public void Tick()
        {
            if (_dirty && Time.unscaledTime - _lastWrite >= MIN_SECONDS_BETWEEN_WRITES) Write();
        }

        private void OnTreasuryChanged(string currency, double amount) => _dirty = true;
        private void OnDistrict(DistrictState district) => _dirty = true;
        private void OnJob(ConstructionJob job) => _dirty = true;
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => _dirty = true;

        private void Write()
        {
            _state.LastAdvance = _timeline.LastAdvance;
            _state.Balances = new System.Collections.Generic.Dictionary<string, double>(_treasury.Balances);

            if (!_slot.Save(_state)) Debug.LogWarning("#Save# The save on this device is from a newer build; it is not written over.");

            _dirty = false;
            _lastWrite = Time.unscaledTime;
        }
    }
}
