using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Relics;
using Codigames.Game.UI.Data;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Magic;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Localization;
using UnityEngine;

namespace Codigames.Game.UI.Relics
{
    // A Shrine's section of its card, as the card's presenter asks for it (the web's shrineView and activationOverlay):
    // what it holds and how, and its presses — the picker, Activate, a flask. Kept apart so the card's presenter takes
    // one thing for the relics, not seven.
    public class ShrinePanels
    {
        private readonly Shrines _shrines;
        private readonly Kingdom.Relics.Relics _relics;
        private readonly RelicCards _cards;
        private readonly RelicWords _words;
        private readonly RelicActions _actions;
        private readonly ManaPool _mana;
        private readonly PriceTerms _prices;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly Sprite _painting;
        private readonly Sprite _paintingEmpty;

        public ShrinePanels(Shrines shrines, Kingdom.Relics.Relics relics, RelicCards cards, RelicWords words, RelicActions actions,
            ManaPool mana, PriceTerms prices, NumberFormat numbers, Localizer localizer, Codigames.Game.Data.Relics.RelicArtAsset art)
        {
            _shrines = shrines;
            _relics = relics;
            _cards = cards;
            _words = words;
            _actions = actions;
            _mana = mana;
            _prices = prices;
            _numbers = numbers;
            _localizer = localizer;
            _painting = art.ShrineInterior;
            _paintingEmpty = art.ShrineInteriorEmpty;
        }

        public bool IsShrine(DistrictState district) => _shrines.All.Contains(district);

        public ShrinePanelData Panel(DistrictState district, double now)
        {
            var held = _shrines.Hosted(district);
            if (held == null)
                return new ShrinePanelData
                {
                    Painting = _paintingEmpty,
                    Empty = true,
                    Placeable = _cards.Met().Any(id => _cards.Status(id) == RelicStatus.Bag && _cards.Get(id).Kind == RelicKind.City),
                };

            var relic = _cards.Get(held);
            var level = _relics.Level(held);
            var status = _cards.Status(held);
            var panel = new ShrinePanelData
            {
                Painting = _painting,
                Relic = relic.Icon,
                Status = status,
                Head = _localizer.Tr("{name} · Lv {n}", ("name", _localizer.Tr(relic.Name)), ("n", _numbers.Exact(level))),
                Effect = _words.Effect(relic, level),
            };
            if (status == RelicStatus.Awake)
            {
                var window = _relics.WindowMs(held, level);
                var left = Math.Max(0, (_shrines.WindowEndsAt(held) ?? now) - now);
                panel.AwakeFraction = window > 0 ? (float)(left / window) : 0;
                panel.AwakeLeft = _localizer.Tr("{time} left", ("time", _numbers.Duration(Math.Ceiling(left / 1000))));
                return panel;
            }

            var cost = _shrines.ActivationCost(held);
            panel.ActivateLabel = _localizer.Tr("Activate");
            panel.ActivatePrice = _prices.Of(new Dictionary<string, double> { [ManaPool.MANA] = cost });
            if (_mana.Amount < cost && _actions.SmallestFlask() is { } flask)
            {
                panel.FlaskLabel = _localizer.Tr("Use");
                panel.FlaskNote = _localizer.Tr("Flask ×{n}", ("n", _numbers.Exact(flask.Count)));
            }

            return panel;
        }

        public void Activate(DistrictState district)
        {
            if (_shrines.Hosted(district) is { } relic) _actions.Activate(relic);
        }

        public void UseFlask() => _actions.UseFlask();
    }
}
