namespace Codigames.Kingdom.Store
{
    // What the offers read off the rest of the kingdom: its doors, its Townhall, the needs a player just felt, and
    // whether every slot a pack opens still fits under its ceiling.
    public interface IOfferContext
    {
        // A door by the data's id ("store", "heroes", "world"…).
        bool IsDoorOpen(string door);
        int TownhallLevel { get; }
        // manaLow, manaOut, explorersBusy, heroesBenched: is that need felt now?
        bool Feels(string need);
        bool SlotsFit(IProductDefinition product);
    }
}
