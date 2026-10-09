namespace Codigames.Game.UI.Data
{
    // A working building's crew on its card: how many work there of how many may, and the − and + that change it.
    public sealed class CrewStripData
    {
        public CrewStripData(string count, bool canRemove, bool canAdd, string note)
        {
            Count = count;
            CanRemove = canRemove;
            CanAdd = canAdd;
            Note = note;
        }

        // "2 / 3".
        public string Count { get; }
        public bool CanRemove { get; }
        public bool CanAdd { get; }

        // A line under it: what the crew makes, or why no one can be added.
        public string Note { get; }
    }
}
