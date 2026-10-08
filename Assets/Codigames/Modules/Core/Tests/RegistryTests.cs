using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Codigames.Modules.Core.Tests
{
    public class RegistryTests
    {
        private sealed class Thing : IIdentifiable
        {
            public Thing(string id) => Id = id;
            public string Id { get; }
        }

        [Test]
        public void Register_ShouldAddAndAnnounce()
        {
            var registry = new Registry<Thing>();
            var announced = new List<string>();
            registry.Registered += t => announced.Add(t.Id);

            registry.Register(new Thing("a"));

            Assert.That(registry.Contains("a"), Is.True);
            Assert.That(announced, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void Register_ShouldRefuseATakenId()
        {
            var registry = new Registry<Thing>();
            registry.Register(new Thing("a"));

            Assert.Throws<InvalidOperationException>(() => registry.Register(new Thing("a")));
        }

        [Test]
        public void Unregister_ShouldRemoveAndAnnounce()
        {
            var registry = new Registry<Thing>();
            var removed = new List<string>();
            registry.Unregistered += t => removed.Add(t.Id);
            registry.Register(new Thing("a"));

            Assert.That(registry.Unregister("a"), Is.True);
            Assert.That(registry.Unregister("a"), Is.False);
            Assert.That(registry.Items, Is.Empty);
            Assert.That(removed, Is.EqualTo(new[] { "a" }));
        }
    }
}
