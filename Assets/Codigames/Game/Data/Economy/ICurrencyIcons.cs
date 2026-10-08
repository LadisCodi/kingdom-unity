namespace Codigames.Game.Data.Economy
{
    // A currency's icon, wherever an amount of it is shown.
    public interface ICurrencyIcons
    {
        UnityEngine.Sprite IconOf(string currency);
    }
}
