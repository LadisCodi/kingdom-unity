using System.Linq;
using System.Text.RegularExpressions;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Harvest;
using Codigames.Game.Data.Sites;
using Codigames.Game.UI.Research;
using Codigames.Kingdom.Quests;
using Codigames.Modules.Localization;

namespace Codigames.Game.UI.Quests
{
    // What a quest asks, generated from its goal: an imperative naming the goal and nothing else ("Build a Farm.",
    // "Collect 25 Wood."), short enough for the tracker. Spanish names are inflected by rule (the head noun takes the
    // plural, and an adjective straight after it), as the web does.
    public class QuestProse
    {
        private static readonly string[] LINKS = { "de", "del", "para", "con", "en" };

        private readonly Localizer _localizer;
        private readonly BuildingCollection _buildings;
        private readonly FeatureCollection _features;
        private readonly ProvinceSitesAsset _sites;
        private readonly TechProse _tech;
        private readonly NumberFormat _numbers;

        public QuestProse(Localizer localizer, BuildingCollection buildings, FeatureCollection features, ProvinceSitesAsset sites, TechProse tech,
            NumberFormat numbers)
        {
            _localizer = localizer;
            _buildings = buildings;
            _features = features;
            _sites = sites;
            _tech = tech;
            _numbers = numbers;
        }

        private bool Spanish => _localizer.Culture.TwoLetterISOLanguageName == "es";

        public string Line(IQuestDefinition quest)
        {
            var target = quest.GoalTarget;
            var amount = (int)quest.GoalAmount;
            var n = _numbers.Exact(amount);
            switch (quest.GoalType)
            {
                case GoalType.BuildDistrict:
                    return target == null
                        ? _localizer.Trn(amount, "Build {n} building.", "Build {n} buildings.", ("n", n))
                        : amount == 1
                            ? _localizer.Tr("Build {what}.", ("what", One(District(target))))
                            : _localizer.Tr("Build {n} {things}.", ("n", n), ("things", Plural(amount, District(target))));
                case GoalType.RepairDistrict:
                {
                    var ruins = _sites.Abandoned.Where(a => a.District == target).ToList();
                    if (amount == 1 && ruins.Count == 1)
                    {
                        var name = _localizer.Tr(ruins[0].Name);
                        return _localizer.Tr("Repair {what}.", ("what", char.ToLowerInvariant(name[0]) + name.Substring(1)));
                    }

                    var what = (target == null ? _localizer.Tr("building") : District(target)).ToLowerInvariant();
                    return amount == 1
                        ? _localizer.Tr("Repair {what}.", ("what", TheOld(what)))
                        : _localizer.Tr("Repair {n} old {things}.", ("n", n), ("things", Plural(amount, what)));
                }
                case GoalType.UpgradeDistrict:
                {
                    var what = target == null ? _localizer.Tr("building") : District(target);
                    var which = target == "Townhall" ? The(what) : One(what);
                    var level = quest.GoalLevel;
                    if (amount == 1)
                    {
                        return level == 0
                            ? _localizer.Tr("Upgrade {what}.", ("what", which))
                            : _localizer.Tr("Upgrade {what} to level {level}.", ("what", which), ("level", level));
                    }

                    return level == 0
                        ? _localizer.Tr("Upgrade {n} {things}.", ("n", n), ("things", Plural(amount, what)))
                        : _localizer.Tr("Upgrade {n} {things} to level {level}.", ("n", n), ("things", Plural(amount, what)), ("level", level));
                }
                case GoalType.CollectResource:
                    return _localizer.Tr("Collect {n} {coin}.", ("n", n), ("coin", Coin(target)));
                case GoalType.HoldResource:
                    return _localizer.Tr("Hold {n} {coin} at once.", ("n", n), ("coin", Coin(target)));
                case GoalType.ReachPopulation:
                    return _localizer.Trn(amount, "Grow to {n} villager.", "Grow to {n} villagers.", ("n", n));
                case GoalType.CompleteTech:
                    return target == null ? _localizer.Tr("Finish a technology.") : _localizer.Tr("Research {name}.", ("name", _tech.Name(target)));
                case GoalType.CompleteTechs:
                    return _localizer.Trn(amount, "Research {n} technology.", "Research {n} technologies.", ("n", n));
                case GoalType.AssignWorkers:
                    return _localizer.Trn(amount, "Put {n} villager to work.", "Put {n} villagers to work.", ("n", n));
                case GoalType.WorkInReach:
                {
                    var building = target == null ? null : _buildings.Cards.OfType<BuildingAsset>().FirstOrDefault(b => b.Id == target);
                    var source = building?.Production.HarvestSources.FirstOrDefault();
                    var feature = _features.Entries.OfType<FeatureAsset>().FirstOrDefault(f => f.Source == source);
                    var what = (feature == null ? _localizer.Tr("field") : _localizer.Tr(feature.DisplayName)).ToLowerInvariant();
                    return _localizer.Tr("Move {what} beside {n} {things}.",
                        ("what", The(target == null ? _localizer.Tr("building") : District(target))), ("n", n), ("things", Plural(amount, what)));
                }
                case GoalType.TrainArmy:
                    return _localizer.Trn(amount, "Train {n} soldier.", "Train {n} soldiers.", ("n", n));
                case GoalType.CollectTaps:
                    return _localizer.Tr("Tap {n} times.", ("n", n));
                case GoalType.DiscoverCells:
                    return _localizer.Trn(amount, "Clear the fog from {n} tile in all.", "Clear the fog from {n} tiles in all.", ("n", n));
                case GoalType.DiscoverFeature:
                    return target == null
                        ? _localizer.Trn(amount, "Find {n} thing.", "Find {n} things.", ("n", n))
                        : _localizer.Tr("Find {n} {things}.", ("n", n), ("things", Plural(amount, Feature(target))));
                case GoalType.ClaimLandmarks:
                {
                    if (target == null) return _localizer.Trn(amount, "Claim {n} landmark.", "Claim {n} landmarks.", ("n", n));
                    var name = _localizer.Tr(_sites.KindOf(target)?.Name ?? target);
                    return amount == 1
                        ? _localizer.Tr("Claim {what}.", ("what", The(name)))
                        : _localizer.Tr("Claim {n} {things}.", ("n", n), ("things", Plural(amount, name)));
                }
                case GoalType.FindLairs:
                    return _localizer.Trn(amount, "Find {n} lair.", "Find {n} lairs.", ("n", n));
                case GoalType.ClearLairs:
                    return _localizer.Trn(amount, "Clear {n} lair.", "Clear {n} lairs.", ("n", n));
                case GoalType.OwnArtifacts:
                    return _localizer.Trn(amount, "Own {n} relic.", "Own {n} relics.", ("n", n));
                case GoalType.OwnHeroes:
                    return _localizer.Trn(amount, "Own {n} hero.", "Own {n} heroes.", ("n", n));
                default:
                    return string.Empty;
            }
        }

        private string District(string id) => id switch
        {
            "AnyDecoration" => _localizer.Tr("decoration"),
            "AnyProducer" => _localizer.Tr("production building"),
            "AnyHall" => _localizer.Tr("military hall"),
            "AnyWorkshop" => _localizer.Tr("workshop"),
            _ => _localizer.Tr(_buildings.Cards.FirstOrDefault(c => c.Id == id)?.DisplayName ?? id),
        };

        private string Feature(string id)
            => _localizer.Tr(_features.Entries.OfType<FeatureAsset>().FirstOrDefault(f => f.Id == id)?.DisplayName ?? id);

        private string Coin(string id) => id switch
        {
            "Gold" => _localizer.Tr("Gold"),
            "Food" => _localizer.Tr("Food"),
            "Wood" => _localizer.Tr("Wood"),
            "Stone" => _localizer.Tr("Stone"),
            "Mana" => _localizer.Tr("Mana"),
            "Knowledge" => _localizer.Tr("Knowledge"),
            "Stardust" => _localizer.Tr("Stardust"),
            "Gems" => _localizer.Tr("Gems"),
            _ => id ?? string.Empty,
        };

        // ---- English: a few names do not take an s; a name ending in s is mass or already plural.

        private static bool IsMass(string one) => one == "Housing" || Regex.IsMatch(one, "s$", RegexOptions.IgnoreCase);

        private static string Irregular(string one) => one switch
        {
            "technology" => "technologies",
            "hero" => "heroes",
            _ => one + "s",
        };

        private string Plural(int n, string one)
        {
            if (Spanish) return n == 1 ? one : PluralEs(one);
            return n == 1 || IsMass(one) ? one : Irregular(one);
        }

        private string One(string name)
        {
            if (Spanish) return EndsInS(HeadOf(name)) ? name : $"{(FeminineEs(name) ? "una" : "un")} {name}";
            return IsMass(name) ? name : $"a {name}";
        }

        private string The(string name)
        {
            if (!Spanish) return $"the {name}";
            var plural = EndsInS(HeadOf(name));
            var feminine = FeminineEs(name);
            return $"{(plural ? (feminine ? "las" : "los") : (feminine ? "la" : "el"))} {name}";
        }

        private string TheOld(string name) => Spanish ? $"{The(name)} en ruinas" : $"the old {name}";

        // ---- Spanish: the head noun takes the plural, and an adjective straight after it.

        private static string HeadOf(string name) => name.Split(' ')[0];

        private static bool EndsInS(string word) => Regex.IsMatch(word, "s$", RegexOptions.IgnoreCase);

        private static bool FeminineEs(string name) => Regex.IsMatch(HeadOf(name), "(a|ión|dad)$", RegexOptions.IgnoreCase);

        private static string PluralWordEs(string word)
        {
            if (EndsInS(word)) return word;
            if (Regex.IsMatch(word, "ión$", RegexOptions.IgnoreCase)) return word.Substring(0, word.Length - 3) + "iones";
            if (Regex.IsMatch(word, "z$", RegexOptions.IgnoreCase)) return word.Substring(0, word.Length - 1) + "ces";
            return Regex.IsMatch(word, "[aeiouáéó]$", RegexOptions.IgnoreCase) ? word + "s" : word + "es";
        }

        private static string PluralEs(string name)
        {
            var words = name.Split(' ');
            if (EndsInS(words[0])) return name;

            words[0] = PluralWordEs(words[0]);
            if (words.Length > 1 && !LINKS.Contains(words[1].ToLowerInvariant())) words[1] = PluralWordEs(words[1]);
            return string.Join(" ", words);
        }
    }
}
