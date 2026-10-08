using System.Collections.Generic;
using NUnit.Framework;

namespace Codigames.Modules.Wallet.Tests
{
    public class WalletTests
    {
        private sealed class Caps : ICurrencyCaps
        {
            public readonly Dictionary<string, double> Values = new();
            public double? CapOf(string currency) => Values.TryGetValue(currency, out var cap) ? cap : (double?)null;
        }

        private static Dictionary<string, double> Cost(params (string Id, double Amount)[] lines)
        {
            var cost = new Dictionary<string, double>();
            foreach (var (id, amount) in lines) cost[id] = amount;
            return cost;
        }

        [Test]
        public void TryPay_ShouldPayAllOrNothing()
        {
            var wallet = new Wallet(balances: Cost(("Gold", 100), ("Wood", 5)));

            Assert.That(wallet.TryPay(Cost(("Gold", 50), ("Wood", 10))), Is.False);
            Assert.That(wallet.Get("Gold"), Is.EqualTo(100));

            Assert.That(wallet.TryPay(Cost(("Gold", 50), ("Wood", 5))), Is.True);
            Assert.That(wallet.Get("Gold"), Is.EqualTo(50));
            Assert.That(wallet.Get("Wood"), Is.EqualTo(0));
        }

        [Test]
        public void Add_ShouldStopAtTheCapAndSayWhatItTook()
        {
            var caps = new Caps();
            caps.Values["Mana"] = 100;
            var wallet = new Wallet(caps, Cost(("Mana", 90)));

            Assert.That(wallet.Add("Mana", 25), Is.EqualTo(10));
            Assert.That(wallet.Get("Mana"), Is.EqualTo(100));
            Assert.That(wallet.Add("Mana", 5), Is.EqualTo(0));
        }

        [Test]
        public void Add_ShouldKeepWhatIsOverAFallenCap()
        {
            var caps = new Caps();
            caps.Values["Mana"] = 50;
            var wallet = new Wallet(caps, Cost(("Mana", 80)));

            Assert.That(wallet.Add("Mana", 10), Is.EqualTo(0));
            Assert.That(wallet.Get("Mana"), Is.EqualTo(80));
        }

        [Test]
        public void Changed_ShouldTellTheNewAmount()
        {
            var wallet = new Wallet();
            var seen = new List<(string, double)>();
            wallet.Changed += (id, amount) => seen.Add((id, amount));

            wallet.Add("Gold", 30);
            wallet.TryPay(Cost(("Gold", 10)));

            Assert.That(seen, Is.EqualTo(new[] { ("Gold", 30d), ("Gold", 20d) }));
        }
    }
}
