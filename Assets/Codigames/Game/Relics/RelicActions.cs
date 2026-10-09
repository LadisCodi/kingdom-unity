using System;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.Relics;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.Relics;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;

namespace Codigames.Game.Relics
{
    // What a press on a relic does, wherever it is offered — its card in the Bag, its sheet, its Shrine's card (the
    // web's doActivateRelic, doRestoreRelic, doLevelRelic, doHostRelic, doUseFlaskFor): the rule, then its sound and,
    // refused, why. Activated is for what the map draws over the aura.
    public class RelicActions
    {
        private readonly Kingdom.Relics.Relics _relics;
        private readonly Shrines _shrines;
        private readonly RelicCollection _catalog;
        private readonly Kingdom.Bag.Bag _bag;
        private readonly ItemCollection _items;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;
        private readonly Localizer _localizer;

        public RelicActions(Kingdom.Relics.Relics relics, Shrines shrines, RelicCollection catalog, Kingdom.Bag.Bag bag, ItemCollection items,
            IClock clock, ISoundService sounds, IQuickInfoMessageService messages, Localizer localizer)
        {
            _relics = relics;
            _shrines = shrines;
            _catalog = catalog;
            _bag = bag;
            _items = items;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
            _localizer = localizer;
        }

        // A relic woke: its id.
        public event Action<string> Activated;

        private string Name(string id) => _localizer.Tr(_catalog.Get<RelicAsset>(id).Name);

        public bool Activate(string id)
        {
            var result = _shrines.Activate(id, _clock.NowMs);
            if (result != ActivateBlock.None)
            {
                _sounds.Play(SoundIds.ERROR);
                if (result == ActivateBlock.NotHosted) Say(_localizer.Tr("Host it in a Shrine first"));
                else if (result == ActivateBlock.Active) Say(_localizer.Tr("{relic} is already awake", ("relic", Name(id))));
                return false;
            }

            _sounds.Play(SoundIds.RELIC_WAKE);
            Activated?.Invoke(id);
            return true;
        }

        public bool Restore(string id)
        {
            if (_relics.Restore(id) != RelicRestoreResult.Restored)
            {
                _sounds.Play(SoundIds.ERROR);
                return false;
            }

            _sounds.Play(SoundIds.QUEST_COMPLETE);
            Say(_localizer.Tr("The {relic} is restored", ("relic", Name(id))));
            return true;
        }

        public bool LevelUp(string id)
        {
            if (_relics.LevelUp(id) != RelicLevelResult.Levelled)
            {
                _sounds.Play(SoundIds.ERROR);
                return false;
            }

            _sounds.Play(SoundIds.QUEST_COMPLETE);
            return true;
        }

        public void Host(string id, string shrineId)
        {
            if (_shrines.Host(id, shrineId) == HostResult.Hosted) _sounds.Play(SoundIds.BUILD_PLACED);
            else _sounds.Play(SoundIds.ERROR);
        }

        public void Unhost(string id)
        {
            if (_shrines.Unhost(id)) _sounds.Play(SoundIds.BUTTON_PRESS);
        }

        // The smallest Mana flask the Bag holds, and how many; null when it holds none.
        public (ItemAsset Item, int Count)? SmallestFlask()
        {
            var flask = _items.Items.OfType<ItemAsset>().Where(i => i.Kind == ItemKind.Flask && _bag.Count(i.Id) > 0)
                .OrderBy(i => i.Value).FirstOrDefault();
            return flask == null ? null : (flask, _bag.Count(flask.Id));
        }

        public void UseFlask()
        {
            if (SmallestFlask() is not { } flask) return;
            _sounds.Play(_bag.Use(flask.Item.Id, 1, _clock.NowMs) == UseItemResult.Used ? SoundIds.BUTTON_PRESS : SoundIds.ERROR);
        }

        private void Say(string text) => _messages.Show(new QuickInfoMessageData(text));
    }
}
