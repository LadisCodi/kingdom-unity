using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Battles;
using Codigames.Game.Data.Sites;
using Codigames.Game.Lairs;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Lairs;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Feedback;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Presenters
{
    // The attack sheet on a lair: its creatures in the doorway, the party picked from the army at home, and the attack.
    // The fight resolves on the tap; a won fight closes the sheet so the playback ends on the lair's card, a step on its
    // path or Claim in Attack's place.
    public class AttackSheetMenuPresenter : AbstractDataMenuPresenter<AttackSheetMenu, string>, IClosableMenuPresenter
    {
        private const string MANA = "Mana";

        private readonly UIManager _ui;
        private readonly Kingdom.Lairs.Lairs _lairs;
        private readonly LairAttack _attack;
        private readonly AttackParty _party;
        private readonly Army _army;
        private readonly LairWords _words;
        private readonly PortraitArt _portraits;
        private readonly ProvinceSitesAsset _sites;
        private readonly ITreasury _treasury;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private readonly IQuickInfoMessageService _messages;
        private readonly MusicDirector _music;
        private readonly PlaybackPreferences _preferences;

        public AttackSheetMenuPresenter(IMenuViewFactory views, UIManager ui, Kingdom.Lairs.Lairs lairs, LairAttack attack, AttackParty party,
            Army army, LairWords words, PortraitArt portraits, ProvinceSitesAsset sites, ITreasury treasury, NumberFormat numbers,
            Localizer localizer, IClock clock, ISoundService sounds, IQuickInfoMessageService messages, MusicDirector music, PlaybackPreferences preferences) : base(views)
        {
            _preferences = preferences;
            _ui = ui;
            _lairs = lairs;
            _attack = attack;
            _party = party;
            _army = army;
            _words = words;
            _portraits = portraits;
            _sites = sites;
            _treasury = treasury;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
            _messages = messages;
            _music = music;
        }

        public void RequestClose() => _ = _ui.HideMenu<AttackSheetMenu>();

        protected override void BindInternal(AttackSheetMenu view)
        {
            // A lair resolves on entry, so nobody is busy: the roster is the party, best answer first.
            if (_lairs.Site(Data) is { } lair) _party.QuickDeploy(lair.Threat);
            _music.Set(MusicMoment.Muster, true);
            _army.Changed += Refresh;
            _treasury.Changed += OnTreasuryChanged;
            Refresh();
        }

        protected override void UnbindInternal(AttackSheetMenu view)
        {
            _music.Set(MusicMoment.Muster, false);
            _army.Changed -= Refresh;
            _treasury.Changed -= OnTreasuryChanged;
        }

        protected override void SubscribeToViewEventsInternal(AttackSheetMenu view)
        {
            view.CloseTapped += RequestClose;
            view.SlotTapped += OnSlot;
            view.TroopTapped += OnTroop;
            view.QuickDeployTapped += OnQuickDeploy;
            view.AttackTapped += OnAttack;
        }

        protected override void UnsubscribeFromViewEventsInternal(AttackSheetMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.SlotTapped -= OnSlot;
            view.TroopTapped -= OnTroop;
            view.QuickDeployTapped -= OnQuickDeploy;
            view.AttackTapped -= OnAttack;
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();

        private void OnSlot(int index)
        {
            _party.Clear(index);
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnTroop(int index)
        {
            var troop = _party.Roster[index];
            if (!_party.Assign(troop))
            {
                _sounds.Play(SoundIds.ERROR);
                _messages.Show(new QuickInfoMessageData(_party.AvailableFor(troop) <= 0
                    ? _localizer.Tr("No {troop}s left to send", ("troop", Name(troop)))
                    : _localizer.Tr("Every troop slot is full")));
                return;
            }

            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnQuickDeploy()
        {
            if (_lairs.Site(Data) is { } lair) _party.QuickDeploy(lair.Threat);
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnAttack()
        {
            var lair = _lairs.Site(Data);
            if (lair == null) return;
            // Which fight of the path this is, for the playback's line — read before the fight moves the path on.
            var fight = _lairs.FightIndex(lair) + 1;
            var report = _attack.Attack(Data, _party.Slots.ToList(), _clock.NowMs);
            if (report.Result == LairResult.Blocked)
            {
                _sounds.Play(SoundIds.ERROR);
                _messages.Show(new QuickInfoMessageData(BlockText(report.Block)));
                _party.Reconcile();
                Refresh();
                return;
            }

            _party.Reconcile();
            var playback = new BattlePlayback(report.Log, _localizer.Tr(lair.Name),
                _localizer.Tr("{creature} · Fight {n} of {total}", ("creature", _words.Creature(lair)), ("n", _numbers.Exact(fight)),
                    ("total", _numbers.Exact(_lairs.Fights(lair)))),
                _attack.TickMs, _clock.NowMs, _preferences.Speed)
            {
                Creatures = true,
                // A fight short of the last pays its share of Hero XP on the field; the last pays at the claim.
                Prizes = report.HeroXp > 0
                    ? new[] { Kingdom.Heroes.Prize.Currency(Kingdom.Heroes.Heroes.HERO_XP, (int)report.HeroXp) }
                    : System.Array.Empty<Kingdom.Heroes.Prize>(),
            };
            // Won: the sheet is done, and the player lands back on the lair's card when the playback closes.
            if (report.Result is LairResult.Won or LairResult.Cleared) RequestClose();
            else Refresh();
            _ = _ui.ShowMenu<BattleScreen, BattlePlayback>(playback);
        }

        private void Refresh()
        {
            if (View == null || _lairs.Site(Data) is not { } lair) return;
            var enemy = _attack.Formation(lair);
            var power = _attack.Power(lair);
            var attack = _attack.PartyPower(_party.Slots);
            var block = _attack.Block(Data, _party.Slots);
            var fallen = _party.Fallen(lair);
            var price = _attack.Supplies.Select(s => new PriceTerm(s.Key, _numbers.Exact(s.Value), _treasury.Get(s.Key) < s.Value)).ToList();
            View.Show(new AttackSheetData
            {
                Title = _localizer.Tr(lair.Name),
                EnemyHead = _localizer.Tr("Enemy"),
                EnemyPower = Power(power),
                Enemy = enemy.Select(s => Creature(s.Troop, s.Count)).ToList(),
                ArmyHead = _localizer.Tr("Your army"),
                ArmyPower = Power(attack),
                Short = attack < power,
                Slots = _party.SlotCount,
                Party = _party.Slots.Select(s => Squad(s.Troop, "×" + _numbers.Exact(s.Count))).ToList(),
                RosterHead = _localizer.Tr("Troops"),
                Roster = _party.Roster.Select(t =>
                {
                    var tile = Squad(t, _numbers.Exact(_party.LeftAtHome(t)));
                    tile.Name = Name(t);
                    tile.Out = _party.LeftAtHome(t) <= 0;
                    tile.Full = !tile.Out && _party.IsFull;
                    return tile;
                }).ToList(),
                Price = price,
                QuickDeploy = _localizer.Tr("Quick deploy"),
                Action = _localizer.Tr("Attack"),
                Blocked = block is not (LairBlock.None or LairBlock.NotEnoughSupplies),
                Note = block is not (LairBlock.None or LairBlock.NotEnoughSupplies) ? BlockText(block)
                    : fallen == 0 ? _localizer.Tr("No soldiers lost")
                    : _localizer.Trn(fallen, "Expected losses: ~{n} soldier", "Expected losses: ~{n} soldiers", ("n", _numbers.Exact(fallen))),
            });
        }

        private string Power(int power) => "<sprite name=\"power\"> " + _numbers.Exact(power);

        private string Name(string troop)
        {
            var unit = _army.UnitOf(troop);
            var rank = Troops.RankOf(troop);
            var name = _localizer.Tr(unit.Name);
            return rank <= 1 ? name : name + " " + Troops.Roman(rank);
        }

        private AttackSquadData Squad(string troop, string count)
        {
            var bust = _portraits.Of(troop);
            return new AttackSquadData { Bust = bust?.Sprite, Shift = bust?.Shift ?? default, Scale = bust?.Scale ?? 1, Count = count, Rank = Troops.RankOf(troop) };
        }

        // An enemy squad's face: the creature its unit is, whatever its rank — the rank rides on the coin.
        private AttackSquadData Creature(string troop, int count) => new()
        {
            Bust = _sites.CreatureOf(Troops.UnitOf(troop)), Count = "×" + _numbers.Exact(count), Rank = Troops.RankOf(troop),
        };

        private string BlockText(LairBlock block) => block switch
        {
            LairBlock.LairNotFound => _localizer.Tr("Clear a path to the lair first"),
            LairBlock.AlreadyCleared => _localizer.Tr("That lair is already cleared"),
            LairBlock.AlreadyDefeated => _localizer.Tr("They are beaten — claim what they left behind"),
            LairBlock.EmptyParty => _localizer.Tr("Pick who goes in"),
            LairBlock.NoSoldiers => _localizer.Tr("A lair wants soldiers — a hero cannot go in alone"),
            LairBlock.TooManySlots => _localizer.Tr("Too many kinds of unit — buy another party slot"),
            LairBlock.NotEnoughUnits => _localizer.Tr("You do not have that many at home"),
            LairBlock.NotEnoughSupplies => _localizer.Tr("Not enough Mana to attack"),
            _ => string.Empty,
        };
    }
}
