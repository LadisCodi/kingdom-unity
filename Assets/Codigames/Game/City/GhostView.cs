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
        // The move arrows (the web's drawMoveArrows): one a way the ghost can step, bobbing outward.
        private const float ARROW_CYCLE = 1.25f;
        private static readonly Color ARROW = new Color32(0x4f, 0x9f, 0x33, 0xeb);
        private static readonly Color ARROW_LIGHT = new Color32(0x8f, 0xd4, 0x66, 0xeb);
        private static readonly Color ARROW_RIM = new Color32(0x2f, 0x6b, 0x1f, 0xff);
        // Tail to tip in cells (along, across): a short shaft and a broad head.
        private static readonly Vector2[] ARROW_SHAPE =
        {
            new(0f, -0.11f), new(0.26f, -0.11f), new(0.26f, -0.26f), new(0.55f, 0f), new(0.26f, 0.26f), new(0.26f, 0.11f), new(0f, 0.11f),
        };
        private static readonly int[] ARROW_TRIANGLES = { 0, 1, 5, 0, 5, 6, 2, 3, 4 };

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
        private readonly Vector2Int[] _steps = new Vector2Int[4];
        private int _stepCount;
        private Vector3 _alongX;
        private Vector3 _alongY;
        private Vector2Int _size = Vector2Int.one;
        private Arrow[] _arrows;

        private sealed class Arrow
        {
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public LineRenderer Rim;
            public readonly Vector3[] Points = new Vector3[7];
            public readonly Color[] Colours = new Color[7];
        }

        // The ways it can step, one grid axis each (none while it is carried): `alongX` and `alongY` are one cell
        // along each axis on the ground, `size` its footprint.
        public void SetSteps(System.Collections.Generic.IReadOnlyList<Vector2Int> steps, Vector3 alongX, Vector3 alongY, Vector2Int size)
        {
            _stepCount = Mathf.Min(steps.Count, _steps.Length);
            for (var i = 0; i < _stepCount; i++) _steps[i] = steps[i];
            _alongX = alongX;
            _alongY = alongY;
            _size = size;
        }

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
            PoseArrows(t, offset);
        }

        private void PoseArrows(float t, Vector3 offset)
        {
            var count = _held ? 0 : _stepCount;
            if (count > 0 && _arrows == null) BuildArrows();
            if (_arrows == null) return;

            var centre = _corners.Length == 4 ? (_corners[0] + _corners[2]) / 2f + offset : _at;
            var bob = 0.5f - 0.5f * Mathf.Cos(t % ARROW_CYCLE / ARROW_CYCLE * Mathf.PI * 2f);
            var tall = Mathf.Abs(_alongX.y) + Mathf.Abs(_alongY.y);
            for (var i = 0; i < _arrows.Length; i++)
            {
                var arrow = _arrows[i];
                var on = i < count;
                arrow.Renderer.enabled = arrow.Rim.enabled = on;
                if (!on) continue;

                var d = _steps[i];
                var v = _alongX * d.x + _alongY * d.y;
                var across = d.x != 0 ? _alongY : _alongX;
                var along = (d.x != 0 ? _size.x : _size.y) / 2f + 0.3f + bob * 0.12f;
                for (var p = 0; p < ARROW_SHAPE.Length; p++)
                {
                    var point = centre + v * (along + ARROW_SHAPE[p].x) + across * ARROW_SHAPE[p].y;
                    arrow.Points[p] = point;
                    // Lit from above: light at the top of the cell's height, dark at its foot.
                    var k = Mathf.InverseLerp(centre.y + tall * 0.3f, centre.y - tall * 0.3f, point.y);
                    arrow.Colours[p] = Color.Lerp(ARROW_LIGHT, ARROW, k);
                }

                arrow.Mesh.vertices = arrow.Points;
                arrow.Mesh.colors = arrow.Colours;
                arrow.Mesh.RecalculateBounds();
                arrow.Rim.SetPositions(arrow.Points);
            }
        }

        private void BuildArrows()
        {
            _arrows = new Arrow[4];
            var material = _plotRenderer.sharedMaterial;
            for (var i = 0; i < _arrows.Length; i++)
            {
                var go = new GameObject("MoveArrow");
                go.transform.SetParent(transform, false);
                var mesh = new Mesh { name = "Move arrow", vertices = new Vector3[7], colors = new Color[7] };
                mesh.triangles = ARROW_TRIANGLES;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerID = _outline.sortingLayerID;
                renderer.sortingOrder = _outline.sortingOrder + 1;
                var rim = new GameObject("Rim").AddComponent<LineRenderer>();
                rim.transform.SetParent(go.transform, false);
                rim.sharedMaterial = _outline.sharedMaterial;
                rim.useWorldSpace = true;
                rim.loop = true;
                rim.positionCount = 7;
                rim.widthMultiplier = 0.025f;
                rim.numCornerVertices = 2;
                rim.startColor = rim.endColor = ARROW_RIM;
                rim.sortingLayerID = _outline.sortingLayerID;
                rim.sortingOrder = _outline.sortingOrder + 2;
                _arrows[i] = new Arrow { Mesh = mesh, Renderer = renderer, Rim = rim };
            }
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
            if (_arrows != null)
                foreach (var arrow in _arrows) Destroy(arrow.Mesh);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
