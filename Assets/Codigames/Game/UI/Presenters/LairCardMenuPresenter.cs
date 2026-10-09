using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.Data.Sites;
using Codigames.Game.Lairs;
using Codigames.Game.UI.Lairs;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // A lair's card: nothing else — the enemy's squads and power are the attack screen's, and a raid count is never
    // shown. Rebuilt only when what it says moves (the hoard, the path, beaten); a tick rewrites the countdown alone.
    public class LairCardMenuPresenter : AbstractDataMenuPresenter<LairCardMenu, string>, IClosableMenuPresenter, ITickable
    {
        private const string HERO_XP = "HeroXp";
        private const string KNOWLEDGE = "Knowledge";
        private static readonly string[] RAIDABLE = { "Gold", "Food", "Wood", "Stone" };

        private readonly UIManager _ui;
        private readonly Kingdom.Lairs.Lairs _lairs;
        private readonly ProvinceSitesAsset _sites;
        private readonly LairWords _words;
        private readonly ICurrencyIcons _icons;
        private readonly ITreasury _treasury;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly CardFraming _framing;

        private string _signature;
        private string _clockText;

        public LairCardMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Lairs.Lairs lairs, ProvinceSitesAsset sites, LairWords words,
            ICurrencyIcons icons, ITreasury treasury, NumberFormat numbers, Localizer localizer, IClock clock, ISoundService sounds,
            CardFraming framing) : base(views)
        {
            _ui = ui;
            _lairs = lairs;
            _sites = sites;
            _words = words;
            _icons = icons;
            _treasury = treasury;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _framing = framing;
        }

        public void RequestClose() => _ = _ui.HideMenu<LairCardMenu>();

        protected override void BindInternal(LairCardMenu view)
        {
            _signature = null;
            Refresh();
            if (_lairs.Site(Data) is { } lair) _framing.Frame(lair.Anchor, lair.Size, lair.Size, View.CardTop);
            _lairs.Changed += OnLairChanged;
            _lairs.Raided += OnRaided;
        }

        protected override void UnbindInternal(LairCardMenu view)
        {
            _lairs.Changed -= OnLairChanged;
            _lairs.Raided -= OnRaided;
        }

        protected override void SubscribeToViewEventsInternal(LairCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.ActionTapped += OnAction;
        }

        protected override void UnsubscribeFromViewEventsInternal(LairCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.ActionTapped -= OnAction;
        }

        public void Tick()
        {
            if (!IsShown || _lairs.StateOf(Data) is not { } state) return;
            var left = state.NextRaidAt is { } at ? System.Math.Max(0, (at - _clock.NowMs) / 1000) : 0;
            var text = _numbers.Duration(System.Math.Ceiling(left));
            if (text == _clockText || state.Defeated) return;
            _clockText = text;
            View.SetClock(text);
        }

        private void OnLairChanged(string id)
        {
            if (id == Data) Refresh();
        }

        private void OnRaided(string id, double at, IReadOnlyDictionary<string, double> took)
        {
            if (id == Data) Refresh();
        }

        private void OnAction()
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            if (_lairs.AwaitsClaim(Data))
            {
                _lairs.Claim(Data, _treasury);
                RequestClose();
            }
        }

        private void Refresh()
        {
            var lair = _lairs.Site(Data);
            var state = _lairs.StateOf(Data);
            if (lair == null || state == null || state.Cleared)
            {
                RequestClose();
                return;
            }

            var now = _clock.NowMs;
            var signature = string.Join(",", state.Hoard.Select(h => h.Key + h.Value)) + state.Won + state.Defeated
                            + string.Join(",", RAIDABLE.Select(c => _lairs.HoardFull(lair, c, now)));
            if (signature == _signature) return;
            _signature = signature;

            var site = _sites.LairOf(lair.Id);
            var creature = _words.Creature(lair);
            var one = lair.Threat == "Any";
            var fights = _lairs.Fights(lair);
            var won = _lairs.Won(lair);
            var chips = new List<(UnityEngine.Sprite, string, string)>();
            // The hoard first: what the raids took, and clearing the lair is how it comes back.
            foreach (var currency in RAIDABLE)
            {
                if (!state.Hoard.TryGetValue(currency, out var amount) || amount <= 0) continue;
                chips.Add((_icons.IconOf(currency), _numbers.Exact(amount), _lairs.HoardFull(lair, currency, now) ? _localizer.Tr("full") : null));
            }

            var reward = _lairs.ClearReward(lair);
            if (reward.HeroXp > 0) chips.Add((_icons.IconOf(HERO_XP), _numbers.Exact(reward.HeroXp), null));
            if (reward.Knowledge > 0) chips.Add((_icons.IconOf(KNOWLEDGE), _numbers.Exact(reward.Knowledge), null));

            _clockText = null;
            View.Show(new LairCardData
            {
                Title = _localizer.Tr(lair.Name),
                Painting = site?.Painting,
                Flavour = _localizer.Tr(lair.Flavour),
                Beaten = state.Defeated,
                ClockLabel = state.Defeated
                    ? one ? _localizer.Tr("{creature} is beaten", ("creature", creature)) : _localizer.Tr("{creature} are beaten", ("creature", creature))
                    : _localizer.Tr("They will attack your city in"),
                ClockValue = state.Defeated ? _localizer.Tr("Claim what they left behind") : string.Empty,
                ProgressHead = _localizer.Tr("Progress"),
                Fights = fights,
                Won = won,
                PathLabel = state.Defeated
                    ? _localizer.Tr("All {n} fights won", ("n", _numbers.Exact(fights)))
                    : _localizer.Tr("Fight {n} of {total}", ("n", _numbers.Exact(won + 1)), ("total", _numbers.Exact(fights))),
                RewardHead = _localizer.Tr("Reward"),
                Chips = chips,
                Action = state.Defeated ? _localizer.Tr("Claim") : _localizer.Tr("Attack"),
            });
            Tick();
        }
    }
}
