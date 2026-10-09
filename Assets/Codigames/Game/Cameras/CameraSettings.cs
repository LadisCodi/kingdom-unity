using Codigames.Modules.Cameras;
using UnityEngine;
using ModuleVector2 = Codigames.Modules.Core.Vector2;

namespace Codigames.Game.Cameras
{
    // How the province camera feels, tuned in the editor. Zoom is said as the web says it: how many cells fit
    // across the screen's width (at the web's zoom 1 a cell is 128 of a phone's 375 points: 2.93 across; its zoom
    // runs 0.4 to 2.5), so every screen shape sees the same width of ground.
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Kingdom/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Zoom (cells across the screen's width)")]
        [SerializeField, Range(0.1f, 10f)] private float _zoomSensitivity = 1f;
        [SerializeField, Tooltip("Where a game starts.")] private float _startCellsAcross = 2.93f;
        [SerializeField, Tooltip("Most zoomed in.")] private float _fewestCellsAcross = 1.17f;
        [SerializeField, Tooltip("Most zoomed out.")] private float _mostCellsAcross = 7.32f;
        [SerializeField, Range(0f, 1f)] private float _zoomElasticFactor = 0.2f;

        [Header("Inertia")]
        [SerializeField] private bool _useInertia = true;
        [SerializeField] private float _damping = 5f;
        [SerializeField] private float _centerDuration = 0.4f;

        [Header("Bounds"), Tooltip("Where the view may pan when nothing else bounds it; the province's own edges do in the game.")]
        [SerializeField] private UnityEngine.Vector2 _minLimit = new(-20f, -20f);
        [SerializeField] private UnityEngine.Vector2 _maxLimit = new(20f, 20f);
        [SerializeField, Range(0f, 1f)] private float _elasticFactor = 0.4f;
        [SerializeField] private float _snapBackSpeed = 10f;

        public float ZoomSensitivity => _zoomSensitivity;
        public float StartCellsAcross => _startCellsAcross;
        public float FewestCellsAcross => _fewestCellsAcross;
        public float MostCellsAcross => _mostCellsAcross;
        public float ZoomElasticFactor => _zoomElasticFactor;
        public bool UseInertia => _useInertia;
        public float Damping => _damping;
        public ModuleVector2 MinLimit => new(_minLimit.x, _minLimit.y);
        public ModuleVector2 MaxLimit => new(_maxLimit.x, _maxLimit.y);
        public float ElasticFactor => _elasticFactor;
        public float SnapBackSpeed => _snapBackSpeed;
        public float CenterDuration => _centerDuration;
    }
}
