using System.Linq;
using Codigames.Kingdom.Research;
using Codigames.Kingdom.Tests.Builders;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Research
{
    public class TechTreeRulesTests
    {
        private static string[] Problems(params ITechnology[] technologies)
            => TechTreeRules.Problems(new Catalog<ITechnology>(technologies), new ResearchFixture.FakeTree()).ToArray();

        [Test]
        public void ALegalPage_ShouldHaveNoProblems()
        {
            Assert.That(Problems(
                new TechBuilder("Forestry").In("Kingdom", 1, 0, 1).Build(),
                new TechBuilder("Farming").In("Kingdom", 1, 1, 0).Requires("Forestry").Build(),
                new TechBuilder("Masonry").In("Kingdom", 2, 2, 0).Requires("Farming").Build()), Is.Empty);
        }

        [Test]
        public void Problems_ShouldNameEveryBrokenRule()
        {
            var problems = Problems(
                new TechBuilder("Forestry").In("Kingdom", 1, 0, 1).Opens(UnlockKind.Harvest, "Forest").Build(),
                new TechBuilder("Woodcraft").In("Kingdom", 1, 0, 1).Opens(UnlockKind.Harvest, "Forest").Build(),
                new TechBuilder("Masonry").In("Kingdom", 1, 2, 0).Requires("Forestry").Build(),
                new TechBuilder("Ballads").In("Sagas", 1, 1, 3).Requires("Forestry", "Nowhere").Build(),
                new TechBuilder("Drums").In("Sagas", 4, 3, 1).Build());

            Assert.That(problems, Has.Some.Contains("share a slot"));
            Assert.That(problems, Has.Some.Contains("both open Harvest Forest"));
            Assert.That(problems, Has.Some.Contains("Masonry requires Forestry, which is not in the row right above it"));
            Assert.That(problems, Has.Some.Contains("from another book"));
            Assert.That(problems, Has.Some.Contains("Nowhere, which is not a technology"));
            Assert.That(problems, Has.Some.Contains("outside the page"));
            Assert.That(problems, Has.Some.Contains("band 4"));
        }
    }
}
