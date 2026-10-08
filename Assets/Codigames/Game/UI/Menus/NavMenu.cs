using System;
using System.Collections.Generic;
using Codigames.Game.UI.Widgets;
using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.UI.Menus
{
    // The nav bar: a wooden beam along the bottom with a tab per door. It steps out of the frame while a menu
    // is open, since every menu carries its own way out. View only: the NavMenuPresenter decides.
    public class NavMenu : Menu
    {
        [SerializeField] private List<NavTab> _tabs = new();
        [SerializeField] private RectTransform _bar;
        [SerializeField] private float _tuckSeconds = 0.2f;

        private readonly Dictionary<NavTab, Action> _handlers = new();
        private Tween _tuck;

        // A tab was tapped: its id.
        public event Action<string> TabTapped;

        public IReadOnlyList<NavTab> Tabs => _tabs;

        public void SetTucked(bool tucked)
        {
            _tuck?.Kill();
            var target = tucked ? -_bar.rect.height - 20 : 0;
            _tuck = _bar.DOAnchorPosY(target, _tuckSeconds).SetEase(tucked ? Ease.InCubic : Ease.OutCubic);
            CanvasGroup.blocksRaycasts = !tucked;
        }

        protected override void SubscribeToEventsInternal()
        {
            foreach (var tab in _tabs) tab.Tapped += OnTab(tab);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            foreach (var tab in _tabs) tab.Tapped -= OnTab(tab);
        }

        private Action OnTab(NavTab tab)
        {
            if (!_handlers.TryGetValue(tab, out var handler)) _handlers[tab] = handler = () => TabTapped?.Invoke(tab.Id);
            return handler;
        }

        protected override void DisposeInternal() => _tuck?.Kill();
    }
}
