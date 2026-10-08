using System.Collections.Generic;

namespace Kingdom.Sim.State
{
    // The simulated payer and its monthly budget.
    public sealed class PayerState
    {
        public string Profile { get; set; }
        public double ChosenAt { get; set; }
        public double MonthIndex { get; set; }
        public double SpentCentsThisMonth { get; set; }
        public List<Purchase> Purchases { get; set; }
        public double Refusals { get; set; }
    }
}
