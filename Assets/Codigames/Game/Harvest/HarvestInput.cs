using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Map;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using ModuleVector3 = Codigames.Modules.Core.Vector3;

namespace Codigames.Game.Harvest
{
    // A tap on the ground takes from it: what it took rises off the cell, and what refused it is said.
    public class HarvestInput
    {
        // Where the yield starts, above the cell's centre, in world units.
        private const float RISE_FROM = 0.15f;

        private readonly Harvesting _harvesting;
        private readonly ProvinceMap _map;
        private readonly IClock _clock;
        private readonly IWorldFeedbackService _feedback;
        private readonly IQuickInfoMessageService _messages;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly ISoundService _sounds;
        private readonly Feedback.TapPunch _punch;
        private readonly UI.Hud.RewardFlight _flight;
        private readonly UI.Hud.RewardFragments _fragments;

        public HarvestInput(Harvesting harvesting, ProvinceMap map, IClock clock,
            IWorldFeedbackService feedback, IQuickInfoMessageService messages, ICurrencyIcons icons, NumberFormat numbers,
            Localizer localizer, ISoundService sounds, Feedback.TapPunch punch, UI.Hud.RewardFlight flight,
            UI.Hud.RewardFragments fragments)
        {
            _punch = punch;
            _flight = flight;
            _fragments = fragments;
            _sounds = sounds;
            _harvesting = harvesting;
            _map = map;
            _clock = clock;
            _feedback = feedback;
            _messages = messages;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void Take(ModuleVector2Int cell)
        {
            var source = _harvesting.SourceAt(cell);
            var result = _harvesting.Tap(cell, _clock.NowMs);
            switch (result.Refusal)
            {
                case TapRefusal.None:
                    _sounds.Play(SoundIds.TapOn(source?.Id));
                    _punch.Cell(cell);
                    Fly(cell, result.Currency, result.Paid);
                    var centre = _map.CellCentre(cell);
                    var view = _feedback.Spawn<YieldFeedbackView>(new ModuleVector3(centre.x, centre.y + RISE_FROM, 0f));
                    view.Show(_icons.IconOf(result.Currency), "+" + _numbers.Number(result.Paid));
                    _ = view.Play();
                    break;
                case TapRefusal.NoMana:
                    _sounds.Play(SoundIds.ERROR);
                    _messages.Show(new QuickInfoMessageData(_localizer.Tr("Out of Mana")));
                    break;
                case TapRefusal.Exhausted:
                    _sounds.Play(SoundIds.TAP_EMPTY);
                    _messages.Show(new QuickInfoMessageData(_localizer.Tr("Growing back")));
                    break;
            }
        }

        // What the tap took flies to the header from the cell.
        private void Fly(ModuleVector2Int cell, string currency, double amount)
        {
            var screen = UnityEngine.Camera.main.WorldToScreenPoint(_map.CellCentre(cell));
            _flight.Fly(new System.Collections.Generic.Dictionary<string, double> { [currency] = amount }, screen,
                (c, a) => _fragments.For(c, a, true));
        }
    }
}
