using System.Linq;
using Codigames.Kingdom.Notices;
using Codigames.Kingdom.Notices.State;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Notices
{
    public class InboxTests
    {
        private sealed class Settings : INoticeSettings
        {
            public int Shown { get; set; } = 4;
            public int Kept { get; set; } = 30;
        }

        private NoticesState _state;
        private Inbox _inbox;

        [SetUp]
        public void SetUp()
        {
            _state = new NoticesState();
            _inbox = new Inbox(_state, new Settings { Kept = 3 });
        }

        [Test]
        public void Post_ShouldFileNewestFirst_WhateverOrderTheyCome()
        {
            _inbox.Post(News.Built("a", 1, 200));
            _inbox.Post(News.Built("b", 1, 100));
            _inbox.Post(News.Built("c", 1, 300));

            Assert.That(_inbox.All.Select(n => n.District), Is.EqualTo(new[] { "c", "a", "b" }));
        }

        [Test]
        public void Post_ShouldFileAnEventOnce_HoweverOftenItIsSeen()
        {
            _inbox.Post(News.Built("a", 2, 100));
            _inbox.Post(News.Built("a", 2, 150));

            Assert.That(_inbox.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void Post_ShouldDropTheOldest_PastTheCap()
        {
            for (var i = 1; i <= 4; i++) _inbox.Post(News.Built("d" + i, 1, i * 100));

            Assert.That(_inbox.All.Select(n => n.District), Is.EqualTo(new[] { "d4", "d3", "d2" }));
        }

        [Test]
        public void Read_ShouldEmptyItsGroup_AndLeaveTheOthers()
        {
            _inbox.Post(News.Built("a", 1, 100));
            _inbox.Post(News.Sighted("stones", 200));
            var changes = 0;
            _inbox.Changed += () => changes++;

            _inbox.Read(NewsGroup.Built);

            Assert.That(_inbox.Of(NewsGroup.Built), Is.Empty);
            Assert.That(_inbox.Of(NewsGroup.Sighted).Count, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Groups_ShouldListTheNewestGroupFirst()
        {
            _inbox.Post(News.Sighted("stones", 100));
            _inbox.Post(News.Built("a", 1, 300));
            _inbox.Post(News.Sighted("farm", 200));

            Assert.That(_inbox.Groups, Is.EqualTo(new[] { NewsGroup.Built, NewsGroup.Sighted }));
        }
    }
}
