using NUnit.Framework;

namespace Codigames.Modules.Lifecycle.Tests
{
    public class AppLifecycleTests
    {
        private AppLifecycle _lifecycle;
        private int _suspended;

        [SetUp]
        public void SetUp()
        {
            _lifecycle = new AppLifecycle();
            _suspended = 0;
            _lifecycle.Suspending += () => _suspended++;
        }

        [Test]
        public void Paused_ShouldSuspendOnlyWhenPausing()
        {
            _lifecycle.Paused(false);
            _lifecycle.Paused(true);

            Assert.That(_suspended, Is.EqualTo(1));
        }

        [Test]
        public void FocusChanged_ShouldSuspendOnlyWhenFocusIsLost()
        {
            _lifecycle.FocusChanged(true);
            _lifecycle.FocusChanged(false);

            Assert.That(_suspended, Is.EqualTo(1));
        }

        [Test]
        public void Quitting_ShouldSuspend()
        {
            _lifecycle.Quitting();

            Assert.That(_suspended, Is.EqualTo(1));
        }
    }
}
