using System.Linq;
using Codigames.Kingdom.City.State;

namespace Codigames.Kingdom.Research
{
    // What opens each book: the Kingdom's tree from the first minute; the Sagas once a Tavern stands; the Atlas
    // once the Watchtower is claimed. Each is a fact the kingdom can only gain, so a book once open stays open.
    public class Bookshelf : IBookshelf
    {
        public const string KINGDOM = "Kingdom";
        public const string SAGAS = "Sagas";
        public const string ATLAS = "Atlas";

        private const string TAVERN = "Tavern";

        private readonly CityState _city;

        public Bookshelf(CityState city)
        {
            _city = city;
        }

        public bool IsOpen(string tome) => tome switch
        {
            KINGDOM => true,
            SAGAS => _city.Districts.Any(d => d.DefinitionId == TAVERN && d.Built),
            // The Watchtower is a landmark, and claiming one is not in the game yet.
            ATLAS => false,
            _ => false,
        };

        public bool IsFound(string tome) => tome == KINGDOM || IsOpen(tome);
    }
}
