using UnityEngine;

namespace Kingdom.Game.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool _applyLeft = true;
        [SerializeField] private bool _applyRight = true;
        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;

        private RectTransform _rect;
        private Canvas _canvas;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (HasChanged())
            {
                Apply();
            }
        }

        private bool HasChanged()
        {
            return Screen.safeArea != _lastSafeArea
                   || Screen.width != _lastScreenSize.x
                   || Screen.height != _lastScreenSize.y
                   || Screen.orientation != _lastOrientation;
        }

        private void Apply()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) return;
            }

            Rect safeArea = Screen.safeArea;

            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            Rect pixelRect = _canvas.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f) return;

            anchorMin.x /= pixelRect.width;
            anchorMin.y /= pixelRect.height;
            anchorMax.x /= pixelRect.width;
            anchorMax.y /= pixelRect.height;

            if (!_applyLeft) anchorMin.x = 0f;
            if (!_applyRight) anchorMax.x = 1f;
            if (!_applyBottom) anchorMin.y = 0f;
            if (!_applyTop) anchorMax.y = 1f;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
