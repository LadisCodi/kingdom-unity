using System;
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // The province's cells under the player's fingers: a tap names a cell, and a finger that comes down on
    // whatever the drag handler holds is its drag instead of the camera's. A still finger held on something that
    // moves picks it up — a ring filling beside the finger while it waits — and the same finger then carries it.
    // A finger that starts over the UI is never the map's.
    public class MapGestures : IStartable, IDisposable
    {
        // How long a still press waits before it picks up; the ring shows a beat after it starts, so a tap never
        // flashes one.
        private const float HOLD_SECONDS = 0.45f;
        private const float RING_DELAY_SECONDS = 0.12f;
        // How far a press may wander, in reference pixels, and still be a hold.
        private const float SLOP = 10f;

        private readonly ProvinceMap _map;
        private readonly Dictionary<LeanFinger, bool> _claims = new();
        private IMapDragHandler _dragHandler;
        private Func<ModuleVector2Int, bool> _tapGate;
        private IMapHoldHandler _holdHandler;
        private readonly Dictionary<LeanFinger, (Vector2 Start, bool Ringing)> _holds = new();
        private readonly HashSet<LeanFinger> _picked = new();

        public MapGestures(ProvinceMap map)
        {
            _map = map;
        }

        // A tap on the province: the cell.
        public event Action<ModuleVector2Int> Tapped;

        // A hold has begun to count: where on the screen, and how long the ring has left to fill.
        public event Action<Vector2, float> HoldRingStarted;

        // The hold was let go, wandered, or picked something up.
        public event Action HoldRingEnded;

        public void Start()
        {
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            LeanTouch.OnFingerUp += OnFingerUp;
            LeanTouch.OnFingerTap += OnFingerTap;
        }

        public void Dispose()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUpdate -= OnFingerUpdate;
            LeanTouch.OnFingerUp -= OnFingerUp;
            LeanTouch.OnFingerTap -= OnFingerTap;
        }

        public void SetDragHandler(IMapDragHandler handler) => _dragHandler = handler;

        // Which taps reach the map: a tutorial line may hold them back. Null lets every tap through.
        public void SetTapGate(Func<ModuleVector2Int, bool> gate) => _tapGate = gate;

        public void SetHoldHandler(IMapHoldHandler handler) => _holdHandler = handler;

        // Is the finger the map's own (dragging a ghost), not the camera's?
        public bool Owns(LeanFinger finger) => _claims.TryGetValue(finger, out var claimed) && claimed;

        public void ClearDragHandler(IMapDragHandler handler)
        {
            if (_dragHandler == handler) _dragHandler = null;
        }

        // Whether a finger is the drag handler's: asked once, on its first touch, by whoever asks first.
        public bool Claims(LeanFinger finger)
        {
            if (_claims.TryGetValue(finger, out var claimed)) return claimed;

            claimed = !finger.StartedOverGui && _dragHandler != null && _dragHandler.BeginDrag(CellUnder(finger));
            _claims[finger] = claimed;
            return claimed;
        }

        public ModuleVector2Int CellUnder(Vector2 screenPosition)
        {
            var camera = Camera.main;
            var world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return _map.CellAt(world);
        }

        private ModuleVector2Int CellUnder(LeanFinger finger) => CellUnder(finger.ScreenPosition);

        private void OnFingerDown(LeanFinger finger)
        {
            if (Claims(finger) || finger.StartedOverGui || _holdHandler == null) return;
            if (_holdHandler.CanHold(CellUnder(finger))) _holds[finger] = (finger.ScreenPosition, false);
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            Wait(finger);

            // Picked up by a hold: the same finger drags it, once the drag handler is there to take it.
            if (_picked.Contains(finger) && _dragHandler != null)
            {
                _picked.Remove(finger);
                _claims[finger] = _dragHandler.BeginDrag(CellUnder(finger));
            }

            if (_claims.TryGetValue(finger, out var claimed) && claimed && finger.ScreenDelta != Vector2.zero)
                _dragHandler?.Drag(CellUnder(finger));
        }

        // A still press counts towards a hold: the ring after a beat, the pick-up at the end.
        private void Wait(LeanFinger finger)
        {
            if (!_holds.TryGetValue(finger, out var hold)) return;

            var slop = SLOP * Screen.dpi / 160f;
            if (Vector2.Distance(finger.ScreenPosition, hold.Start) > Mathf.Max(SLOP, slop))
            {
                EndHold(finger);
                return;
            }

            if (!hold.Ringing && finger.Age >= RING_DELAY_SECONDS)
            {
                _holds[finger] = (hold.Start, true);
                HoldRingStarted?.Invoke(hold.Start, HOLD_SECONDS - RING_DELAY_SECONDS);
            }

            if (finger.Age < HOLD_SECONDS) return;
            EndHold(finger);
            if (_holdHandler != null && _holdHandler.Hold(CellUnder(finger))) _picked.Add(finger);
        }

        private void EndHold(LeanFinger finger)
        {
            if (!_holds.Remove(finger, out var hold)) return;
            if (hold.Ringing) HoldRingEnded?.Invoke();
        }

        private void OnFingerUp(LeanFinger finger)
        {
            EndHold(finger);
            _picked.Remove(finger);
            if (_claims.TryGetValue(finger, out var claimed) && claimed) _dragHandler?.EndDrag();
            _claims.Remove(finger);
        }

        private void OnFingerTap(LeanFinger finger)
        {
            if (finger.StartedOverGui || finger.IsOverGui) return;
            var cell = CellUnder(finger);
            if (_tapGate != null && !_tapGate(cell)) return;
            Tapped?.Invoke(cell);
        }
    }
}
