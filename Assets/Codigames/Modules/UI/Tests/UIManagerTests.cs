using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Codigames.Modules.UI.Tests
{
    public class UIManagerTests
    {
        private sealed class ShopView : IMenuView
        {
            public Task Show() => Task.CompletedTask;
            public Task Hide() => Task.CompletedTask;
            public void OnFocusGained() { }
            public void OnFocusLost() { }
        }

        private sealed class BagView : IMenuView
        {
            public Task Show() => Task.CompletedTask;
            public Task Hide() => Task.CompletedTask;
            public void OnFocusGained() { }
            public void OnFocusLost() { }
        }

        private sealed class HeroesView : IMenuView
        {
            public Task Show() => Task.CompletedTask;
            public Task Hide() => Task.CompletedTask;
            public void OnFocusGained() { }
            public void OnFocusLost() { }
        }

        private sealed class Views : IMenuViewFactory
        {
            public TView Resolve<TView>() where TView : class, IMenuView => (TView)Activator.CreateInstance(typeof(TView));
        }

        private sealed class Groups : IMenuGroups
        {
            // The shop and the bag are tabs of one group.
            public bool AreGrouped(Type a, Type b) => a != b && (a == typeof(ShopView) || a == typeof(BagView)) && (b == typeof(ShopView) || b == typeof(BagView));
        }

        private sealed class Closable<TView> : AbstractMenuPresenter<TView>, IClosableMenuPresenter where TView : class, IMenuView
        {
            public Closable(IMenuViewFactory views) : base(views) { }
            public UIManager Manager { get; set; }
            public void RequestClose() => Manager.HideMenu<TView>().Wait();
        }

        private Closable<ShopView> _shop;
        private Closable<BagView> _bag;
        private Closable<HeroesView> _heroes;
        private UIManager _ui;

        [SetUp]
        public void SetUp()
        {
            var views = new Views();
            _shop = new Closable<ShopView>(views);
            _bag = new Closable<BagView>(views);
            _heroes = new Closable<HeroesView>(views);
            _ui = new UIManager(() => new IMenuPresenter[] { _shop, _bag, _heroes }, new Groups());
            _shop.Manager = _bag.Manager = _heroes.Manager = _ui;
        }

        [Test]
        public async Task ShowMenu_ShouldCoverTheTopAndRevealItOnClose()
        {
            await _ui.ShowMenu<HeroesView>();
            await _ui.ShowMenu<ShopView>();

            Assert.That(_heroes.IsShown, Is.False);
            Assert.That(_shop.HasFocus, Is.True);

            _ui.CloseTopMost();

            Assert.That(_shop.IsShown, Is.False);
            Assert.That(_heroes.IsShown, Is.True);
            Assert.That(_heroes.HasFocus, Is.True);
        }

        [Test]
        public async Task ShowMenu_ShouldReplaceAMenuOfTheSameGroup()
        {
            await _ui.ShowMenu<HeroesView>();
            await _ui.ShowMenu<ShopView>();
            await _ui.ShowMenu<BagView>();

            _ui.CloseTopMost();

            // The bag replaced the shop, so closing it goes straight back to the heroes.
            Assert.That(_shop.IsShown, Is.False);
            Assert.That(_heroes.IsShown, Is.True);
        }

        [Test]
        public async Task CloseAll_ShouldCloseTheStackWithoutRevealingIt()
        {
            await _ui.ShowMenu<HeroesView>();
            await _ui.ShowMenu<ShopView>();

            await _ui.CloseAll();

            Assert.That(_shop.IsShown, Is.False);
            Assert.That(_heroes.IsShown, Is.False);
            Assert.That(_ui.HasOverlayOpen, Is.False);
        }

        [Test]
        public void ShowMenu_ShouldRefuseAMenuWithNoPresenter()
        {
            var ui = new UIManager(() => new List<IMenuPresenter>(), new Groups());

            Assert.ThrowsAsync<InvalidOperationException>(() => ui.ShowMenu<ShopView>());
        }

        [Test]
        public void CloseTopMost_ShouldDoNothingWithNoOverlay()
        {
            Assert.DoesNotThrow(() => _ui.CloseTopMost());
            Assert.That(_ui.HasOverlayOpen, Is.False);
        }
    }
}
