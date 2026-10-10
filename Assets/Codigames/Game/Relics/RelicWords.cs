using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.Relics;
using Codigames.Kingdom.Battles;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Localization;

namespace Codigames.Game.Relics
{
    // One number a relic is judged on at a level: stable key, icon, name, value.
    public readonly struct RelicStatLine
    {
        public RelicStatLine(string key, string icon, string label, string value)
        {
            Key = key;
            Icon = icon;
            Label = label;
            Value = value;
        }

        public string Key { get; }
        public string Icon { get; }
        public string Label { get; }
        public string Value { get; }
    }

    // The same stat at a level and the next, and whether the level moves it.
    public readonly struct RelicStatChange
    {
        public RelicStatChange(RelicStatLine now, string to)
        {
            Now = now;
            To = to;
        }

        public RelicStatLine Now { get; }
        public string To { get; }
        public bool Changed => To != Now.Value;
    }

    // A relic in the player's language (the web's relicStats): what each number it moves is called and drawn with,
    // its value as a percent, its tiles at a level against the next, and its sentence — the same numbers in it, ending
    // on its window for a city relic, since an activation is the only way one ever acts.
    public class RelicWords
    {
        private readonly Localizer _localizer;
        private readonly NumberFormat _numbers;
        private readonly Kingdom.Relics.Relics _relics;

        public RelicWords(Localizer localizer, NumberFormat numbers, Kingdom.Relics.Relics relics)
        {
            _localizer = localizer;
            _numbers = numbers;
            _relics = relics;
        }

        // What a stat is called on a relic's card, and its icon; keyed by the stat, so two relics that move the same
        // number say the same words about it. A speed is named as one.
        public (string Icon, string Label)? Face(string stat) => stat switch
        {
            RelicStats.RECOVERY_SPEED => ("hourglass", _localizer.Tr("Recovery speed")),
            RelicStats.HARVEST_STOCK => ("Wood", _localizer.Tr("Natural resources")),
            RelicStats.UNITS_PER_STRIKE => ("plus", _localizer.Tr("Per swing and tap")),
            RelicStats.TRAINING_SPEED => ("army", _localizer.Tr("Training speed")),
            RelicStats.WORKER_STRIKE_SPEED => ("clock", _localizer.Tr("Crew swing")),
            RelicStats.WORKER_SPEED => ("workers", _localizer.Tr("Crew walk")),
            RelicStats.TAX_RATE => ("Gold", _localizer.Tr("Tax rate")),
            "stardustYield" => ("Stardust", _localizer.Tr("Stardust from rooms")),
            "roomHaul" => ("dungeon", _localizer.Tr("A room’s gold and stone")),
            "armyCap" => ("army", _localizer.Tr("Army the halls field")),
            "worldImprovementYield" => ("build", _localizer.Tr("District yield")),
            _ => null,
        };

        // How big a multiplier is, in the player's terms: 320%, never ×4.20.
        public string Percent(double value) => _numbers.Exact(Combat.JsRound(Math.Max(0, value - 1) * 100)) + "%";

        // +320% for a multiplier, +3 for a flat term.
        private string Say(bool add, double value) => add ? "+" + _numbers.Number(value, 1) : "+" + Percent(value);

        // The cells a Chebyshev square of this reach covers: radius 2 is 25.
        public string Cells(int radius) => _localizer.Tr("{n} cells", ("n", _numbers.Exact((2 * radius + 1) * (2 * radius + 1))));

        // Everything the relic is worth at `level`, pure in the level so two reads zip into honest pairs.
        public IReadOnlyList<RelicStatLine> StatsAt(RelicAsset relic, int level)
        {
            var value = _relics.Value(relic.Id, level);
            var lines = new List<RelicStatLine>();
            foreach (var stat in relic.Stats)
                if (Face(stat.Stat) is { } face) lines.Add(new RelicStatLine(stat.Stat, face.Icon, face.Label, Say(stat.Add, value)));
            if (relic.Kind == RelicKind.City)
            {
                lines.Add(new RelicStatLine("aura", "compass", _localizer.Tr("Aura"), Cells(_relics.Radius(relic.Id, level))));
                lines.Add(new RelicStatLine("window", "hourglass", _localizer.Tr("Awake for"), _numbers.Duration(_relics.WindowMs(relic.Id, level) / 1000)));
            }

            return lines;
        }

        public IReadOnlyList<RelicStatChange> Changes(RelicAsset relic, int level)
        {
            var after = StatsAt(relic, level + 1).ToDictionary(s => s.Key, s => s.Value);
            return StatsAt(relic, level).Select(s => new RelicStatChange(s, after.TryGetValue(s.Key, out var to) ? to : s.Value)).ToList();
        }

        // What the relic is for, with its numbers in it.
        public string Story(RelicAsset relic, int level)
        {
            var story = _localizer.Tr(relic.Story, ("p", Percent(_relics.Value(relic.Id, level))));
            return relic.Kind == RelicKind.City
                ? _localizer.Tr("{story}, for {time}.", ("story", story), ("time", SpokenWindow(_relics.WindowMs(relic.Id, level))))
                : _localizer.Tr("{story}.", ("story", story));
        }

        // What its number does now, in a line: "Your villagers pay +10%"; a speed reads as faster.
        public string Effect(RelicAsset relic, int level)
        {
            var subject = _localizer.Tr(relic.Subject);
            var value = _relics.Value(relic.Id, Math.Max(1, level));
            var first = relic.Stats.FirstOrDefault();
            if (first.Add) return subject + " +" + _numbers.Number(value, 1);
            return first.Stat != null && first.Stat.EndsWith("Speed", StringComparison.Ordinal)
                ? _localizer.Tr("{subject} {pct} faster", ("subject", subject), ("pct", Percent(value)))
                : subject + " +" + Percent(value);
        }

        // Its effect in two words: "+10% tax".
        public string Short(RelicAsset relic, int level)
        {
            var value = _relics.Value(relic.Id, Math.Max(1, level));
            var amount = relic.Stats.FirstOrDefault().Add ? "+" + _numbers.Number(value, 1) : "+" + Percent(value);
            return amount + " " + _localizer.Tr(relic.Short);
        }

        // A window as a sentence says it: 30 minutes, 1 hour, 8 hours.
        public string SpokenWindow(double ms)
        {
            var minutes = (int)Math.Round(ms / 60_000);
            if (minutes < 60 || minutes % 60 != 0)
                return _localizer.Trn(minutes, "{n} minute", "{n} minutes", ("n", _numbers.Exact(minutes)));
            var hours = minutes / 60;
            return _localizer.Trn(hours, "{n} hour", "{n} hours", ("n", _numbers.Exact(hours)));
        }
    }
}
