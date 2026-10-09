using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tutorial.State;

namespace Codigames.Kingdom.Doors
{
    // What has just opened, each once: a door of the game, or a book of research found. Taking them remembers them
    // opened for good, so a door never shuts again and nothing is announced twice. A kingdom from before the doors
    // announces nothing.
    public class Openings
    {
        private readonly Doors _doors;
        private readonly IBookshelf _shelf;
        private readonly ITechTree _tree;
        private readonly TutorialState _tutorial;

        public Openings(Doors doors, IBookshelf shelf, ITechTree tree, TutorialState tutorial)
        {
            _doors = doors;
            _shelf = shelf;
            _tree = tree;
            _tutorial = tutorial;
        }

        // Something opened: the doors and the books, in that order.
        public event Action<IReadOnlyList<DoorId>, IReadOnlyList<string>> Opened;

        public static string BookKey(string tome) => "book:" + tome;

        // Takes what has opened since the last time: remembered, and announced.
        public void Take()
        {
            if (_tutorial.Veteran) return;

            var doors = _doors.FreshlyOpen();
            foreach (var door in doors) _doors.MarkSeen(door);

            // The Kingdom's own tree is there from the first minute: it is never found.
            var books = _tree.Tomes.Skip(1).Where(t => _shelf.IsOpen(t) && !_tutorial.Seen.Contains(BookKey(t))).ToList();
            foreach (var book in books) _tutorial.Seen.Add(BookKey(book));

            if (doors.Count > 0 || books.Count > 0) Opened?.Invoke(doors, books);
        }
    }
}
