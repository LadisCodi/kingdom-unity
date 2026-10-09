using System;
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Map
{
    // The province's cells under the player's fingers: a tap names a cell, and a finger that comes down on
    // whatever the drag handler holds is its drag instead of the camera's. A finger that starts over the UI
    // is never the map's.
    public class MapGestures : IStartable, IDisposable
    {
        private readonly ProvinceMap _map;
        private readonly Dictionary<LeanFinger, bool> _claims = new();
        private IMapDragHandler _dragHandler;
        private Func<ModuleVector2Int, bool> _tapGate;

        public MapGestures(ProvinceMap map)
        {
            _map = map;
        }

        // A tap on the province: the cell.
        public event Action<ModuleVector2Int> Tapped;

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

        private void OnFingerDown(LeanFinger finger) => Claims(finger);

        private void OnFingerUpdate(LeanFinger finger)
        {
            if (_claims.TryGetValue(finger, out var claimed) && claimed && finger.ScreenDelta != Vector2.zero)
                _dragHandler?.Drag(CellUnder(finger));
        }

        private void OnFingerUp(LeanFinger finger)
        {
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
