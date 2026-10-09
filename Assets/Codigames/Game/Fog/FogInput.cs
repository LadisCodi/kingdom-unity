using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Harvest;
using Codigames.Game.Map;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Fog;
using Codigames.Modules.Audio;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using ModuleVector3 = Codigames.Modules.Core.Vector3;

namespace Codigames.Game.Fog
{
    // A tap on the fog pays a share of the cell's Gold, the last one clearing it: what it took rises off the cell,
    // and what refused it is said (a refused tap costs nothing).
    public class FogInput
    {
        private const float RISE_FROM = 0.15f;

        private readonly FogOfWar _fog;
        private readonly CityState _city;
        private readonly IConstructionSettings _construction;
        private readonly IFogSettings _settings;
        private readonly ProvinceMap _map;
        private readonly IWorldFeedbackService _feedback;
        private readonly IQuickInfoMessageService _messages;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;

        public FogInput(FogOfWar fog, CityState city, IConstructionSettings construction, IFogSettings settings, ProvinceMap map,
            IWorldFeedbackService feedback, IQuickInfoMessageService messages, ICurrencyIcons icons, NumberFormat numbers,
            Localizer localizer, ISoundService sounds)
        {
            _sounds = sounds;
            _fog = fog;
            _city = city;
            _construction = construction;
            _settings = settings;
            _map = map;
            _feedback = feedback;
            _messages = messages;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void Tap(ModuleVector2Int cell)
        {
            var cost = _fog.IsPayable(cell) ? _fog.TapCost(cell) : 0;

            var result = _fog.Tap(cell);
            switch (result)
            {
                case RevealResult.Paid:
                case RevealResult.Revealed:
                    _sounds.Play(result == RevealResult.Revealed ? SoundIds.REVEAL_DONE : SoundIds.REVEAL_PAID);
                    var centre = _map.CellCentre(cell);
                    var view = _feedback.Spawn<YieldFeedbackView>(new ModuleVector3(centre.x, centre.y + RISE_FROM, 0f));
                    view.Show(_icons.IconOf(FogOfWar.GOLD), "−" + _numbers.Number(cost));
                    _ = view.Play();
                    break;
                case RevealResult.NotReachable:
                    Say(_localizer.Tr("Clear a path to it first — the fog lifts from the edges"));
                    break;
                case RevealResult.OutOfReach:
                    Say(_localizer.Tr("Raise the Townhall to level {n} to explore this far", ("n", _numbers.Number(LevelReaching(cell)))));
                    break;
                case RevealResult.NotEnoughGold:
                    Say(_localizer.Tr("Not enough Gold"));
                    break;
            }
        }

        private void Say(string message)
        {
            _sounds.Play(SoundIds.ERROR);
            _messages.Show(new QuickInfoMessageData(message));
        }

        // The first Townhall level whose reach holds the cell.
        private int LevelReaching(ModuleVector2Int cell)
        {
            var rings = _fog.Rings(cell);
            var reach = _settings.ReachPerTownhallLevel;
            for (var level = 1; level <= reach.Count; level++)
            {
                if (reach[level - 1] >= rings) return level;
            }

            return reach.Count + 1;
        }
    }
}
