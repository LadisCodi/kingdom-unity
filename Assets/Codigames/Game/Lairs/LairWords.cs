using System.Linq;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Localization;
using Vector2Int = Codigames.Modules.Core.Vector2Int;

namespace Codigames.Game.Lairs
{
    // What a lair is called in the player's language: the creature its threat reads as — derived, never a second
    // authored list — and the refusal its ground gives, naming whose it is, which is what sends the player to clear it.
    public class LairWords
    {
        private readonly Localizer _localizer;
        private readonly LairGround _ground;

        public LairWords(Localizer localizer, LairGround ground)
        {
            _localizer = localizer;
            _ground = ground;
        }

        public string Creature(ILairSite lair) => lair.Threat switch
        {
            "Warrior" => _localizer.Tr("Orcs"),
            "Lancer" => _localizer.Tr("Goblins"),
            "Archer" => _localizer.Tr("Harpies"),
            "Cavalry" => _localizer.Tr("Wolf riders"),
            "Any" => _localizer.Tr("A drake"),
            _ => _localizer.Tr("A warband"),
        };

        // One creature holds; a band hold.
        public string HoldsThisGround(ILairSite lair)
        {
            var creature = Creature(lair);
            return lair.Threat is "Any" or null
                ? _localizer.Tr("{creature} holds this ground", ("creature", creature))
                : _localizer.Tr("{creature} hold this ground", ("creature", creature));
        }

        // The refusal for a cell, naming the lair that holds it; the plain one when none does.
        public string HoldsThisGround(Vector2Int cell)
        {
            var id = _ground.HoldingAt(cell);
            var lair = _ground.All.FirstOrDefault(l => l.Id == id);
            return lair != null ? HoldsThisGround(lair) : _localizer.Tr("A lair holds this ground");
        }
    }
}
