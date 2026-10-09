namespace Codigames.Game.UI.Data.Research
{
    // A chapter heading on a book's page: its name, and while it is shut what it still asks for.
    public readonly struct ChapterData
    {
        public ChapterData(string name, string gate, float top)
        {
            Name = name;
            Gate = gate;
            Top = top;
        }

        public string Name { get; }

        // Empty once the chapter is open.
        public string Gate { get; }

        // Page pixels from the top.
        public float Top { get; }
    }
}
