using Codigames.Modules.Cameras;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.Cameras
{
    // Advances the camera's springs, inertia and glides every frame. Presentation time: Time.deltaTime.
    public class CameraTicker : ITickable
    {
        private readonly CameraController _camera;

        public CameraTicker(CameraController camera)
        {
            _camera = camera;
        }

        public void Tick() => _camera.Tick(Time.deltaTime);
    }
}
