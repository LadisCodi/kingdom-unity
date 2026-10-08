using DG.Tweening;
using Lean.Touch;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Kingdom.Game.Cameras
{
    // 2D orthographic camera controller: drag-pan in the XY plane (with inertia and elastic
    // bounds) and pinch / scroll zoom that drives the Cinemachine lens orthographic size.
    // The Cinemachine camera follows the target; panning moves that target.
    public class CameraManager : MonoBehaviour
    {
        private const float MIN_MOVEMENT_THRESHOLD = 0.0000001f;

        [Header("Scene References")]
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private CinemachineCamera _virtualCamera;
        [SerializeField] private Transform _targetTransform;

        [Header("Zoom Settings (orthographic size)")]
        [SerializeField, Range(0.1f, 10f)] private float _zoomSensitivity = 1.0f;
        [SerializeField] private float _minZoom = 3f;   // most zoomed in (smallest orthographic size)
        [SerializeField] private float _maxZoom = 20f;  // most zoomed out (largest orthographic size)
        [SerializeField, Range(0f, 1f)] private float _zoomElasticFactor = 0.2f;

        [Header("Inertia & Movement")]
        [SerializeField] private bool _useInertia = true;
        [SerializeField] private float _damping = 5.0f;
        [SerializeField] private float _centerDuration = 0.4f;

        [Header("Boundaries (X, Y)")]
        [SerializeField] private Vector2 _minLimit;
        [SerializeField] private Vector2 _maxLimit;
        [SerializeField, Range(0f, 1f)] private float _elasticFactor = 0.4f;
        [SerializeField] private float _snapBackSpeed = 10f;

        private LeanFinger _dragFinger;
        private bool _isDragging;
        private bool _isPinching;
        private Plane _groundPlane = new Plane(Vector3.forward, Vector3.zero);
        private Matrix4x4 _inverseProjectionView;
        private Vector3 _lastWorldPoint;
        private Vector3 _inertiaVelocity;
        private float _fixedZ;
        private Tween _centerTween;

        private void Awake() => InitializeReferences();

        private void InitializeReferences()
        {
            _fixedZ = _targetTransform.position.z;
            // 2D: the pan plane is XY, whose normal is the world Z axis.
            _groundPlane = new Plane(Vector3.forward, _targetTransform.position);
        }

        private void OnEnable()
        {
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerDrag;
            LeanTouch.OnFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUpdate -= OnFingerDrag;
            LeanTouch.OnFingerUp -= OnFingerUp;

            _centerTween?.Kill();
        }

        private void Update()
        {
            HandleMouseScroll();
            HandlePinchZoom();
            ApplyPhysicsLogic();
        }

        #region Zoom helpers

        private float OrthoSize => _virtualCamera != null ? _virtualCamera.Lens.OrthographicSize : 0f;

        private void SetOrthoSize(float size)
        {
            if (_virtualCamera == null) return;
            var lens = _virtualCamera.Lens;
            lens.OrthographicSize = size;
            _virtualCamera.Lens = lens;
        }

        #endregion

        #region Input Handling

        private void HandleMouseScroll()
        {
            if (Mouse.current == null) return;
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                ApplyZoom(-(scrollValue / 120f) * 2f);
            }
        }

        private void HandlePinchZoom()
        {
            var fingers = LeanTouch.GetFingers(ignoreIfStartedOverGui: true, ignoreIfOverGui: true);

            if (fingers.Count == 2)
            {
                _isPinching = true;
                float pinchScale = LeanGesture.GetPinchScale(fingers);

                if (pinchScale != 1.0f)
                {
                    _isDragging = false;
                    _dragFinger = null;
                    ApplyZoom(1.0f - pinchScale);
                }
            }
            else
            {
                _isPinching = false;
            }
        }

        // Adjusts the orthographic zoom. Negative delta zooms in (smaller size), positive zooms
        // out. Zoom-out is hard-capped at the max zoom (no elasticity, so off-map
        // scenery never comes into view); zooming in past the min zoom is elastic.
        private void ApplyZoom(float delta)
        {
            if (_virtualCamera == null) return;

            float current = OrthoSize;
            float step = delta * _zoomSensitivity * (current * 0.1f);
            float target = current + step;

            if (target > _maxZoom)
            {
                // Hard cap on zoom-out — no elastic overshoot.
                target = _maxZoom;
            }
            else if (target < _minZoom)
            {
                // Elastic resistance when zooming in past the limit.
                target = current + (step * _zoomElasticFactor);
            }

            SetOrthoSize(target);
        }

        private void OnFingerDown(LeanFinger finger)
        {
            if (!IsValidDragFinger(finger)) return;
            _dragFinger = finger;
            _isDragging = true;
            _inertiaVelocity = Vector3.zero;
            _centerTween?.Kill(); // The player took over panning; stop the auto-centre.

            Matrix4x4 proj = _gameplayCamera.projectionMatrix;
            Matrix4x4 view = _gameplayCamera.worldToCameraMatrix;
            _inverseProjectionView = (proj * view).inverse;

            _lastWorldPoint = ScreenPointToWorldUsingSavedMatrix(finger.ScreenPosition);
        }

        private void OnFingerDrag(LeanFinger finger)
        {
            if (!IsValidDragFinger(finger) || !_isDragging) return;

            Vector3 currentWorld = ScreenPointToWorldUsingSavedMatrix(finger.ScreenPosition);
            Vector3 delta = _lastWorldPoint - currentWorld;

            if (delta.sqrMagnitude > MIN_MOVEMENT_THRESHOLD)
            {
                Vector3 targetPos = ApplyElasticConstraints(_targetTransform.position, _targetTransform.position + delta);
                _targetTransform.position = new Vector3(targetPos.x, targetPos.y, _fixedZ);

                if (Time.deltaTime > 0)
                {
                    _inertiaVelocity = Vector3.Lerp(_inertiaVelocity, delta / Time.deltaTime, 20f * Time.deltaTime);
                }
            }
            _lastWorldPoint = currentWorld;
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (_dragFinger == finger) { _dragFinger = null; _isDragging = false; }
        }

        #endregion

        #region Physics & Limits

        private void ApplyPhysicsLogic()
        {
            // --- Zoom snapback (elastic) back into [_minZoom, _maxZoom] ---
            if (!_isPinching && _virtualCamera != null)
            {
                float current = OrthoSize;
                float clamped = Mathf.Clamp(current, _minZoom, _maxZoom);
                if (Mathf.Abs(current - clamped) > 0.01f)
                {
                    SetOrthoSize(Mathf.Lerp(current, clamped, _snapBackSpeed * Time.deltaTime));
                }
            }

            // --- Pan snapback (XY limits) + inertia ---
            if (_isDragging || _targetTransform == null) return;

            Vector3 currentPos = _targetTransform.position;
            Vector3 clampedPos = GetClampedPosition(currentPos);

            if (Vector3.Distance(currentPos, clampedPos) > 0.01f)
            {
                _inertiaVelocity = Vector3.zero;
                _targetTransform.position = Vector3.Lerp(currentPos, clampedPos, _snapBackSpeed * Time.deltaTime);
            }
            else if (_useInertia && _inertiaVelocity.sqrMagnitude > 0.0001f)
            {
                _targetTransform.position += _inertiaVelocity * Time.deltaTime;
                _inertiaVelocity = Vector3.Lerp(_inertiaVelocity, Vector3.zero, _damping * Time.deltaTime);
                _targetTransform.position = new Vector3(_targetTransform.position.x, _targetTransform.position.y, _fixedZ);
            }
        }

        private Vector3 ApplyElasticConstraints(Vector3 currentPos, Vector3 targetPos)
        {
            if (targetPos.x < _minLimit.x || targetPos.x > _maxLimit.x)
                targetPos.x = currentPos.x + ((targetPos.x - currentPos.x) * _elasticFactor);

            if (targetPos.y < _minLimit.y || targetPos.y > _maxLimit.y)
                targetPos.y = currentPos.y + ((targetPos.y - currentPos.y) * _elasticFactor);

            return targetPos;
        }

        private Vector3 GetClampedPosition(Vector3 pos) => new Vector3(
            Mathf.Clamp(pos.x, _minLimit.x, _maxLimit.x),
            Mathf.Clamp(pos.y, _minLimit.y, _maxLimit.y),
            _fixedZ);

        // Smoothly pans the camera to centre on a world position (clamped to the pan bounds),
        // cancelling any inertia. Interrupted if the player starts dragging.
        public void CenterOn(Vector3 worldPosition)
        {
            if (_targetTransform == null) return;

            _inertiaVelocity = Vector3.zero;
            _centerTween?.Kill();

            var target = GetClampedPosition(new Vector3(worldPosition.x, worldPosition.y, _fixedZ));
            _centerTween = _targetTransform.DOMove(target, _centerDuration).SetEase(Ease.OutCubic);
        }

        // Pans so that the position lands at the given viewport anchor (0.5,0.5 = centre;
        // higher y = higher on screen), instead of the screen centre. Zoom-consistent (uses the ortho size).
        public void CenterOn(Vector3 worldPosition, Vector2 viewportAnchor)
        {
            if (_gameplayCamera == null)
            {
                CenterOn(worldPosition);
                return;
            }

            var halfHeight = _gameplayCamera.orthographicSize;
            var halfWidth = halfHeight * _gameplayCamera.aspect;

            var offset = new Vector3(
                (viewportAnchor.x - 0.5f) * 2f * halfWidth,
                (viewportAnchor.y - 0.5f) * 2f * halfHeight,
                0f);

            CenterOn(worldPosition - offset);
        }

        #endregion

        #region Helpers & Gizmos

        private Vector3 ScreenPointToWorldUsingSavedMatrix(Vector2 screenPos)
        {
            float x = (screenPos.x / Screen.width) * 2f - 1f;
            float y = (screenPos.y / Screen.height) * 2f - 1f;
            Vector4 clipNear = new Vector4(x, y, -1f, 1f);
            Vector4 clipFar = new Vector4(x, y, 1f, 1f);
            Vector4 wNear = _inverseProjectionView * clipNear;
            Vector4 wFar = _inverseProjectionView * clipFar;
            Vector3 worldNear = (Vector3)(wNear / wNear.w);
            Vector3 worldFar = (Vector3)(wFar / wFar.w);
            Ray r = new Ray(worldNear, (worldFar - worldNear).normalized);
            return _groundPlane.Raycast(r, out float enter) ? r.GetPoint(enter) : _targetTransform.position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((_minLimit.x + _maxLimit.x) / 2f, (_minLimit.y + _maxLimit.y) / 2f, _fixedZ);
            Vector3 size = new Vector3(_maxLimit.x - _minLimit.x, _maxLimit.y - _minLimit.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }

        private bool IsValidDragFinger(LeanFinger finger) =>
            finger != null && !finger.StartedOverGui && !IsSimulationFinger(finger) && (_dragFinger == null || finger == _dragFinger);

        private bool IsSimulationFinger(LeanFinger finger) => finger.Index == 42 || finger.Index == -42;

        #endregion
    }
}
