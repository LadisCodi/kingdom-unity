namespace Codigames.Kingdom.Crews.State
{
    public enum WorkerActivity
    {
        // At the door: nothing to work, or the store has no room.
        Idle,
        MovingToCell,
        Working,
        // Walking the load home.
        MovingHome,
    }
}
