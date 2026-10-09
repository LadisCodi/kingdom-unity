namespace Codigames.Kingdom.Army
{
    public interface IArmySettings
    {
        // Of the soldiers who fall, the share that reaches a bed instead of dying.
        double WoundedShare { get; }
        // A batch's mending against training as many: its price, and its time.
        double HealCostShare { get; }
        double HealTimeShare { get; }
    }
}
