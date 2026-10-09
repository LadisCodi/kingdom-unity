using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Research;
using UnityEngine;

namespace Codigames.Game.Data.Research
{
    // The shelf: its books in order, each with its bands.
    [CreateAssetMenu(fileName = "TechTree", menuName = "Kingdom/Data/Tech Tree")]
    public class TechTreeAsset : DataSettings, ITechTree
    {
        [SerializeField] private List<BookData> _books = new();

        public IReadOnlyList<BookData> Books => _books;

        public IReadOnlyList<string> Tomes => _books.Select(b => b.Id).ToList();

        public BookData Book(string tome) => _books.FirstOrDefault(b => b.Id == tome);

        public int Eras(string tome) => Book(tome)?.Eras.Count ?? 0;

        public int CellsToOpen(string tome, int era) => Era(tome, era)?.CellsToOpen ?? 0;

        public int? EraReward(string tome, int era) => Era(tome, era)?.Reward;

        private EraData Era(string tome, int era)
        {
            var book = Book(tome);
            return book != null && era >= 1 && era <= book.Eras.Count ? book.Eras[era - 1] : null;
        }

        public override IEnumerable<string> Problems()
        {
            if (_books.Count == 0 || _books[0].Id != Bookshelf.KINGDOM) yield return "The Kingdom's tree comes first.";
            foreach (var book in _books)
            {
                if (book.Eras.Count == 0) yield return $"{book.Id} has no band.";
                else if (book.Eras[0].CellsToOpen != 0) yield return $"{book.Id}'s first band opens with the book: it asks for no cells.";
            }
        }
    }
}
