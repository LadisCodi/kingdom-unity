namespace Codigames.Modules.Wallet
{
    // Port: the most a wallet may hold of a currency; null = no cap. A cap may move (a pool that grows).
    public interface ICurrencyCaps
    {
        double? CapOf(string currency);
    }
}
