using System;
using NUnit.Framework;

namespace Codigames.Modules.Timeline.Tests
{
    public class TimelineTests
    {
        // A store that fills at a rate up to a cap, and an upgrade, due at a moment, that doubles the rate:
        // the classic pair whose order matters.
        private sealed class Producer : ITimedSystem
        {
            public double Stored;
            public double Rate = 1;
            public double Cap = 1000;
            public double? UpgradeAt;
            private double _last;

            public Producer(double start) => _last = start;

            public double? NextBoundary(double after) => UpgradeAt.HasValue && UpgradeAt.Value > after ? UpgradeAt : null;

            public void ApplyDue(double time)
            {
                if (UpgradeAt.HasValue && UpgradeAt.Value <= time)
                {
                    Rate *= 2;
                    UpgradeAt = null;
                }
            }

            public void RunUntil(double time)
            {
                Stored = Math.Min(Cap, Stored + (time - _last) * Rate);
                _last = time;
            }
        }

        [Test]
        public void Advance_ShouldEqualManySmallSteps()
        {
            var once = new Producer(0) { UpgradeAt = 300, Cap = 10_000 };
            var onceTimeline = new Timeline(0);
            onceTimeline.Register(once);
            onceTimeline.Advance(700);

            var stepped = new Producer(0) { UpgradeAt = 300, Cap = 10_000 };
            var steppedTimeline = new Timeline(0);
            steppedTimeline.Register(stepped);
            for (var t = 7; t <= 700; t += 7) steppedTimeline.Advance(t);

            Assert.That(once.Stored, Is.EqualTo(300 + 400 * 2));
            Assert.That(stepped.Stored, Is.EqualTo(once.Stored));
        }

        [Test]
        public void Advance_ShouldApplyWorkAtItsMomentNotAtTheEnd()
        {
            var producer = new Producer(0) { UpgradeAt = 100, Cap = 10_000 };
            var timeline = new Timeline(0);
            timeline.Register(producer);

            var steps = timeline.Advance(200);

            // 100 at the old rate, then 100 at the doubled one.
            Assert.That(producer.Stored, Is.EqualTo(300));
            Assert.That(steps, Is.EqualTo(1));
            Assert.That(timeline.LastAdvance, Is.EqualTo(200));
        }

        [Test]
        public void Advance_ShouldNeverOverflowACap()
        {
            var producer = new Producer(0) { Cap = 50 };
            var timeline = new Timeline(0);
            timeline.Register(producer);

            timeline.Advance(1_000_000);

            Assert.That(producer.Stored, Is.EqualTo(50));
        }
    }
}
