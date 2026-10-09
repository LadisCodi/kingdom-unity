using System;
using Codigames.Game.Data.Economy;
using Codigames.Game.Map;
using Codigames.Kingdom.Harvest;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;
using ModuleVector2Int = Codigames.Modules.Core.Vector2Int;
using ModuleVector3 = Codigames.Modules.Core.Vector3;

namespace Codigames.Game.Harvest
{
    // A tap on the map's ground, while no menu is open, takes from it: what it took rises off the cell, and
    // what refused it is said.
    public class HarvestInput : IStartable, IDisposable
    {
        // Where the yield starts, above the cell's centre, in world units.
        private const float RISE_FROM = 0.15f;

        private readonly MapGestures _gestures;
        private readonly UIManager _ui;
        private readonly Harvesting _harvesting;
        private readonly ProvinceMap _map;
        private readonly IClock _clock;
        private readonly IWorldFeedbackService _feedback;
        private readonly IQuickInfoMessageService _messages;
        private readonly ICurrencyIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;

        public HarvestInput(MapGestures gestures, UIManager ui, Harvesting harvesting, ProvinceMap map, IClock clock,
            IWorldFeedbackService feedback, IQuickInfoMessageService messages, ICurrencyIcons icons, NumberFormat numbers,
            Localizer localizer)
        {
            _gestures = gestures;
            _ui = ui;
            _harvesting = harvesting;
            _map = map;
            _clock = clock;
            _feedback = feedback;
            _messages = messages;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
        }

        public void Start() => _gestures.Tapped += OnTapped;

        public void Dispose() => _gestures.Tapped -= OnTapped;

        private void OnTapped(ModuleVector2Int cell)
        {
            if (_ui.HasOverlayOpen) return;

            var result = _harvesting.Tap(cell, _clock.NowMs);
            switch (result.Refusal)
            {
                case TapRefusal.None:
                    var centre = _map.CellCentre(cell);
                    var view = _feedback.Spawn<YieldFeedbackView>(new ModuleVector3(centre.x, centre.y + RISE_FROM, 0f));
                    view.Show(_icons.IconOf(result.Currency), "+" + _numbers.Number(result.Paid));
                    _ = view.Play();
                    break;
                case TapRefusal.NoMana:
                    _messages.Show(new QuickInfoMessageData(_localizer.Tr("Out of Mana")));
                    break;
                case TapRefusal.Exhausted:
                    _messages.Show(new QuickInfoMessageData(_localizer.Tr("Growing back")));
                    break;
            }
        }
    }
}
