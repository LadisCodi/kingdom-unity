using System.Collections.Generic;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.City
{
    public class TrainingBatchTests
    {
        [Test]
        public void GemsToFinish_ShouldChargeAGemAFewSecondsAndNeverNothing()
        {
            Assert.That(GemRush.GemsToFinish(0, 5), Is.EqualTo(1));
            Assert.That(GemRush.GemsToFinish(11, 5), Is.EqualTo(3));
            Assert.That(GemRush.GemsToFinish(6_000, 5), Is.EqualTo(1_200));
            Assert.That(GemRush.GemsToFinish(61_730, 5), Is.EqualTo(12_300), "rounded to three figures");
        }
    }
}
