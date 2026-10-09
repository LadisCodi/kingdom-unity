using Codigames.Modules.Cameras;
using Lean.Touch;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using ModuleVector2 = Codigames.Modules.Core.Vector2;

namespace Codigames.Game.Cameras
{
    // Reports touches and the mouse wheel to the camera module: one finger drags, two pinch. A finger that
    // starts over the UI, or that the map's drag handler takes (MapGestures), moves nothing.
    public class CameraInputHook : MonoBehaviour
    {
        // LeanTouch's simulated fingers (mouse pinch emulation) carry these indices.
        private const int SIMULATED_FINGER = 42;

        private CameraController _camera;
        private Map.MapGestures _gestures;
        private LeanFinger _dragFinger;
        private bool _wasPinching;

        [Inject]
        public void Construct(CameraController camera, Map.MapGestures gestures)
        {
            _camera = camera;
            _gestures = gestures;
        }

        private void OnEnable()
        {
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            LeanTouch.OnFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUpdate -= OnFingerUpdate;
            LeanTouch.OnFingerUp -= OnFingerUp;
        }

        private void Update()
        {
            if (_camera == null) return;

            ReportScroll();
            ReportPinch();
        }

        private void ReportScroll()
        {
            if (Mouse.current == null) return;
            _camera.Scroll(Mouse.current.scroll.ReadValue().y);
        }

        private void ReportPinch()
        {
            var fingers = LeanTouch.GetFingers(ignoreIfStartedOverGui: true, ignoreIfOverGui: true);

            if (fingers.Count == 2)
            {
                _dragFinger = null;
                _wasPinching = true;
                _camera.Pinch(LeanGesture.GetPinchScale(fingers));
            }
            else if (_wasPinching)
            {
                _wasPinching = false;
                _camera.EndPinch();
            }
        }

        private void OnFingerDown(LeanFinger finger)
        {
            if (_camera == null || !IsDragFinger(finger)) return;

            _dragFinger = finger;
            _camera.BeginDrag(ToModule(finger.ScreenPosition));
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            if (_camera == null || finger != _dragFinger) return;

            // A long press picked something up: from here on the finger carries it, not the camera.
            if (_gestures.Owns(finger))
            {
                _dragFinger = null;
                _camera.EndDrag();
                return;
            }

            _camera.Drag(ToModule(finger.ScreenPosition), Time.deltaTime);
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (finger != _dragFinger) return;

            _dragFinger = null;
            _camera.EndDrag();
        }

        private bool IsDragFinger(LeanFinger finger)
            => finger != null && !finger.StartedOverGui && Mathf.Abs(finger.Index) != SIMULATED_FINGER
               && (_dragFinger == null || finger == _dragFinger) && !_gestures.Claims(finger);

        private static ModuleVector2 ToModule(UnityEngine.Vector2 v) => new(v.x, v.y);
    }
}
