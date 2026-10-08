using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Codigames.Modules.Core.Tests
{
    public class CatalogTests
    {
        private class Thing : IIdentifiable
        {
            public Thing(string id) => Id = id;
            public string Id { get; }
        }

        private sealed class SpecialThing : Thing
        {
            public SpecialThing(string id) : base(id) { }
        }

        [Test]
        public void Items_ShouldKeepTheAuthoredOrder()
        {
            var catalog = new Catalog<Thing>(new[] { new Thing("b"), new Thing("a"), new Thing("c") });

            Assert.That(catalog.Items, Has.Count.EqualTo(3));
            Assert.That(catalog.Items[0].Id, Is.EqualTo("b"));
            Assert.That(catalog.Items[2].Id, Is.EqualTo("c"));
        }

        [Test]
        public void Get_ShouldFindByIdAndThrowWhenMissing()
        {
            var catalog = new Catalog<Thing>(new Thing[] { new SpecialThing("farm") });

            Assert.That(catalog.Get<SpecialThing>("farm").Id, Is.EqualTo("farm"));
            Assert.That(catalog.TryGet("mill", out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => catalog.Get("mill"));
        }

        [Test]
        public void Constructor_ShouldRefuseTwoItemsWithOneId()
        {
            Assert.Throws<ArgumentException>(() => new Catalog<Thing>(new[] { new Thing("a"), new Thing("a") }));
        }
    }
}
