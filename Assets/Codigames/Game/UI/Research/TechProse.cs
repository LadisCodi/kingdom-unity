using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Research;
using Codigames.Kingdom.Harvest;
using Codigames.Kingdom.Research;
using Codigames.Modules.Core;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Research
{
    // What a technology says about itself, generated from what it does: "Unlocks the Farm", "+5% build speed".
    // Only a mechanic, whose effect is code, carries a written line.
    public class TechProse
    {
        private static readonly Regex AIMED = new(@"\[([^\]]*)\]", RegexOptions.Compiled);
        private static readonly Regex SPACES = new(@"\s+", RegexOptions.Compiled);
        private static readonly string[] ROMAN = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        private readonly Localizer _localizer;
        private readonly IBuildingCards _buildings;
        private readonly ICatalog<IHarvestSource> _sources;
        private readonly ITechnologyCards _cards;
        private readonly NumberFormat _numbers;

        public TechProse(Localizer localizer, IBuildingCards buildings, ICatalog<IHarvestSource> sources, ITechnologyCards cards,
            NumberFormat numbers)
        {
            _localizer = localizer;
            _buildings = buildings;
            _sources = sources;
            _cards = cards;
            _numbers = numbers;
        }

        // Everything it does, as one line.
        public string Line(ITechnology tech)
        {
            if (tech.Unlocks.Count > 0) return _localizer.Tr("Unlocks {what}", ("what", Serial(UnlockClauses(tech.Unlocks))));
            if (tech.Effects.Count > 0) return string.Join(" · ", EffectGroups(tech.Effects).Where(s => s != ""));

            var prose = _cards.Card(tech.Id)?.Description?.Trim() ?? "";
            return _localizer.Tr(prose);
        }

        public string Name(string id) => _localizer.Tr(_cards.Card(id)?.DisplayName ?? id);

        private string DistrictName(string id)
            => _localizer.Tr(_buildings.Cards.FirstOrDefault(c => c.Id == id)?.DisplayName ?? id);

        // Units are not in the game yet: their id is their name until they are.
        private string UnitName(string id) => _localizer.Tr(id);

        private string HarvestOne(string id) => id switch
        {
            "Forest" => _localizer.Tr("a forest"),
            "Crops" => _localizer.Tr("a farm plot"),
            "Berries" => _localizer.Tr("a berry bush"),
            "Meat" => _localizer.Tr("wild game"),
            "Fish" => _localizer.Tr("a shoal"),
            "Stone" => _localizer.Tr("a mountain"),
            "MountainIron" => _localizer.Tr("an iron mountain"),
            "MountainGold" => _localizer.Tr("a gold mountain"),
            _ => id,
        };

        private string HarvestMany(string id) => id switch
        {
            "Forest" => _localizer.Tr("the forests"),
            "Crops" => _localizer.Tr("crop plots"),
            "Berries" => _localizer.Tr("the berry bushes"),
            "Meat" => _localizer.Tr("the wild game"),
            "Fish" => _localizer.Tr("the fish shoals"),
            "Stone" => _localizer.Tr("the mountains"),
            "MountainIron" => _localizer.Tr("the iron mountains"),
            "MountainGold" => _localizer.Tr("the gold mountains"),
            _ => id,
        };

        private string TerrainName(string id) => id switch
        {
            "Grassland" => _localizer.Tr("the grasslands"),
            "Plains" => _localizer.Tr("the plains"),
            "Desert" => _localizer.Tr("the desert"),
            "Snow" => _localizer.Tr("the snows"),
            "Tundra" => _localizer.Tr("the tundra"),
            "Water" => _localizer.Tr("the open water"),
            _ => id,
        };

        private string TagName(string id) => id switch
        {
            "Melee" => _localizer.Tr("tag::Melee"),
            "Distance" => _localizer.Tr("tag::Distance"),
            "Mounted" => _localizer.Tr("tag::Mounted"),
            _ => id,
        };

        private string ResourceName(string id) => id switch
        {
            "Gold" => _localizer.Tr("Gold"),
            "Food" => _localizer.Tr("Food"),
            "Wood" => _localizer.Tr("Wood"),
            "Stone" => _localizer.Tr("Stone"),
            _ => id ?? "",
        };

        private string Serial(IReadOnlyList<string> parts)
            => parts.Count < 2
                ? parts.FirstOrDefault() ?? ""
                : _localizer.Tr("{list} and {last}", ("list", string.Join(", ", parts.Take(parts.Count - 1))), ("last", parts[^1]));

        private string UnlockPhrase(TechUnlock unlock) => unlock.Kind switch
        {
            UnlockKind.District => _localizer.Tr("the {name}", ("name", DistrictName(unlock.Id))),
            UnlockKind.DistrictLevel => _localizer.Tr("{name} level {n}", ("name", DistrictName(unlock.Id)), ("n", unlock.Level)),
            UnlockKind.DistrictCount => _localizer.Tr("one more {name}", ("name", DistrictName(unlock.Id))),
            UnlockKind.Unit => _localizer.Tr("the {name}", ("name", UnitName(unlock.Id))),
            UnlockKind.Evolution => $"{UnitName(unlock.Id)} {(unlock.Level < ROMAN.Length ? ROMAN[unlock.Level] : unlock.Level.ToString())}",
            UnlockKind.Harvest => HarvestMany(unlock.Id),
            UnlockKind.Terrain => TerrainName(unlock.Id),
            UnlockKind.WorldUpgrade => _localizer.Tr("the {name} on the world board", ("name", _localizer.Tr(unlock.Id))),
            _ => _localizer.Tr("nothing"),
        };

        // Building levels that share a number collapse into one clause, and one set of buildings reaching several
        // levels is one clause too.
        private List<string> UnlockClauses(IReadOnlyList<TechUnlock> unlocks)
        {
            var byLevel = new List<(int Level, List<string> Names)>();
            var rest = new List<string>();
            foreach (var unlock in unlocks)
            {
                if (unlock.Kind != UnlockKind.DistrictLevel)
                {
                    rest.Add(UnlockPhrase(unlock));
                    continue;
                }

                var at = byLevel.FindIndex(l => l.Level == unlock.Level);
                if (at < 0) byLevel.Add((unlock.Level, new List<string> { DistrictName(unlock.Id) }));
                else byLevel[at].Names.Add(DistrictName(unlock.Id));
            }

            var bySet = new List<(List<string> Names, List<int> Levels)>();
            foreach (var (level, names) in byLevel)
            {
                var at = bySet.FindIndex(s => s.Names.SequenceEqual(names));
                if (at < 0) bySet.Add((names, new List<int> { level }));
                else bySet[at].Levels.Add(level);
            }

            var clauses = bySet.Select(set =>
            {
                var which = set.Levels.Count == 1
                    ? _localizer.Tr("level {n}", ("n", set.Levels[0]))
                    : _localizer.Tr("levels {list}", ("list", Serial(set.Levels.Select(l => l.ToString()).ToList())));
                return set.Names.Count == 1
                    ? _localizer.Tr("{name} {which}", ("name", set.Names[0]), ("which", which))
                    : _localizer.Tr("{names} at {which}", ("names", Serial(set.Names)), ("which", which));
            }).ToList();
            clauses.AddRange(rest);
            return clauses;
        }

        private IEnumerable<string> EffectGroups(IReadOnlyList<TechEffect> effects)
        {
            var groups = new List<List<TechEffect>>();
            foreach (var effect in effects)
            {
                var group = effect.Target == TargetKind.Global
                    ? null
                    : groups.FirstOrDefault(g => g[0].Target == effect.Target && g[0].Stat == effect.Stat && g[0].Op == effect.Op
                                                 && g[0].Value == effect.Value);
                if (group == null) groups.Add(new List<TechEffect> { effect });
                else group.Add(effect);
            }

            return groups.Select(g => g.Count == 1 ? Sentence(g[0], null) : Sentence(g[0], Serial(g.Select(TargetPhrase).ToList())));
        }

        private string TargetPhrase(TechEffect effect) => effect.Target switch
        {
            TargetKind.District => DistrictName(effect.TargetId),
            TargetKind.Unit => UnitName(effect.TargetId),
            TargetKind.Harvest => HarvestOne(effect.TargetId),
            TargetKind.UnitTag => TagName(effect.TargetId),
            TargetKind.WorldDistrict => _localizer.Tr(effect.TargetId),
            _ => effect.TargetId ?? "",
        };

        private string Sentence(TechEffect effect, string targets)
        {
            var template = TechStatSentences.For(effect.Stat, effect.Op);
            if (template == null) return "";

            var aimed = effect.Target != TargetKind.Global;
            var coin = effect.Target == TargetKind.Harvest && _sources.TryGet(effect.TargetId, out var source) ? ResourceName(source.Currency) : "";
            var text = AIMED.Replace(_localizer.Tr(template), m => aimed ? m.Groups[1].Value : "")
                .Replace("{v}", Amount(effect.Value, effect.Op))
                .Replace("{pct}", Signed(System.Math.Round(effect.Value * 1000) / 10) + "%")
                .Replace("{target}", targets ?? TargetPhrase(effect))
                .Replace("{resource}", coin);
            return SPACES.Replace(text, " ").Trim();
        }

        private string Amount(double value, EffectOp op) => op == EffectOp.Percent ? Signed(value) + "%" : Signed(value);

        private string Signed(double value) => (value < 0 ? "−" : "+") + _numbers.Number(System.Math.Abs(value), 2);
    }
}
