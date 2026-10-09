using Codigames.Game.Map;
using Codigames.Modules.Cameras;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Cameras
{
    // Starts the camera at the game's opening zoom, then advances its springs, inertia and glides every frame.
    // Presentation time: Time.deltaTime.
    public class CameraTicker : IStartable, ITickable
    {
        private readonly CameraController _camera;
        private readonly ICameraRig _rig;
        private readonly ProvinceCameraSettings _settings;

        public CameraTicker(CameraController camera, ICameraRig rig, ProvinceCameraSettings settings)
        {
            _camera = camera;
            _rig = rig;
            _settings = settings;
        }

        public void Start() => _rig.OrthographicSize = _settings.StartZoom;

        public void Tick() => _camera.Tick(Time.deltaTime);
    }
}
