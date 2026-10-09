namespace Codigames.Kingdom.Research
{
    // Which books are open. A book opens on a fact about the world, never on a research, and stays open.
    public interface IBookshelf
    {
        bool IsOpen(string tome);

        // Not on the shelf until it is found (the Kingdom's tree is always there).
        bool IsFound(string tome);
    }
}
