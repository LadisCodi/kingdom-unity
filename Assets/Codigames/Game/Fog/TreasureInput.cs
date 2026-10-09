using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Map;
using Codigames.Kingdom.Fog;
using Codigames.Modules.Audio;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using ModuleVector3 = Codigames.Modules.Core.Vector3;

namespace Codigames.Game.Fog
{
    // A tap on a revealed treasure picks it up, free: its coin rises off the cell with a pop and flies to the plank.
    public class TreasureInput
    {
        private const float RISE_FROM = 0.15f;

        private readonly Treasures _treasures;
        private readonly ProvinceMap _map;
        private readonly IWorldFeedbackService _feedback;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly ISoundService _sounds;
        private readonly UI.Hud.RewardFlight _flight;
        private readonly UI.Hud.RewardFragments _fragments;

        public TreasureInput(Treasures treasures, ProvinceMap map, IWorldFeedbackService feedback, ICurrencyIcons icons, NumberFormat numbers,
            ISoundService sounds, UI.Hud.RewardFlight flight, UI.Hud.RewardFragments fragments)
        {
            _treasures = treasures;
            _map = map;
            _feedback = feedback;
            _icons = icons;
            _numbers = numbers;
            _sounds = sounds;
            _flight = flight;
            _fragments = fragments;
        }

        // Picks up the treasure on the cell; false when there is none to pick up.
        public bool TryPickUp(ModuleVector2Int cell)
        {
            var reward = _treasures.PickUp(cell);
            if (reward == null) return false;

            _sounds.Play(SoundIds.POP);
            var centre = _map.CellCentre(cell);
            foreach (var line in reward.Where(l => l.Value > 0))
            {
                var view = _feedback.Spawn<Harvest.YieldFeedbackView>(new ModuleVector3(centre.x, centre.y + RISE_FROM, 0f));
                view.Show(_icons.IconOf(line.Key), "+" + _numbers.Count(line.Value));
                _ = view.Play();
            }

            var screen = UnityEngine.Camera.main.WorldToScreenPoint(centre);
            _flight.Fly(new Dictionary<string, double>(reward), screen, (c, a) => _fragments.For(c, a, true));
            return true;
        }
    }
}
