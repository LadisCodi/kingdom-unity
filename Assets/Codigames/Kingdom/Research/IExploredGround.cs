namespace Codigames.Kingdom.Research
{
    // How much of the province the kingdom has opened up: the count a chapter of research waits for.
    public interface IExploredGround
    {
        int RevealedCount { get; }
    }
}
