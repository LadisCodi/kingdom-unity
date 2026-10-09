using UnityEngine;

namespace Codigames.Game.City
{
    // The building being placed, before it is: its art standing faint on the plot, over the plot's diamond,
    // outlined green where it may go and red where it may not. View only: the placement presenter moves it.
    public class GhostView : MonoBehaviour
    {
        private const float ART_ALPHA = 0.8f;

        [SerializeField] private SpriteRenderer _art;
        [SerializeField] private MeshFilter _plot;
        [SerializeField] private MeshRenderer _plotRenderer;
        [SerializeField] private LineRenderer _outline;
        [SerializeField] private Color _legal = new(0.55f, 0.95f, 0.45f, 1f);
        [SerializeField] private Color _illegal = new(0.95f, 0.3f, 0.25f, 1f);
        [SerializeField, Range(0f, 1f)] private float _plotAlpha = 0.3f;

        private Mesh _mesh;

        public void Show(Sprite art, Vector3 basePosition, float plotWidth, Vector3[] corners, bool legal)
        {
            gameObject.SetActive(true);

            _art.sprite = art;
            _art.transform.position = basePosition;
            if (art != null) _art.transform.localScale = Vector3.one * (plotWidth / (art.rect.width / art.pixelsPerUnit));
            _art.color = new Color(1f, 1f, 1f, ART_ALPHA);

            var tint = legal ? _legal : _illegal;
            DrawPlot(corners, new Color(tint.r, tint.g, tint.b, _plotAlpha));
            _outline.positionCount = corners.Length;
            _outline.SetPositions(corners);
            _outline.startColor = _outline.endColor = tint;
        }

        public void Hide() => gameObject.SetActive(false);

        private void DrawPlot(Vector3[] corners, Color color)
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Ghost plot" };
                _plot.sharedMesh = _mesh;
            }

            _mesh.Clear();
            _mesh.vertices = corners;
            _mesh.colors = new[] { color, color, color, color };
            _mesh.uv = new[] { Vector2.up, Vector2.one, Vector2.right, Vector2.zero };
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
