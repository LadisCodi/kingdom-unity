using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.UI.Buildings;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Crews;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using UnityEngine;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // One building's card (the web's districtCard): its name, number and level; what it is; Upgrade — which opens the
    // upgrade sheet — or, while it is being built, the gem Finish; the band of what it is worth now; the Townhall's
    // training panel and a producer's crew. Redrawn once a second and whenever the purse, a job or the line moves.
    public class DistrictCardMenuPresenter : AbstractDataMenuPresenter<DistrictCardMenu, string>, IClosableMenuPresenter, ITickable,
        IInspectedDistrict
    {
        private const string VILLAGER = "Villager";

        private readonly UIManager _ui;
        private readonly Construction _construction;
        private readonly CityState _city;
        private readonly BuildingCollection _buildings;
        private readonly IConstructionSettings _settings;
        private readonly ITreasury _treasury;
        private readonly IClock _clock;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly Stores _stores;
        private readonly VillagerTraining _training;
        private readonly Workforce _crews;
        private readonly BuildingStats _stats;
        private readonly BuildingStatProse _prose;
        private readonly GemRush _rush;
        private readonly UiIcons _icons;
        private readonly PortraitArt _portraits;
        private readonly ISoundService _sounds;
        private readonly CardFraming _framing;
        private readonly Speedups _speedups;
        private readonly Harmony _harmony;
        private readonly IHarmonySettings _harmonySettings;
        private readonly Adjacency _adjacency;
        private readonly City.WorkAreaView _workArea;
        private readonly Kingdom.Goods.Workshops _workshops;
        private readonly Kingdom.Goods.Stockpile _stockpile;
        private readonly Codigames.Game.Data.Goods.GoodCollection _goods;
        private readonly PriceTerms _prices;
        private readonly Kingdom.Army.Army _army;
        private readonly Codigames.Game.UI.Research.TechProse _techs;

        // A hall's chosen rank for this session, and the best rank open when it was chosen: a better one opening lets it lapse.
        private readonly Dictionary<string, (string Troop, string Best)> _picks = new();

        // The second the card was last drawn at: it is redrawn once a second.
        private double _shownSeconds = -1;

        // How many one press of Train orders: the knob turns through them, and it is not saved.
        private TrainAmount _amount = TrainAmount.One;

        public DistrictCardMenuPresenter(IMenuViewFactory views, UIManager ui, Construction construction, CityState city,
            BuildingCollection buildings, IConstructionSettings settings, ITreasury treasury, IClock clock, NumberFormat numbers,
            Localizer localizer, Stores stores, VillagerTraining training, Workforce crews, BuildingStats stats,
            BuildingStatProse prose, GemRush rush, UiIcons icons, PortraitArt portraits, ISoundService sounds,
            CardFraming framing, Speedups speedups, Harmony harmony, IHarmonySettings harmonySettings, Adjacency adjacency, City.WorkAreaView workArea,
            Kingdom.Goods.Workshops workshops, Kingdom.Goods.Stockpile stockpile, Codigames.Game.Data.Goods.GoodCollection goods,
            PriceTerms prices, Kingdom.Army.Army army, Codigames.Game.UI.Research.TechProse techs) : base(views)
        {
            _army = army;
            _techs = techs;
            _workshops = workshops;
            _stockpile = stockpile;
            _goods = goods;
            _prices = prices;
            _workArea = workArea;
            _harmony = harmony;
            _harmonySettings = harmonySettings;
            _adjacency = adjacency;
            _speedups = speedups;
            _framing = framing;
            _ui = ui;
            _construction = construction;
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _treasury = treasury;
            _clock = clock;
            _numbers = numbers;
            _localizer = localizer;
            _stores = stores;
            _training = training;
            _crews = crews;
            _stats = stats;
            _prose = prose;
            _rush = rush;
            _icons = icons;
            _portraits = portraits;
            _sounds = sounds;
        }

        private DistrictState District => _city.Districts.FirstOrDefault(d => d.Id == Data);

        public string Inspected => IsShown ? Data : null;

        public void RequestClose() => _ = _ui.HideMenu<DistrictCardMenu>();

        public void Tick()
        {
            if (!IsShown) return;

            // Once a second: the countdowns, and the store filling.
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second != _shownSeconds) Refresh();
        }

        protected override void BindInternal(DistrictCardMenu view)
        {
            Refresh();
            if (District is { } district)
            {
                var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
                _framing.Frame(district.Anchor, building.Width, building.Height, View.CardTop);
            }

            _treasury.Changed += OnTreasuryChanged;
            _construction.JobStarted += OnJob;
            _construction.JobCompleted += OnJobCompleted;
            _training.Arrived += Refresh;
        }

        protected override void UnbindInternal(DistrictCardMenu view)
        {
            _workArea.Hide();
            _treasury.Changed -= OnTreasuryChanged;
            _construction.JobStarted -= OnJob;
            _construction.JobCompleted -= OnJobCompleted;
            _training.Arrived -= Refresh;
        }

        protected override void SubscribeToViewEventsInternal(DistrictCardMenu view)
        {
            view.CloseTapped += RequestClose;
            view.MoveTapped += OnMove;
            view.UpgradeTapped += OnUpgrade;
            view.FinishWorkTapped += OnFinishWork;
            view.SpeedUpWorkTapped += OnSpeedUpWork;
            view.SpeedUpTrainingTapped += OnSpeedUpTraining;
            view.MakeTapped += OnMake;
            view.RankPicked += OnRankPicked;
            view.HealTapped += OnHeal;
            view.CancelGoodTapped += OnCancelGood;
            view.FinishGoodTapped += OnFinishGood;
            view.SpeedUpGoodTapped += OnSpeedUpGood;
            view.AmountTapped += OnAmount;
            view.TrainTapped += OnTrain;
            view.FinishTrainingTapped += OnFinishTraining;
            view.CrewMinusTapped += OnCrewMinus;
            view.CrewPlusTapped += OnCrewPlus;
        }

        protected override void UnsubscribeFromViewEventsInternal(DistrictCardMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.MoveTapped -= OnMove;
            view.UpgradeTapped -= OnUpgrade;
            view.FinishWorkTapped -= OnFinishWork;
            view.SpeedUpWorkTapped -= OnSpeedUpWork;
            view.SpeedUpTrainingTapped -= OnSpeedUpTraining;
            view.MakeTapped -= OnMake;
            view.RankPicked -= OnRankPicked;
            view.HealTapped -= OnHeal;
            view.CancelGoodTapped -= OnCancelGood;
            view.FinishGoodTapped -= OnFinishGood;
            view.SpeedUpGoodTapped -= OnSpeedUpGood;
            view.AmountTapped -= OnAmount;
            view.TrainTapped -= OnTrain;
            view.FinishTrainingTapped -= OnFinishTraining;
            view.CrewMinusTapped -= OnCrewMinus;
            view.CrewPlusTapped -= OnCrewPlus;
        }

        // Moving it: the card makes way for the ghost, and comes back when it is put down.
        private async void OnMove()
        {
            var district = District;
            if (district == null) return;
            await _ui.CloseAll();
            _ = _ui.ShowMenu<PlacementMenu, PlacementOrder>(PlacementOrder.MoveDistrict(district.Id, district.DefinitionId));
        }

        private void OnTreasuryChanged(string currency, double amount) => Refresh();
        private void OnJob(ConstructionJob job) => Refresh();
        private void OnJobCompleted(ConstructionJob job, DistrictState district) => Refresh();

        // Everything buying a level says lives behind the button: the sheet, in the card's place until it is closed.
        private async void OnUpgrade()
        {
            var district = Data;
            await _ui.HideMenu<DistrictCardMenu>();
            _ = _ui.ShowMenu<UpgradeSheetMenu, string>(district);
        }

        private void OnFinishWork()
        {
            var job = JobOf(Data);
            var done = job != null && _rush.FinishJob(job.Id, _clock.NowMs);
            _sounds.Play(done ? SoundIds.UPGRADE_BOUGHT : SoundIds.ERROR);
            Refresh();
        }

        // Speed up: the picker, with Finish inside it, last.
        private void OnSpeedUpWork()
        {
            if (JobOf(Data) is { } job) _ = _ui.ShowMenu<SpeedupMenu, SpeedJob>(SpeedJob.Construction(job.Id));
        }

        private void OnSpeedUpTraining()
            => _ = _ui.ShowMenu<SpeedupMenu, SpeedJob>(District is { } hall && _army.Line(hall.Id).Count > 0 ? SpeedJob.Hall(hall.Id) : SpeedJob.Training());

        private void OnMake()
        {
            var refusal = _workshops.Enqueue(Data, _clock.NowMs);
            _sounds.Play(refusal == Kingdom.Goods.WorkshopRefusal.None ? SoundIds.BUTTON_PRESS : SoundIds.ERROR);
            Refresh();
        }

        private void OnCancelGood(int index)
        {
            _sounds.Play(SoundIds.BUTTON_PRESS);
            _workshops.Cancel(Data, index, _clock.NowMs);
            Refresh();
        }

        private void OnFinishGood()
        {
            var refusal = _workshops.Rush(Data, _clock.NowMs);
            _sounds.Play(refusal == Kingdom.Goods.WorkshopRefusal.None ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            Refresh();
        }

        private void OnSpeedUpGood() => _ = _ui.ShowMenu<SpeedupMenu, SpeedJob>(SpeedJob.Workshop(Data));

        // The workshop's panel: its good and recipe, its stock, its crew, its queue, the next out, and Make.
        private WorkshopPanelData Workshop(DistrictState district, double now)
        {
            var good = _workshops.GoodOf(district);
            if (good == null || !district.Built) return null;

            var asset = _goods.Get<Codigames.Game.Data.Goods.GoodAsset>(good.Id);
            var name = _localizer.Tr(good.Name);
            var crew = _crews.Assigned(district.Id);
            var queue = _workshops.Queue(district.Id);
            var capacity = _workshops.Capacity(district);
            var worked = Math.Min(crew, queue.Count);
            var slots = new List<WorkshopSlotData>();
            for (var i = 0; i < Math.Max(capacity, queue.Count); i++)
            {
                slots.Add(i < queue.Count
                    ? new WorkshopSlotData { Icon = asset.Icon, Filled = true, Working = i < worked, Progress = (float)_workshops.Progress(district.Id, i, now) }
                    : new WorkshopSlotData());
            }

            var recipe = string.Join("  ", good.Input.Where(p => p.Value > 0).Select(p => Inline(p.Key, p.Value))
                .Concat(string.IsNullOrEmpty(good.InputGood) ? Array.Empty<string>() : new[] { Inline(good.InputGood, good.InputGoodAmount) })
                .Concat(good.InputMana > 0 ? new[] { Inline(Kingdom.Magic.ManaPool.MANA, good.InputMana) } : Array.Empty<string>()));

            var inputGoods = string.IsNullOrEmpty(good.InputGood) ? null : new Dictionary<string, double> { [good.InputGood] = good.InputGoodAmount };
            var price = _prices.Of(good.InputMana > 0
                ? good.Input.Concat(new[] { new KeyValuePair<string, double>(Kingdom.Magic.ManaPool.MANA, good.InputMana) }).ToDictionary(p => p.Key, p => p.Value)
                : good.Input, inputGoods);
            var refusal = _workshops.Refusal(district.Id, now);
            var next = _workshops.NextSeconds(district.Id, now);
            var gems = _workshops.RushCost(district.Id, now);

            return new WorkshopPanelData
            {
                GoodIcon = asset.Icon,
                GoodName = name,
                Recipe = recipe,
                Held = _numbers.Exact(_stockpile.Get(good.Id)),
                HeldLabel = _localizer.Tr("in store"),
                CrewLine = "<sprite name=\"workers\"> " + (crew == 0
                    ? _localizer.Tr("No villagers here — nothing is being made")
                    : _localizer.Tr("{n} working · {time} each", ("n", _numbers.Exact(crew)), ("time", _numbers.Duration(_workshops.NeedMs(district) / 1000 / crew)))),
                CrewWarning = crew == 0,
                Slots = slots,
                Next = next == null ? null : "<sprite name=\"hourglass\"> " + _localizer.Tr("next in {time}", ("time", _numbers.Countdown(Math.Ceiling(next.Value)))),
                SpeedUp = _speedups.For(SpeedJob.Workshop(district.Id)).Count > 0,
                SpeedUpLabel = "<sprite name=\"hourglass\"> " + _localizer.Tr("Speed up"),
                Finish = new[] { new PriceTerm(GemRush.GEMS, _numbers.Exact(gems), _treasury.Get(GemRush.GEMS) < gems) },
                CanFinish = _treasury.Get(GemRush.GEMS) >= gems,
                FinishLabel = _localizer.Tr("Finish"),
                MakeLabel = _localizer.Tr("Make {good}", ("good", name)),
                MakePrice = price,
                CanMake = refusal == Kingdom.Goods.WorkshopRefusal.None,
                MakeNote = refusal == Kingdom.Goods.WorkshopRefusal.QueueFull
                    ? _localizer.Tr("The queue is full")
                    : _localizer.Tr("{n}/{cap} queued", ("n", _numbers.Exact(queue.Count)), ("cap", _numbers.Exact(capacity))),
            };
        }

        private string Inline(string id, double amount) => "<sprite name=\"" + id + "\"> " + _numbers.Exact(amount);

        private void OnAmount()
        {
            _amount = (TrainAmount)(((int)_amount + 1) % 4);
            Refresh();
        }

        private void OnTrain()
        {
            if (District is { } hall && IsHall(_buildings.Get<BuildingAsset>(hall.DefinitionId)))
            {
                var troop = Picked(hall);
                var refusal = _army.Train(hall.Id, troop, Amount(troop), _clock.NowMs);
                _sounds.Play(refusal == Kingdom.Army.ArmyRefusal.None ? SoundIds.BUTTON_PRESS : SoundIds.ERROR);
                Refresh();
                return;
            }

            var villagers = _training.Train(_training.Plan(_amount).Count, _clock.NowMs);
            if (villagers != TrainRefusal.None) _sounds.Play(SoundIds.ERROR);
            Refresh();
        }

        private void OnFinishTraining()
        {
            var done = District is { } hall && _army.Line(hall.Id).Count > 0
                ? _army.Rush(hall.Id, _clock.NowMs) == Kingdom.Army.ArmyRefusal.None
                : _rush.FinishLine(_clock.NowMs);
            _sounds.Play(done ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            Refresh();
        }

        private void OnRankPicked(string troop)
        {
            if (District is not { } hall) return;
            if (_army.Gate(troop, hall) != Kingdom.Army.ArmyRefusal.None)
            {
                _sounds.Play(SoundIds.ERROR);
                return;
            }

            _sounds.Play(SoundIds.BUTTON_PRESS);
            _picks[hall.Id] = (troop, BestOpen(hall));
            View.CloseRanks();
            Refresh();
        }

        private void OnHeal(string troop)
        {
            var refusal = _army.Heal(troop, _clock.NowMs);
            _sounds.Play(refusal == Kingdom.Army.ArmyRefusal.None ? SoundIds.BUTTON_PRESS : SoundIds.ERROR);
            Refresh();
        }

        private bool IsHall(BuildingAsset building) => _army.IsHall(building);

        // The rank a hall shows: the one chosen, until a better one opens; else the best open; else its first.
        private string Picked(DistrictState hall)
        {
            var best = BestOpen(hall);
            return _picks.TryGetValue(hall.Id, out var pick) && pick.Best == best ? pick.Troop : best;
        }

        private string BestOpen(DistrictState hall)
        {
            var troops = _army.TroopsOf(hall);
            return troops.LastOrDefault(t => _army.Gate(t, hall) == Kingdom.Army.ArmyRefusal.None) ?? troops.FirstOrDefault();
        }

        private string TroopName(string troop)
        {
            if (troop == null) return string.Empty;
            var rank = Kingdom.Army.Troops.RankOf(troop);
            var name = _localizer.Tr(_army.UnitOf(troop).Name);
            return rank > 1 ? name + " " + Kingdom.Army.Troops.Roman(rank) : name;
        }

        private int Amount(string troop) => _amount switch
        {
            TrainAmount.One => 1,
            TrainAmount.Ten => 10,
            TrainAmount.Hundred => 100,
            _ => _army.Most(troop),
        };

        // Why a troop cannot be trained here now; null when it can, or when only the purse stands in the way.
        private string GateWords(DistrictState hall, string troop, int count)
        {
            var refusal = _army.Refusal(hall.Id, troop, count);
            var room = Math.Max(0, _army.Cap - _army.Committed);
            return refusal switch
            {
                Kingdom.Army.ArmyRefusal.HallLevel => _localizer.Tr("{name} level {n}", ("name", _localizer.Tr(_buildings.Get<BuildingAsset>(hall.DefinitionId).DisplayName)),
                    ("n", _numbers.Exact(_army.RankOf(troop).MinHallLevel))),
                Kingdom.Army.ArmyRefusal.TechRequired => _localizer.Tr("Needs {tech}", ("tech", _techs.Name(_army.TechFor(troop)))),
                Kingdom.Army.ArmyRefusal.OtherRank => _localizer.Tr("Finish the current batch"),
                Kingdom.Army.ArmyRefusal.AtCapacity => room == 0 ? _localizer.Tr("Max army reached") : _localizer.Tr("Room for {n}", ("n", _numbers.Exact(room))),
                _ => null,
            };
        }

        // A military hall's troops: the rank shown, its numbers, Train for the knob's amount or why not, its ranks, the line.
        private TrainingPanelData Hall(DistrictState hall, double now)
        {
            var troop = Picked(hall);
            if (troop == null) return null;
            var unit = _army.UnitOf(troop);
            var rank = _army.RankOf(troop);
            var bust = _portraits.Of(troop);
            var count = Amount(troop);
            var gate = GateWords(hall, troop, count);
            var price = _army.TrainCost(troop, count);
            var type = unit.Tags.Contains("Distance") ? _localizer.Tr("Ranged") : unit.Tags.Contains("Mounted") ? _localizer.Tr("Mounted") : _localizer.Tr("Melee");
            var picked = troop;

            return new TrainingPanelData
            {
                Bust = bust?.Sprite,
                BustShift = bust?.Shift ?? Vector2.zero,
                BustScale = bust?.Scale ?? 1,
                Owned = "x" + _numbers.Exact(_army.Count(troop)),
                Rank = Kingdom.Army.Troops.RankOf(troop),
                Tag = type,
                Description = _localizer.Tr(unit.Description),
                Stats = new[]
                {
                    new StatTileData(_icons.Get("atk"), _localizer.Tr("stat::Attack"), _numbers.Exact(rank.Atk)),
                    new StatTileData(_icons.Get("dmg"), _localizer.Tr("Damage"), _numbers.Exact(rank.Dmg)),
                    new StatTileData(_icons.Get("def"), _localizer.Tr("Defence"), _numbers.Exact(rank.Def)),
                    new StatTileData(_icons.Get("hp"), _localizer.Tr("Health"), _numbers.Exact(rank.Hp)),
                },
                Ranks = _army.TroopsOf(hall).Select(t =>
                {
                    var r = _army.RankOf(t);
                    var b = _portraits.Of(t);
                    var shut = _army.Gate(t, hall) switch
                    {
                        Kingdom.Army.ArmyRefusal.HallLevel => _localizer.Tr("{troop} needs the hall at level {n}", ("troop", TroopName(t)), ("n", _numbers.Exact(r.MinHallLevel))),
                        Kingdom.Army.ArmyRefusal.TechRequired => _localizer.Tr("{troop} is not researched yet", ("troop", TroopName(t))),
                        _ => null,
                    };
                    return new RankRowData
                    {
                        Troop = t, Bust = b?.Sprite, BustShift = b?.Shift ?? Vector2.zero, BustScale = b?.Scale ?? 1, Rank = Kingdom.Army.Troops.RankOf(t),
                        Name = TroopName(t), Locked = shut == null ? null : "<sprite name=\"padlock\"> " + shut,
                        Numbers = $"<sprite name=\"atk\">{_numbers.Exact(r.Atk)}  <sprite name=\"dmg\">{_numbers.Exact(r.Dmg)}  <sprite name=\"def\">{_numbers.Exact(r.Def)}  <sprite name=\"hp\">{_numbers.Exact(r.Hp)}",
                        Picked = t == picked,
                    };
                }).ToList(),
                Amount = _amount == TrainAmount.All ? _localizer.Tr("All") : "x" + _numbers.Exact(count),
                Price = _prices.Of(price),
                Gate = gate,
                CanTrain = gate == null && _treasury.CanAfford(price),
                Batch = HallBatch(hall, now),
                Empty = _localizer.Tr("Nothing in training"),
            };
        }

        private WorkData HallBatch(DistrictState hall, double now)
        {
            var line = _army.Line(hall.Id);
            if (line.Count == 0) return null;
            var head = line[0];
            var left = head.StartedAt.HasValue ? Math.Max(0, head.CompletesAt - now) / 1000 : head.Seconds;
            var gems = _army.RushCost(hall.Id, now);
            var count = line.Sum(i => i.Count);
            return new WorkData(head.Kind == Kingdom.Army.State.HallItemKind.Heal ? _localizer.Tr("Mending") : _localizer.Tr("Training"),
                (float)_army.HeadProgress(hall.Id, now), _numbers.Duration(Math.Ceiling(left)), Gems(gems), _treasury.Get(GemRush.GEMS) >= gems,
                count > 1 ? "x" + _numbers.Exact(count) : string.Empty,
                _localizer.Tr("Total time: {time}", ("time", _numbers.Duration(Math.Ceiling(_army.RemainingSeconds(hall.Id, now) ?? 0)))))
            {
                SpeedUp = _speedups.For(SpeedJob.Hall(hall.Id)).Count > 0,
            };
        }

        // The Infirmary's beds: who lies in them, and the price of mending each troop.
        private WardData Ward()
        {
            return new WardData
            {
                Beds = _localizer.Tr("{used} of {cap} beds taken", ("used", _numbers.Exact(_army.WoundedCount)), ("cap", _numbers.Exact(_army.Beds))),
                Empty = _localizer.Tr("Nobody in the beds. Soldiers hurt in a fight wait here instead of dying."),
                Rows = _army.Ward.Where(w => w.Value > 0).Select(w =>
                {
                    var bust = _portraits.Of(w.Key);
                    var refusal = _army.HealRefusal(w.Key);
                    return new WardRowData
                    {
                        Troop = w.Key, Bust = bust?.Sprite, BustShift = bust?.Shift ?? Vector2.zero, BustScale = bust?.Scale ?? 1,
                        Rank = Kingdom.Army.Troops.RankOf(w.Key),
                        Name = _numbers.Exact(w.Value) + " " + TroopName(w.Key),
                        Line = _localizer.Tr("Off the roster until they are back on their feet. Cheaper to mend than to replace."),
                        Price = _prices.Of(_army.HealCost(w.Key, w.Value)),
                        Gate = refusal == Kingdom.Army.ArmyRefusal.AtCapacity ? _localizer.Tr("No room in the ranks — upgrade this hall") : null,
                        CanHeal = refusal == Kingdom.Army.ArmyRefusal.None,
                        HealLabel = _localizer.Tr("Heal"),
                    };
                }).ToList(),
            };
        }

        private void OnCrewMinus()
        {
            _crews.Unassign(Data, _clock.NowMs);
            Refresh();
        }

        private void OnCrewPlus()
        {
            _crews.Assign(Data, _clock.NowMs);
            Refresh();
        }

        private ConstructionJob JobOf(string districtId) => _city.Jobs.FirstOrDefault(j => j.DistrictId == districtId);

        private void Refresh()
        {
            var district = District;
            if (district == null) return;
            var now = _clock.NowMs;
            _shownSeconds = Math.Floor(now / 1000.0);

            var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
            var job = JobOf(district.Id);
            var offer = job == null && district.Built ? _construction.UpgradeOffer(district.Id) : null;

            var card = new DistrictCardData
            {
                Id = district.Id,
                Title = Title(building, district),
                Art = building.ArtFor(district.Level),
                What = _localizer.Tr(building.Description),
                Movable = building.Buildable,
                Upgradable = offer != null && offer.Refusal != ConstructionRefusal.MaxLevel,
                UpgradeReady = offer != null && offer.Refusal == ConstructionRefusal.None,
                Work = job == null ? null : Work(job, now),
                Stats = Stats(building, district, now),
                TrainingHead = IsHall(building) ? TroopName(Picked(district)) : _localizer.Tr("Villager"),
                Training = !district.Built ? null
                    : IsHall(building) ? Hall(district, now)
                    : district.DefinitionId == _settings.Townhall.Id ? Training(now) : null,
                WardHead = _localizer.Tr("Ward"),
                Ward = district.Built && Kingdom.Army.Army.IsInfirmary(building) ? Ward() : null,
                CrewHead = _localizer.Tr("Workers"),
                SpeedUp = "<sprite name=\"hourglass\"> " + _localizer.Tr("Speed up"),
                Crew = _crews.HasCrew(district) && district.Built ? Crew(district) : null,
            };
            card.WorkshopHead = _localizer.Tr("Workshop");
            card.Workshop = Workshop(district, now);
            HarmonyOf(card, building, district);
            // Its work area on the map, and who its crew works, rimmed.
            if (district.Built && _crews.HasCrew(district) && _crews.Radius(district) > 0) _workArea.Show(_crews.Reach(district), _crews.Workable(district));
            else _workArea.Hide();
            NeighboursOf(card, building, district);
            View.Show(card);
        }

        // A decoration is one number, and this is it; the Townhall is where the taxes a surplus moves are collected —
        // silent on a city that has neither supplied nor been asked for any.
        private void HarmonyOf(DistrictCardData card, BuildingAsset building, DistrictState district)
        {
            card.HarmonyHead = _localizer.Tr("Harmony");
            var icon = "<sprite name=\"harmony\"> ";
            if (Harmony.IsDecoration(building))
            {
                card.HarmonyLine = icon + _localizer.Tr("Supplies {n} Harmony", ("n", _numbers.Exact(building.Production.HarmonySupply)));
                card.HarmonyNote = _localizer.Tr("and a house beside it collects more rent");
                return;
            }

            if (district.DefinitionId != _settings.Townhall.Id) return;
            var supply = _harmony.Supply;
            var demand = _harmony.Demand();
            if (supply <= 0 && demand <= 0) return;

            var tier = _harmony.Tier;
            var next = _harmonySettings.SurplusTiers.Where(t => tier == null || t.At > tier.Value.At).Select(t => (HarmonyTier?)t).FirstOrDefault();
            card.HarmonyLine = icon + _localizer.Tr("Harmony {supply} supplied, {demand} demanded", ("supply", _numbers.Exact(supply)), ("demand", _numbers.Exact(demand)));
            card.HarmonyNote = tier is { } paying
                ? _localizer.Tr("+{pct}% taxes", ("pct", _numbers.Exact(Math.Round(paying.Bonus * 100))))
                : next is { } coming && demand > 0
                    ? _localizer.Tr("{at}% of demand pays +{pct}% taxes", ("at", _numbers.Exact(Math.Round(coming.At * 100))), ("pct", _numbers.Exact(Math.Round(coming.Bonus * 100))))
                    : _localizer.Tr("nothing demands it yet");
        }

        // What its neighbours do: a house's rent as a verdict, and every time they move, under a heading.
        private void NeighboursOf(DistrictCardData card, BuildingAsset building, DistrictState district)
        {
            if (!district.Built) return;
            var badges = new List<(string, bool)>();
            if (building.Production.PopulationCapacityPerLevel.Count > 0)
            {
                var gold = _adjacency.GoldOf(district) * 60;
                if (gold != 0)
                {
                    var n = Signed(gold) + " <sprite name=\"Gold\">";
                    badges.Add(gold < 0
                        ? (_localizer.Tr("Crowded {n}/h — houses too close together", ("n", n)), false)
                        : (_localizer.Tr("Cosy neighbourhood {n}/h", ("n", n)), true));
                }
            }

            var times = _adjacency.InEffect(district).Where(e => e.Stat != AdjacencyStat.GoldPerMinute).ToList();
            foreach (var (stat, total) in times)
            {
                var what = stat == AdjacencyStat.WorkTime ? _localizer.Tr("Good neighbours — work time") : _localizer.Tr("A military quarter — training time");
                badges.Add((_localizer.Tr("{what} {label}", ("what", what), ("label", Signed(Math.Round(total * 100)) + "%")), total < 0));
            }

            card.NeighboursHead = times.Count > 0 ? _localizer.Tr("Neighbours") : null;
            card.Neighbours = badges;
        }

        // A signed figure, its minus the typographic one.
        private string Signed(double value) => (value < 0 ? "\u2212" : "+") + _numbers.Exact(Math.Abs(value));

        // The name, its number when there can be more than one, and the level a size down: *Housing #3 Lv 2*.
        private string Title(BuildingAsset building, DistrictState district)
        {
            var cap = CityQueries.MaxCount(building, CityQueries.TownhallLevel(_city, _settings));
            var numbered = building.Buildable && (!cap.HasValue || cap.Value > 1);
            var name = _localizer.Capitalized(_localizer.Tr(building.DisplayName));
            var ordinal = numbered ? " #" + _numbers.Number(district.Ordinal) : string.Empty;
            return name + ordinal + " <size=72%>" + _localizer.Tr("Lv {n}", ("n", _numbers.Number(district.Level))) + "</size>";
        }

        // The construction: what is being done, the bar under the portrait, and the Gems that finish it.
        private WorkData Work(ConstructionJob job, double now)
        {
            var left = Math.Max(0, job.CompletesAt - now) / 1000;
            var progress = (float)Math.Min(1, (now - job.StartedAt) / (job.Seconds * 1000));
            var gems = _rush.JobCost(job.Id, now);
            return new WorkData(job.TargetLevel == 1 ? _localizer.Tr("Building") : _localizer.Tr("Upgrading"), progress,
                _numbers.Duration(Math.Ceiling(left)), Gems(gems), _treasury.Get(GemRush.GEMS) >= gems)
            {
                SpeedUp = _speedups.For(SpeedJob.Construction(job.Id)).Count > 0,
            };
        }

        // What it is worth now: a producer's output per coin, a house's rent, then its level's figures.
        private IReadOnlyList<StatTileData> Stats(BuildingAsset building, DistrictState district, double now)
        {
            var tiles = new List<StatTileData>();
            if (district.Built && _crews.HasCrew(district))
                foreach (var currency in _crews.Currencies(district))
                    tiles.Add(new StatTileData(_icons.Get(currency), _localizer.Tr(currency),
                        "+" + _numbers.Short(_crews.GatherPerSecond(district, currency) * 3600) + "/h"));

            if (district.Built && building.Production.PopulationCapacityPerLevel.Count > 0)
                tiles.Add(new StatTileData(_icons.Get("Gold"), _localizer.Tr("Gold"),
                    "+" + _numbers.Short(_stores.GoldPerMinute(district) * 60) + "/h"));

            foreach (var stat in _stats.At(district, district.Level).Where(s => s.OnCard))
            {
                var value = _prose.Value(stat);
                var bad = false;
                if (stat.Kind == StatKind.Storage && district.Built)
                {
                    value = _numbers.Short(_stores.Held(district, now)) + "/" + _numbers.Short(stat.Value);
                    bad = _stores.IsFull(district, now);
                }
                else if (stat.Kind == StatKind.Beds && district.Built)
                {
                    value = _numbers.Short(_stores.Residents(district)) + "/" + _numbers.Short(stat.Value);
                }

                tiles.Add(new StatTileData(_prose.Icon(stat), _prose.Short(stat.Kind), value, bad));
            }

            // A hall's soldiers' time, at the rank it shows.
            if (district.Built && IsHall(building))
                tiles.Add(new StatTileData(_icons.Get("hourglass"), _prose.Short(StatKind.TrainTime),
                    _numbers.Duration(_army.TrainSeconds(Picked(district), district))));

            return tiles;
        }

        // The Townhall's villagers: who, how many the town has, Train priced for the knob's amount or gated, the batch.
        private TrainingPanelData Training(double now)
        {
            var bust = _portraits.Of(VILLAGER);
            var plan = _training.Plan(_amount);
            var room = _training.Room;
            var gate = room == 0 ? _localizer.Tr("No house to live in")
                : room < plan.Count ? _localizer.Tr("Room for {n}", ("n", _numbers.Exact(room)))
                : null;
            var food = _treasury.Get(VillagerTraining.FOOD);

            return new TrainingPanelData
            {
                Bust = bust?.Sprite,
                BustShift = bust?.Shift ?? Vector2.zero,
                BustScale = bust?.Scale ?? 1,
                Owned = "x" + _numbers.Exact(_city.Population),
                Tag = _localizer.Tr("Worker"),
                Description = _localizer.Tr("A hardworking settler who tends the fields and pays rent."),
                Amount = _amount == TrainAmount.All ? _localizer.Tr("All") : "x" + _numbers.Exact(plan.Count),
                Price = new[] { new PriceTerm(VillagerTraining.FOOD, _numbers.Exact(plan.Cost), food < plan.Cost) },
                Gate = gate,
                CanTrain = gate == null && food >= plan.Cost,
                Batch = Batch(now),
                Empty = _localizer.Tr("Nothing in training"),
            };
        }

        private WorkData Batch(double now)
        {
            var head = _training.Current;
            if (head == null) return null;

            var count = _city.Trainees.Count;
            var left = head.ArrivesAt is double at ? Math.Max(0, at - now) / 1000 : head.Seconds;
            var progress = head.StartedAt is double started ? (float)Math.Min(1, (now - started) / (head.Seconds * 1000)) : 0;
            var gems = _rush.LineCost(now);
            return new WorkData(_localizer.Tr("Training"), progress, _numbers.Duration(Math.Ceiling(left)), Gems(gems),
                _treasury.Get(GemRush.GEMS) >= gems, count > 1 ? "x" + _numbers.Exact(count) : string.Empty,
                _localizer.Tr("Total time: {time}", ("time", _numbers.Duration(Math.Ceiling(_training.RemainingSeconds(now) ?? 0)))))
            {
                SpeedUp = _speedups.For(SpeedJob.Training()).Count > 0,
            };
        }

        // The crew stepper: who works here against the most it holds.
        private CrewPanelData Crew(DistrictState district)
        {
            var bust = _portraits.Of(VILLAGER);
            var assigned = _crews.Assigned(district.Id);
            return new CrewPanelData(bust?.Sprite, bust?.Shift ?? Vector2.zero, bust?.Scale ?? 1, _numbers.Exact(assigned),
                " / " + _numbers.Exact(_crews.Limit(district)), assigned > 0, _crews.CanAssign(district));
        }

        private PriceTerm[] Gems(double gems)
            => new[] { new PriceTerm(GemRush.GEMS, _numbers.Exact(gems), _treasury.Get(GemRush.GEMS) < gems) };
    }
}
