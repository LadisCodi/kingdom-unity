using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Relics;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Relics;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Audio;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The relic picker's logic (the web's relicPickToggle, relicPickClear, relicPickConfirm): a tap on a relic seats it
    // in the Shrine's slot, or takes it out if it is the one there; a tap on the filled slot empties it. Select hosts
    // what the slot holds — or takes the Shrine's relic out when it was emptied; a relic already in another Shrine asks
    // first, since moving it ends its window there. A popup over the Shrine's card. Its data: the Shrine's district.
    public class RelicPickerMenuPresenter : AbstractDataMenuPresenter<RelicPickerMenu, string>, IPopupMenuPresenter
    {
        private readonly UIManager _ui;
        private readonly Shrines _shrines;
        private readonly RelicCards _cards;
        private readonly RelicWords _words;
        private readonly RelicActions _actions;
        private readonly Kingdom.Relics.Relics _relics;
        private readonly Kingdom.City.State.CityState _city;
        private readonly Localizer _localizer;
        private readonly NumberFormat _numbers;
        private readonly ISoundService _sounds;

        private string _slot;

        public RelicPickerMenuPresenter(IMenuViewFactory views, UIManager ui, Shrines shrines, RelicCards cards, RelicWords words,
            RelicActions actions, Kingdom.Relics.Relics relics, Kingdom.City.State.CityState city, Localizer localizer, NumberFormat numbers,
            ISoundService sounds) : base(views)
        {
            _ui = ui;
            _shrines = shrines;
            _cards = cards;
            _words = words;
            _actions = actions;
            _relics = relics;
            _city = city;
            _localizer = localizer;
            _numbers = numbers;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<RelicPickerMenu>();

        private Kingdom.City.State.DistrictState Shrine => _city.Districts.FirstOrDefault(d => d.Id == Data);

        protected override void BindInternal(RelicPickerMenu view)
        {
            _slot = Shrine is { } shrine ? _shrines.Hosted(shrine) : null;
            Refresh();
            view.ScrollToTop();
        }

        protected override void SubscribeToViewEventsInternal(RelicPickerMenu view)
        {
            view.CloseTapped += RequestClose;
            view.CardTapped += OnCard;
            view.SlotTapped += OnSlot;
            view.SelectTapped += OnSelect;
        }

        protected override void UnsubscribeFromViewEventsInternal(RelicPickerMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.CardTapped -= OnCard;
            view.SlotTapped -= OnSlot;
            view.SelectTapped -= OnSelect;
        }

        private void OnCard(string id)
        {
            _slot = _slot == id ? null : id;
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnSlot()
        {
            _slot = null;
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnSelect()
        {
            var shrine = Shrine;
            if (shrine == null) return;
            var held = _shrines.Hosted(shrine);
            if (_slot == held)
            {
                RequestClose();
                return;
            }

            if (_slot == null)
            {
                _actions.Unhost(held);
                RequestClose();
                return;
            }

            var elsewhere = _shrines.HostOf(_slot);
            if (elsewhere == null || elsewhere.Id == shrine.Id)
            {
                Host(_slot, shrine.Id);
                return;
            }

            // In another Shrine: ask first.
            var relic = _cards.Get(_slot);
            var id = _slot;
            var name = _localizer.Tr(relic.Name);
            _ = _ui.ShowMenu<ConfirmMenu, ConfirmData>(new ConfirmData
            {
                Title = _localizer.Tr("Move relic"),
                Art = relic.Icon,
                Text = _localizer.Tr("{name} is already in another Shrine. Move it to this one?", ("name", name)),
                Note = _shrines.IsAwake(id) ? _localizer.Tr("It is awake there: moving it ends its window.") : null,
                CancelLabel = _localizer.Tr("Cancel"),
                OkLabel = _localizer.Tr("Move"),
                Ok = () => Host(id, shrine.Id),
            });
        }

        private void Host(string relic, string shrineId)
        {
            _actions.Host(relic, shrineId);
            RequestClose();
        }

        private void Refresh()
        {
            if (View == null || Data == null) return;
            var list = _cards.Hostable();
            var cards = list.Select(id => _cards.PickCard(id, id == _slot, Effect(id))).ToList();
            var chosen = _slot == null ? null : _cards.PickCard(_slot, false, Effect(_slot));
            View.Show(_localizer.Tr("Choose a relic"), _localizer.Tr("Relics"), cards, _localizer.Tr("No city relic is restored yet"),
                _localizer.Tr("Shrine"), _numbers.Exact(_slot == null ? 0 : 1) + "/" + _numbers.Exact(1), chosen, _localizer.Tr("Select"));
        }

        private string Effect(string id) => _words.Short(_cards.Get(id), _relics.Level(id));
    }
}
