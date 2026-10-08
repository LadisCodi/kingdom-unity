using Codigames.Modules.Cameras;
using UnityEngine;
using ModuleVector2 = Codigames.Modules.Core.Vector2;

namespace Codigames.Game.Cameras
{
    // How the province camera feels, tuned in the editor.
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Kingdom/Camera Settings")]
    public class CameraSettings : ScriptableObject, ICameraSettings
    {
        [Header("Zoom (orthographic size)")]
        [SerializeField, Range(0.1f, 10f)] private float _zoomSensitivity = 1f;
        [SerializeField] private float _minZoom = 1.27f;
        [SerializeField] private float _maxZoom = 7.93f;
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
        public float MinZoom => _minZoom;
        public float MaxZoom => _maxZoom;
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
