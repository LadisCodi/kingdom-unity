using System.Linq;
using Codigames.Game.Editor.Data;
using NUnit.Framework;

namespace Codigames.Game.Tests
{
    public class DataTests
    {
        // The balance in the project is legal, by every rule Kingdom states.
        [Test]
        public void Balance_ShouldHaveNoProblems()
        {
            var problems = DataValidator.Problems().ToList();

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
