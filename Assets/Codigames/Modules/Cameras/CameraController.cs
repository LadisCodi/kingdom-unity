using System;
using Codigames.Modules.Core;

namespace Codigames.Modules.Cameras
{
    // A 2D orthographic camera you drag and pinch: drag pans with inertia and elastic bounds, pinch and scroll
    // zoom with an elastic floor and a hard ceiling, and it can glide to centre on a point. Input arrives
    // through the methods below; time through Tick.
    public class CameraController
    {
        private const float MIN_MOVEMENT = 0.0000001f;
        private const float INERTIA_RESPONSE = 20f;
        private const float SNAP_EPSILON = 0.01f;

        private readonly ICameraRig _rig;
        private readonly ICameraSettings _settings;

        private bool _isDragging;
        private bool _isPinching;
        private Vector2 _lastScreen;
        private float _unitsPerPixel;
        private Vector2 _inertia;

        private bool _isCentering;
        private Vector2 _centerFrom;
        private Vector2 _centerTo;
        private float _centerElapsed;

        public CameraController(ICameraRig rig, ICameraSettings settings)
        {
            _rig = rig;
            _settings = settings;
        }

        public bool IsDragging => _isDragging;

        public void BeginDrag(Vector2 screenPosition)
        {
            _isDragging = true;
            _isCentering = false;
            _inertia = Vector2.Zero;
            // Measured once per drag, so the camera moving under the finger does not feed back into the drag.
            _unitsPerPixel = 2f * _rig.OrthographicSize / Math.Max(1f, _rig.ScreenHeight);
            _lastScreen = screenPosition;
        }

        public void Drag(Vector2 screenPosition, float deltaTime)
        {
            if (!_isDragging) return;

            var delta = (_lastScreen - screenPosition) * _unitsPerPixel;

            if (delta.SqrMagnitude > MIN_MOVEMENT)
            {
                _rig.Position = ApplyElasticConstraints(_rig.Position, _rig.Position + delta);
                if (deltaTime > 0f) _inertia = Vector2.Lerp(_inertia, delta / deltaTime, INERTIA_RESPONSE * deltaTime);
            }

            _lastScreen = screenPosition;
        }

        public void EndDrag() => _isDragging = false;

        // A two-finger pinch: scale > 1 spreads the fingers (zoom in), < 1 brings them together.
        public void Pinch(float scale)
        {
            _isPinching = true;
            if (Math.Abs(scale - 1f) < float.Epsilon) return;

            _isDragging = false;
            ApplyZoom(1f - scale);
        }

        public void EndPinch() => _isPinching = false;

        // A mouse wheel: positive scrolls up (zoom in).
        public void Scroll(float delta)
        {
            if (Math.Abs(delta) > 0.01f) ApplyZoom(-(delta / 120f) * 2f);
        }

        // Glides the view to centre on a world point (clamped to the bounds); a drag interrupts it.
        public void CenterOn(Vector2 worldPosition)
        {
            _inertia = Vector2.Zero;
            _centerFrom = _rig.Position;
            _centerTo = Clamp(worldPosition);
            _centerElapsed = 0f;
            _isCentering = true;
        }

        // Glides so the world point lands at a viewport anchor ((0.5, 0.5) = the centre; higher y = higher up).
        public void CenterOn(Vector2 worldPosition, Vector2 viewportAnchor)
        {
            var halfHeight = _rig.OrthographicSize;
            var halfWidth = halfHeight * _rig.Aspect;
            var offset = new Vector2((viewportAnchor.X - 0.5f) * 2f * halfWidth, (viewportAnchor.Y - 0.5f) * 2f * halfHeight);
            CenterOn(worldPosition - offset);
        }

        public void Tick(float deltaTime)
        {
            SnapZoomBack(deltaTime);

            if (_isCentering)
            {
                AdvanceCentering(deltaTime);
                return;
            }

            if (_isDragging) return;

            var position = _rig.Position;
            var clamped = Clamp(position);

            if (Vector2.Distance(position, clamped) > SNAP_EPSILON)
            {
                _inertia = Vector2.Zero;
                _rig.Position = Vector2.Lerp(position, clamped, _settings.SnapBackSpeed * deltaTime);
            }
            else if (_settings.UseInertia && _inertia.SqrMagnitude > 0.0001f)
            {
                _rig.Position = position + _inertia * deltaTime;
                _inertia = Vector2.Lerp(_inertia, Vector2.Zero, _settings.Damping * deltaTime);
            }
        }

        // Negative zooms in (a smaller size), positive zooms out. Out is a hard stop at the maximum, so nothing
        // off the map comes into view; in past the minimum is elastic and springs back.
        private void ApplyZoom(float delta)
        {
            var current = _rig.OrthographicSize;
            var step = delta * _settings.ZoomSensitivity * (current * 0.1f);
            var target = current + step;

            if (target > _settings.MaxZoom) target = _settings.MaxZoom;
            else if (target < _settings.MinZoom) target = current + step * _settings.ZoomElasticFactor;

            _rig.OrthographicSize = target;
        }

        private void SnapZoomBack(float deltaTime)
        {
            if (_isPinching) return;

            var current = _rig.OrthographicSize;
            var clamped = Math.Clamp(current, _settings.MinZoom, _settings.MaxZoom);
            if (Math.Abs(current - clamped) > SNAP_EPSILON) _rig.OrthographicSize = Lerp(current, clamped, _settings.SnapBackSpeed * deltaTime);
        }

        private void AdvanceCentering(float deltaTime)
        {
            if (_isDragging)
            {
                _isCentering = false;
                return;
            }

            _centerElapsed += deltaTime;
            var t = _settings.CenterDuration <= 0f ? 1f : Math.Min(1f, _centerElapsed / _settings.CenterDuration);
            _rig.Position = _centerFrom + (_centerTo - _centerFrom) * EaseOutCubic(t);
            if (t >= 1f) _isCentering = false;
        }

        private Vector2 ApplyElasticConstraints(Vector2 current, Vector2 target)
        {
            if (target.X < _settings.MinLimit.X || target.X > _settings.MaxLimit.X)
                target.X = current.X + (target.X - current.X) * _settings.ElasticFactor;

            if (target.Y < _settings.MinLimit.Y || target.Y > _settings.MaxLimit.Y)
                target.Y = current.Y + (target.Y - current.Y) * _settings.ElasticFactor;

            return target;
        }

        private Vector2 Clamp(Vector2 position) => new(
            Math.Clamp(position.X, _settings.MinLimit.X, _settings.MaxLimit.X),
            Math.Clamp(position.Y, _settings.MinLimit.Y, _settings.MaxLimit.Y));

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);

        private static float EaseOutCubic(float t)
        {
            var u = 1f - t;
            return 1f - u * u * u;
        }
    }
}
