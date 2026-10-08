using NUnit.Framework;

namespace Codigames.Modules.Core.Tests
{
    public class VectorTests
    {
        [Test]
        public void Operators_ShouldWorkComponentWise()
        {
            var a = new Vector2(1f, 2f);
            var b = new Vector2(3f, 5f);

            Assert.That(a + b, Is.EqualTo(new Vector2(4f, 7f)));
            Assert.That(b - a, Is.EqualTo(new Vector2(2f, 3f)));
            Assert.That(a * 2f, Is.EqualTo(new Vector2(2f, 4f)));
            Assert.That(new Vector3(a, 3f).XY, Is.EqualTo(a));
        }

        [Test]
        public void Lerp_ShouldClampItsFraction()
        {
            var a = Vector2.Zero;
            var b = new Vector2(10f, 20f);

            Assert.That(Vector2.Lerp(a, b, 0.5f), Is.EqualTo(new Vector2(5f, 10f)));
            Assert.That(Vector2.Lerp(a, b, 2f), Is.EqualTo(b));
            Assert.That(Vector3.Lerp(Vector3.Zero, Vector3.One, -1f), Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void Magnitude_ShouldBeTheEuclideanLength()
        {
            Assert.That(new Vector2(3f, 4f).Magnitude, Is.EqualTo(5f));
            Assert.That(Vector3.Distance(Vector3.Zero, new Vector3(2f, 3f, 6f)), Is.EqualTo(7f));
        }
    }
}
