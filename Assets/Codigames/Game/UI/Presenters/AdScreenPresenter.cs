using System;
using Codigames.Game.Ads;
using Codigames.Game.UI.Menus;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The rewarded video's clock: the seconds left derived from when it started, never counted down, so a paused app
    // resolves to the right number; Claim pays what it was watched for and closes it.
    public class AdScreenPresenter : AbstractDataMenuPresenter<AdScreen, AdWatch>, ITickable
    {
        private readonly UIManager _ui;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;

        public AdScreenPresenter(IMenuViewFactory views, UIManager ui, IClock clock, NumberFormat numbers) : base(views)
        {
            _ui = ui;
            _clock = clock;
            _numbers = numbers;
        }

        public void Tick()
        {
            if (View == null || Data == null || !IsShown) return;
            Refresh();
        }

        protected override void BindInternal(AdScreen view) => Refresh();

        protected override void SubscribeToViewEventsInternal(AdScreen view) => view.ClaimTapped += OnClaim;

        protected override void UnsubscribeFromViewEventsInternal(AdScreen view) => view.ClaimTapped -= OnClaim;

        private int Left => Math.Max(0, (int)Math.Ceiling((Data.Seconds * 1000 - (_clock.NowMs - Data.StartedAt)) / 1000));

        private void Refresh() => View.ShowLeft(_numbers.Count(Left), Left == 0);

        private async void OnClaim()
        {
            if (Left > 0) return;
            var reward = Data.Reward;
            await _ui.HideMenu<AdScreen>();
            reward?.Invoke();
        }
    }
}
