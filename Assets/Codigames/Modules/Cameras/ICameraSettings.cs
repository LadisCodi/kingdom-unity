using Codigames.Modules.Core;

namespace Codigames.Modules.Cameras
{
    // Port: how the camera feels.
    public interface ICameraSettings
    {
        float ZoomSensitivity { get; }

        // Most zoomed in (the smallest orthographic size) and most zoomed out (the largest).
        float MinZoom { get; }
        float MaxZoom { get; }

        // How much a zoom past the minimum still moves (0–1); past the maximum it is a hard stop.
        float ZoomElasticFactor { get; }

        bool UseInertia { get; }
        float Damping { get; }

        // How far the view may pan, and how much a pan past the limits still moves (0–1).
        Vector2 MinLimit { get; }
        Vector2 MaxLimit { get; }
        float ElasticFactor { get; }

        float SnapBackSpeed { get; }
        float CenterDuration { get; }
    }
}
