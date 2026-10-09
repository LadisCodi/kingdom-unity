namespace Codigames.Kingdom.Quests
{
    // A target that names one kind of building, or a group of them by its token (AnyDecoration, AnyProducer).
    public interface IBuildingGroups
    {
        bool Names(string target, string definitionId);
    }
}
