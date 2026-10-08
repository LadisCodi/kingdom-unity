using Codigames.Modules.Core;

namespace Codigames.Modules.Cameras
{
    // Port: the camera the controller drives — where it looks and how much it sees.
    public interface ICameraRig
    {
        // The world point at the centre of the view.
        Vector2 Position { get; set; }

        // Half the visible height, in world units.
        float OrthographicSize { get; set; }

        float Aspect { get; }

        // In pixels, to turn a finger's movement into world units.
        float ScreenHeight { get; }
    }
}
