using UnityEngine;

namespace Codigames.Game.City
{
    // The building being placed, before it is: its art standing faint on the plot, over the plot's diamond,
    // outlined green where it may go and red where it may not. View only: the placement presenter moves it.
    //
    // It moves as the web's does (ghostFx.ts): it floats a little above its plot, bobbing; a finger on it lifts it
    // higher on a spring; it glides from cell to cell instead of snapping; a new build scales in, picking it up
    // punches it, and a refused confirm shakes it. Lifts and offsets are fractions of the plot's width.
    public class GhostView : MonoBehaviour
    {
        private const float ART_ALPHA = 0.8f;
        private const float FLOAT = 0.07f;
        private const float HELD = 0.17f;
        private const float BOB = 0.014f;
        private const float BOB_SECONDS = 1.8f;
        private const float STIFFNESS = 320f;
        private const float DAMPING = 20f;
        private const float GLIDE_SECONDS = 0.045f;
        private const float APPEAR_SECONDS = 0.26f;
        private const float PUNCH_SECONDS = 0.32f;
        private const float SHAKE_SECONDS = 0.38f;

        [SerializeField] private SpriteRenderer _art;
        [SerializeField] private MeshFilter _plot;
        [SerializeField] private MeshRenderer _plotRenderer;
        [SerializeField] private LineRenderer _outline;
        [SerializeField] private Color _legal = new(0.55f, 0.95f, 0.45f, 1f);
        [SerializeField] private Color _illegal = new(0.95f, 0.3f, 0.25f, 1f);
        [SerializeField, Range(0f, 1f)] private float _plotAlpha = 0.3f;
        [SerializeField, Tooltip("What its plot would do to its neighbours and they to it.")] private MapPills _pills;

        private Mesh _mesh;
        private Vector3 _target;
        private Vector3 _at;
        private Vector3[] _corners = System.Array.Empty<Vector3>();
        private Vector3[] _drawn = System.Array.Empty<Vector3>();
        private Vector3 _artScale = Vector3.one;
        private Vector3 _plotHome;
        private float _plotWidth = 1f;
        private float _lift;
        private float _liftVelocity;
        private float _appearAt = float.NegativeInfinity;
        private float _punchAt = float.NegativeInfinity;
        private float _shakeAt = float.NegativeInfinity;
        private bool _scaleIn;
        private bool _held;
        private bool _fresh = true;

        // How high it floats now, in plot widths: a building planted falls from here.
        public float Lift => _lift;

        // A new ghost: a new build scales in from nothing, a move rises off its own plot.
        public void Begin(bool scaleIn)
        {
            _fresh = true;
            _scaleIn = scaleIn;
            _held = false;
            _punchAt = _shakeAt = float.NegativeInfinity;
        }

        public void Show(Sprite art, Vector3 basePosition, float plotWidth, Vector3[] corners, bool legal)
        {
            gameObject.SetActive(true);

            _art.sprite = art;
            _target = basePosition;
            _plotWidth = plotWidth;
            if (art != null) _artScale = Vector3.one * (plotWidth / (art.rect.width / art.pixelsPerUnit));
            if (_fresh)
            {
                _fresh = false;
                _at = basePosition;
                _lift = 0f;
                _liftVelocity = 0f;
                _appearAt = Time.unscaledTime;
            }

            var tint = legal ? _legal : _illegal;
            DrawPlot(corners, new Color(tint.r, tint.g, tint.b, _plotAlpha));
            _corners = corners;
            if (_drawn.Length != corners.Length) _drawn = new Vector3[corners.Length];
            _outline.positionCount = corners.Length;
            _outline.startColor = _outline.endColor = tint;
            Pose(0f);
        }

        public void Hide()
        {
            _pills.Hide();
            _fresh = true;
            gameObject.SetActive(false);
        }

        // A finger took it: a pop and a stretch, and it rises under the finger.
        public void Grab() => _punchAt = Time.unscaledTime;

        public void SetHeld(bool held) => _held = held;

        // A confirm its spot refused: it shakes its head.
        public void Shake() => _shakeAt = Time.unscaledTime;

        private void Awake() => _plotHome = _plot.transform.position;

        private void Update() => Pose(Mathf.Min(0.05f, Time.unscaledDeltaTime));

        private void Pose(float dt)
        {
            var t = Time.unscaledTime;
            var ease = 1f - Mathf.Exp(-dt / GLIDE_SECONDS);
            _at += (_target - _at) * ease;
            if ((_target - _at).sqrMagnitude < 1e-6f) _at = _target;
            var goal = _held ? HELD : FLOAT;
            _liftVelocity += (STIFFNESS * (goal - _lift) - DAMPING * _liftVelocity) * dt;
            _lift += _liftVelocity * dt;
            var bob = _held ? 0f : BOB * Mathf.Sin(t % BOB_SECONDS / BOB_SECONDS * Mathf.PI * 2f);

            float sx = 1f, sy = 1f, alpha = 1f;
            var a = (t - _appearAt) / APPEAR_SECONDS;
            if (a < 1f)
            {
                alpha = Mathf.Min(1f, a * 2.5f);
                if (_scaleIn)
                {
                    var s = EaseOutBack(a);
                    sx *= 0.6f + 0.4f * s;
                    sy *= 0.6f + 0.4f * s;
                }
            }

            var p = (t - _punchAt) / PUNCH_SECONDS;
            if (p < 1f)
            {
                // Picked up: it stretches up off the ground, then wobbles back.
                var d = Mathf.Exp(-4f * p);
                sx *= 1f - 0.1f * d * Mathf.Cos(p * Mathf.PI * 3f);
                sy *= 1f + 0.14f * d * Mathf.Cos(p * Mathf.PI * 3f);
            }

            var k = (t - _shakeAt) / SHAKE_SECONDS;
            var shake = k < 1f ? 0.05f * (1f - k) * Mathf.Sin(k * Mathf.PI * 2f * 4.5f) : 0f;

            var lift = Mathf.Max(0f, _lift + bob);
            _art.transform.position = _at + new Vector3(shake * _plotWidth, lift * _plotWidth, 0f);
            _art.transform.localScale = new Vector3(_artScale.x * sx, _artScale.y * sy, 1f);
            _art.color = new Color(1f, 1f, 1f, ART_ALPHA * alpha);

            // The plot glides with it, on the ground.
            var offset = _at - _target;
            _plot.transform.position = _plotHome + offset;
            for (var i = 0; i < _corners.Length; i++) _drawn[i] = _corners[i] + offset;
            _outline.SetPositions(_drawn);
        }

        // Overshoots a little past 1 and settles: a thing popping into place.
        private static float EaseOutBack(float k)
        {
            const float c = 1.7f;
            var u = k - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        public MapPills Pills => _pills;

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
