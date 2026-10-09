namespace Codigames.Kingdom.Quests
{
    // An absolute goal's value: read off the kingdom as it stands now.
    public interface IQuestGoals
    {
        double Value(IQuestDefinition quest);
    }
}
