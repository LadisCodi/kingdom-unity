namespace Codigames.Kingdom.City
{
    // Why a villager was not queued.
    public enum TrainRefusal
    {
        None,
        // Every bed in the city is taken or promised.
        NoRoom,
        CannotAfford,
    }
}
