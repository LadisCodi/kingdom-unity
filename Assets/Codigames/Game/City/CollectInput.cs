using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Harvest;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Core;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using ModuleVector3 = Codigames.Modules.Core.Vector3;

namespace Codigames.Game.City
{
    // A tap on a building whose store is ready collects it, free: what it held rises off the building.
    public class CollectInput
    {
        private const float RISE_FROM = 0.4f;

        private readonly Stores _stores;
        private readonly ICatalog<IBuildingDefinition> _buildings;
        private readonly ProvinceMap _map;
        private readonly IClock _clock;
        private readonly IWorldFeedbackService _feedback;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly ISoundService _sounds;

        public CollectInput(Stores stores, ICatalog<IBuildingDefinition> buildings, ProvinceMap map, IClock clock,
            IWorldFeedbackService feedback, ICurrencyIcons icons, NumberFormat numbers, ISoundService sounds)
        {
            _sounds = sounds;
            _stores = stores;
            _buildings = buildings;
            _map = map;
            _clock = clock;
            _feedback = feedback;
            _icons = icons;
            _numbers = numbers;
        }

        // False when the store is not ready: the tap is the card's.
        public bool TryCollect(DistrictState district)
        {
            var now = _clock.NowMs;
            if (!_stores.IsReady(district, now)) return false;

            var houses = _stores.Residents(district) > 0;
            var moved = _stores.Collect(district, now);
            _sounds.Play(houses ? SoundIds.TAP_HOUSE : SoundIds.POP);
            var building = _buildings.Get(district.DefinitionId);
            var (basePosition, width) = ProvinceGeometry.Footprint(_map, district.Anchor, building.Width, building.Height);

            var line = 0;
            foreach (var amount in moved)
            {
                var at = new ModuleVector3(basePosition.x, basePosition.y + width / 4f + RISE_FROM + line++ * 0.25f, 0f);
                var view = _feedback.Spawn<YieldFeedbackView>(at);
                view.Show(_icons.IconOf(amount.Key), "+" + _numbers.Number(amount.Value));
                _ = view.Play();
            }

            return true;
        }
    }
}
