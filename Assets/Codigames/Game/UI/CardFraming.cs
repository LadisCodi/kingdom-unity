using System;
using Codigames.Game.Map;
using Cysharp.Threading.Tasks;
using Codigames.Modules.Cameras;
using ModuleVector2 = Codigames.Modules.Core.Vector2;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.UI
{
    // A card about something on the map moves that thing into the map the card leaves visible — the band between
    // the header and the card's top edge — once, when the card opens (the web's frameOnMap). Panning after that is
    // the player's.
    public class CardFraming
    {
        // The header's foot, as a share of the screen's height from its top.
        private const float HEADER = 0.075f;

        private readonly CameraController _camera;
        private readonly ProvinceMap _map;

        public CardFraming(CameraController camera, ProvinceMap map)
        {
            _camera = camera;
            _map = map;
        }

        // Once the card's layout has settled — its height is only known at the end of the frame it opened in.
        public void Frame(ModuleVector2Int anchor, int width, int height, Func<float> cardTop)
            => FrameSettled(anchor, width, height, cardTop).Forget();

        private async UniTaskVoid FrameSettled(ModuleVector2Int anchor, int width, int height, Func<float> cardTop)
        {
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            Frame(anchor, width, height, cardTop());
        }

        private void Frame(ModuleVector2Int anchor, int width, int height, float cardTop)
        {
            var corners = ProvinceGeometry.Corners(_map, anchor, width, height);
            var centre = (corners[0] + corners[2]) / 2f;
            var band = (cardTop + 1f - HEADER) / 2f;
            _camera.CenterOn(new ModuleVector2(centre.x, centre.y), new ModuleVector2(0.5f, band));
        }
    }
}
