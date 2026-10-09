namespace Codigames.Kingdom.Quests
{
    // How far along the quest chain the kingdom is: the active quest's place, past the end once it is done.
    public interface IChainPosition
    {
        int Index { get; }
    }
}
