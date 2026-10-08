using System;
using Kingdom.Sim.Core;
using NUnit.Framework;

namespace Kingdom.Tests.Sim.Core
{
    public class RandTests
    {
        // Golden values from the web prototype's rng.ts: the same seed and parts must give the same bits.
        private static readonly object[] GOLDEN =
        {
            new object[] { 0u, new object[0], 0.4534318521618843, 76 },
            new object[] { 1u, new object[] { "a" }, 0.8610502446535975, 41 },
            new object[] { 42u, new object[] { "heroBag", "Isolde", 3 }, 0.8361963222268969, 57 },
            new object[] { 3735928559u, new object[] { "lair", "Barrow", 2 }, 0.024526695488020778, 55 },
            new object[] { 4294967295u, new object[] { "respawn", "12,7", 5 }, 0.08939199452288449, 93 },
            new object[] { 123456789u, new object[] { "ñandú", -4, "x" }, 0.5630705321673304, 21 },
            new object[] { 7u, new object[] { "ab", "c" }, 0.16317237517796457, 15 },
            new object[] { 7u, new object[] { "a", "bc" }, 0.7647235728800297, 36 },
        };

        [TestCaseSource(nameof(GOLDEN))]
        public void Value_ShouldMatchTheWebPrototype(uint seed, object[] parts, double value, int int100)
        {
            Assert.That(Rand.Value(seed, parts), Is.EqualTo(value));
            Assert.That(Rand.Int(seed, 100, parts), Is.EqualTo(int100));
        }

        [Test]
        public void Value_ShouldNotCollideAcrossPartBoundaries()
        {
            Assert.That(Rand.Value(7, "ab", "c"), Is.Not.EqualTo(Rand.Value(7, "a", "bc")));
        }

        [Test]
        public void Int_ShouldBeZeroForAnEmptyRange()
        {
            Assert.That(Rand.Int(7, 0, "x"), Is.EqualTo(0));
        }

        [Test]
        public void Value_ShouldRefuseAFractionalPart()
        {
            Assert.Throws<ArgumentException>(() => Rand.Value(7, 0.5));
        }
    }
}
