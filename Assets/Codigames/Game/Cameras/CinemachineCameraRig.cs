using Codigames.Modules.Cameras;
using Unity.Cinemachine;
using UnityEngine;
using ModuleVector2 = Codigames.Modules.Core.Vector2;

namespace Codigames.Game.Cameras
{
    // The camera module's rig in Unity: a target a Cinemachine camera follows, and that camera's lens.
    public class CinemachineCameraRig : MonoBehaviour, ICameraRig
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private CinemachineCamera _virtualCamera;
        [SerializeField] private Transform _target;

        public ModuleVector2 Position
        {
            get => new(_target.position.x, _target.position.y);
            set => _target.position = new Vector3(value.X, value.Y, _target.position.z);
        }

        public float OrthographicSize
        {
            get => _virtualCamera.Lens.OrthographicSize;
            set
            {
                var lens = _virtualCamera.Lens;
                lens.OrthographicSize = value;
                _virtualCamera.Lens = lens;
            }
        }

        public float Aspect => _camera.aspect;

        public float ScreenHeight => Screen.height;
    }
}
