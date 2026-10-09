namespace Codigames.Kingdom.City
{
    // A surplus tier: the city's supply over its demand reaching `At` raises the rent by `Bonus` (0.05 = +5%).
    public readonly struct HarmonyTier
    {
        public HarmonyTier(double at, double bonus)
        {
            At = at;
            Bonus = bonus;
        }

        public double At { get; }
        public double Bonus { get; }
    }
}
