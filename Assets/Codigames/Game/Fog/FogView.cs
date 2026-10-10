using System.Collections.Generic;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Fog
{
    // The fog as a sunlit sea of clouds: undiscovered ground under the bank (Kingdom/Cloud Bank — one drifting cloud
    // texture cut to a mask of a texel a cell, thinning toward seen ground), ground the player can clear under a thin
    // veil and a faint patch of cloud, ground seen but out of reach under a cushion. The bank runs on past the
    // province's edge, and lies under everything standing on the ground. Revealed ground is clear.
    public class FogView : MonoBehaviour
    {
        // The drift's clock wraps at a whole repeat of every layer (the shader's DRIFT_S twice).
        private const double CLOCK_WRAP_S = 1200;
        // A cell is this many of the web's pixels across: the shader's numbers are in them.
        private const float WEB_CELL_PX = 128;
        private static readonly int MASK = Shader.PropertyToID("_Mask");
        private static readonly int MASK_RECT = Shader.PropertyToID("_MaskRect");
        private static readonly int WORLD_TO_CELL = Shader.PropertyToID("_WorldToCell");
        private static readonly int CELL_ORIGIN = Shader.PropertyToID("_CellOrigin");
        private static readonly int PIXELS_PER_UNIT = Shader.PropertyToID("_PixelsPerUnit");
        private static readonly int BANK_TIME = Shader.PropertyToID("_BankTime");

        [SerializeField] private Tilemap _floor;
        [SerializeField] private Tilemap _clouds;
        [SerializeField] private TileBase _veil;
        [SerializeField] private TileBase _patch;
        [SerializeField] private TileBase _cushion;
        [SerializeField] private Material _bank;
        [SerializeField, Tooltip("Above the fog floor, under everything standing on the ground.")] private int _bankOrder = -49;
        [SerializeField, Tooltip("Cells the bank's quad runs on past the province's edge.")] private int _margin = 40;

        private FogOfWar _fog;
        private ProvinceMap _map;
        private BoundsInt _bounds;
        private Texture2D _mask;
        private byte[] _maskBytes;
        private Material _bankInstance;
        private GameObject _bankQuad;

        [Inject]
        public void Construct(FogOfWar fog, ProvinceMap map)
        {
            _fog = fog;
            _map = map;
        }

        private void Start()
        {
            // The mask is the province and a ring of bank round it; past it the rim repeats, so the bank runs on.
            var province = _map.Terrain.cellBounds;
            _bounds = new BoundsInt(province.xMin - 1, province.yMin - 1, 0, province.size.x + 2, province.size.y + 2, 1);
            BuildBank();

            Refresh();
            _fog.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (_fog != null) _fog.Changed -= OnChanged;
            if (_mask != null) Destroy(_mask);
            if (_bankInstance != null) Destroy(_bankInstance);
        }

        private void Update()
        {
            if (_bankInstance != null) _bankInstance.SetFloat(BANK_TIME, (float)(Time.unscaledTimeAsDouble % CLOCK_WRAP_S));
        }

        // One quad over the province and far past it, and the mask it reads.
        private void BuildBank()
        {
            if (_bank == null) return;
            _mask = new Texture2D(_bounds.size.x, _bounds.size.y, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Fog mask",
            };
            _maskBytes = new byte[_bounds.size.x * _bounds.size.y];

            var grid = _floor.layoutGrid;
            var origin = grid.CellToWorld(Vector3Int.zero);
            var a = grid.CellToWorld(new Vector3Int(1, 0, 0)) - origin;
            var b = grid.CellToWorld(new Vector3Int(0, 1, 0)) - origin;
            var det = a.x * b.y - b.x * a.y;
            _bankInstance = new Material(_bank) { name = "Cloud Bank (live)" };
            _bankInstance.SetTexture(MASK, _mask);
            _bankInstance.SetVector(MASK_RECT, new Vector4(_bounds.xMin, _bounds.yMin, _bounds.size.x, _bounds.size.y));
            _bankInstance.SetVector(WORLD_TO_CELL, new Vector4(b.y / det, -b.x / det, -a.y / det, a.x / det));
            _bankInstance.SetVector(CELL_ORIGIN, origin);
            _bankInstance.SetFloat(PIXELS_PER_UNIT, WEB_CELL_PX / (a - b).x);

            var min = new Vector3Int(_bounds.xMin - _margin, _bounds.yMin - _margin, 0);
            var max = new Vector3Int(_bounds.xMax + _margin, _bounds.yMax + _margin, 0);
            var corners = new[]
            {
                grid.CellToWorld(new Vector3Int(min.x, min.y, 0)), grid.CellToWorld(new Vector3Int(max.x, min.y, 0)),
                grid.CellToWorld(new Vector3Int(max.x, max.y, 0)), grid.CellToWorld(new Vector3Int(min.x, max.y, 0)),
            };
            var mesh = new Mesh { name = "Cloud bank", vertices = corners, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            mesh.RecalculateBounds();

            _bankQuad = new GameObject("CloudBank");
            _bankQuad.transform.SetParent(transform, false);
            _bankQuad.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = _bankQuad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _bankInstance;
            renderer.sortingLayerID = _floor.GetComponent<TilemapRenderer>().sortingLayerID;
            renderer.sortingOrder = _bankOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // A reveal can change what can be paid for far away (the reach), so the whole fog is drawn again.
        private void OnChanged(IReadOnlyCollection<ModuleVector2Int> cells) => Refresh();

        private void Refresh()
        {
            var count = _bounds.size.x * _bounds.size.y;
            var positions = new Vector3Int[count];
            var floors = new TileBase[count];
            var clouds = new TileBase[count];

            // allPositionsWithin walks x fastest, then y: the mask's own row order.
            var i = 0;
            foreach (var position in _bounds.allPositionsWithin)
            {
                positions[i] = position;
                var cell = ProvinceCoordinates.FromTilemap(position);
                var bank = !_map.Contains(cell) || _fog.VisibilityAt(cell) == Visibility.Undiscovered;
                if (_maskBytes != null) _maskBytes[i] = bank ? (byte)255 : (byte)0;
                if (!bank && _fog.VisibilityAt(cell) == Visibility.Discovered)
                {
                    floors[i] = _veil;
                    clouds[i] = _fog.IsPayable(cell) ? _patch : _cushion;
                }

                i++;
            }

            _floor.SetTiles(positions, floors);
            _clouds.SetTiles(positions, clouds);
            if (_mask == null) return;
            _mask.SetPixelData(_maskBytes, 0);
            _mask.Apply(false);
        }
    }
}
