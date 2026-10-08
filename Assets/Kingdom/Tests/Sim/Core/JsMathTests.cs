using Kingdom.Sim.Core;
using NUnit.Framework;

namespace Kingdom.Tests.Sim.Core
{
    public class JsMathTests
    {
        [TestCase(2.5, 3)]
        [TestCase(3.5, 4)]
        [TestCase(-2.5, -2)]
        [TestCase(-2.6, -3)]
        [TestCase(0.49999999999999994, 0)]
        public void Round_ShouldRoundHalvesUpLikeJavaScript(double value, double expected)
        {
            Assert.That(JsMath.Round(value), Is.EqualTo(expected));
        }
    }
}
