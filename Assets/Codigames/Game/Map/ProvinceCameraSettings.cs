using Codigames.Game.Cameras;
using Codigames.Modules.Cameras;
using UnityEngine;
using ModuleVector2 = Codigames.Modules.Core.Vector2;

namespace Codigames.Game.Map
{
    // The camera's feel as tuned, bounded by the province itself: it may pan as far as the painted map reaches.
    // The zoom, tuned in cells across the screen, becomes an orthographic size for this screen's shape (a cell is
    // one world unit wide).
    public class ProvinceCameraSettings : ICameraSettings
    {
        private readonly CameraSettings _tuned;
        private readonly ICameraRig _rig;
        private readonly ModuleVector2 _min;
        private readonly ModuleVector2 _max;

        public ProvinceCameraSettings(CameraSettings tuned, ProvinceMap province, ICameraRig rig)
        {
            _tuned = tuned;
            _rig = rig;
            var bounds = province.Terrain.localBounds;
            var origin = province.Terrain.transform.position;
            _min = new ModuleVector2(origin.x + bounds.min.x, origin.y + bounds.min.y);
            _max = new ModuleVector2(origin.x + bounds.max.x, origin.y + bounds.max.y);
        }

        public float ZoomSensitivity => _tuned.ZoomSensitivity;
        public float MinZoom => OrthographicSizeFor(_tuned.FewestCellsAcross);
        public float MaxZoom => OrthographicSizeFor(_tuned.MostCellsAcross);
        public float StartZoom => OrthographicSizeFor(_tuned.StartCellsAcross);
        public float ZoomElasticFactor => _tuned.ZoomElasticFactor;
        public bool UseInertia => _tuned.UseInertia;
        public float Damping => _tuned.Damping;
        public ModuleVector2 MinLimit => _min;
        public ModuleVector2 MaxLimit => _max;
        public float ElasticFactor => _tuned.ElasticFactor;
        public float SnapBackSpeed => _tuned.SnapBackSpeed;
        public float CenterDuration => _tuned.CenterDuration;

        // Half the visible height that shows this many cells across.
        private float OrthographicSizeFor(float cellsAcross) => cellsAcross / _rig.Aspect / 2f;
    }
}
