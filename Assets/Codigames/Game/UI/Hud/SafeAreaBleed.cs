using UnityEngine;

namespace Codigames.Game.UI.Hud
{
    // A bar on a screen edge that bleeds under the notch or the home bar: its height is its own plus that edge's
    // inset, so what it holds sits just inside the safe area while its material fills the rest.
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaBleed : MonoBehaviour
    {
        private enum Edge { Top, Bottom }

        [SerializeField] private Edge _edge = Edge.Top;
        [SerializeField, Tooltip("The bar's height inside the safe area, in reference pixels.")]
        private float _height = 107f;

        private RectTransform _rect;
        private Canvas _canvas;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea || Screen.width != _lastScreen.x || Screen.height != _lastScreen.y) Apply();
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (_canvas == null) return;

            var insetPixels = _edge == Edge.Top ? Screen.height - Screen.safeArea.yMax : Screen.safeArea.yMin;
            var inset = insetPixels / _canvas.rootCanvas.scaleFactor;
            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, _height + inset);
        }
    }
}
