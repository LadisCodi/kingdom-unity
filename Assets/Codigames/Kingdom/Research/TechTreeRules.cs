using System.Collections.Generic;
using System.Linq;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Research
{
    // What a legal tree is: every placed card in a book and band that exist, in a slot of its own on a
    // three-column page, needing only cards of its own book in the row right above it (none on the first row),
    // and no two cards opening the same thing.
    public static class TechTreeRules
    {
        public const int COLUMNS = 3;

        public static IEnumerable<string> Problems(ICatalog<ITechnology> technologies, ITechTree tree)
        {
            var slots = new Dictionary<(string, int, int), string>();
            var gates = new Dictionary<(UnlockKind, string, int), string>();

            foreach (var tech in technologies.Items)
            {
                foreach (var id in tech.Requires.Where(id => !technologies.Contains(id)))
                    yield return $"{tech.Id} requires {id}, which is not a technology.";

                if (!tech.IsPlaced) continue;

                if (!tree.Tomes.Contains(tech.Tome)) yield return $"{tech.Id} is in {tech.Tome}, which is not a book.";
                else if (tech.Era < 1 || tech.Era > tree.Eras(tech.Tome)) yield return $"{tech.Id} is in band {tech.Era}, which {tech.Tome} does not have.";

                if (tech.Column < 0 || tech.Column >= COLUMNS) yield return $"{tech.Id} is outside the page's {COLUMNS} columns.";
                if (slots.TryGetValue((tech.Tome, tech.Row, tech.Column), out var other)) yield return $"{tech.Id} and {other} share a slot.";
                else slots[(tech.Tome, tech.Row, tech.Column)] = tech.Id;

                if (tech.Row == 0 && tech.Requires.Count > 0) yield return $"{tech.Id} is on the first row, so it requires nothing.";

                foreach (var id in tech.Requires)
                {
                    if (!technologies.TryGet(id, out var required)) continue;
                    if (required.Tome != tech.Tome) yield return $"{tech.Id} requires {id}, from another book.";
                    else if (!required.IsPlaced || required.Row != tech.Row - 1) yield return $"{tech.Id} requires {id}, which is not in the row right above it.";
                }

                foreach (var unlock in tech.Unlocks)
                {
                    var key = (unlock.Kind, unlock.Id, unlock.Level);
                    if (gates.TryGetValue(key, out var opener)) yield return $"{tech.Id} and {opener} both open {unlock.Kind} {unlock.Id}.";
                    else gates[key] = tech.Id;
                }
            }
        }
    }
}
