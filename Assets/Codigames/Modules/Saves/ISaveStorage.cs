namespace Codigames.Modules.Saves
{
    // Port: where a save's text lives, by slot. Writing replaces the slot whole or not at all.
    public interface ISaveStorage
    {
        // The slot's text; null when there is none.
        string Read(string slot);

        void Write(string slot, string text);

        // Sets a slot's text aside under another name, so a save that could not be read is kept, not lost.
        void Keep(string slot, string asSlot);

        void Delete(string slot);
    }
}
