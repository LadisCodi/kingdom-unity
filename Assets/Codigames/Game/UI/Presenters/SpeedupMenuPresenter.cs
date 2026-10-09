using System;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Bag;
using Codigames.Game.Data.City;
using Codigames.Game.UI.Bag;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.Bag;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The speed-up picker for one timer (the web's speedupScreen): what it is and how long it has left, Auto's plan,
    // the speed-ups that fit, and the Gems that finish it. It closes once the timer is done.
    public class SpeedupMenuPresenter : AbstractDataMenuPresenter<SpeedupMenu, SpeedJob>, IClosableMenuPresenter, ITickable
    {
        private readonly Kingdom.Army.Army _army;
        private readonly Kingdom.Goods.Workshops _workshops;
        private readonly Codigames.Game.Data.Goods.GoodCollection _goods;
        private readonly UIManager _ui;
        private readonly Speedups _speedups;
        private readonly Kingdom.Bag.Bag _bag;
        private readonly ItemCollection _items;
        private readonly ItemTiles _tiles;
        private readonly GemRush _rush;
        private readonly CityState _city;
        private readonly BuildingCollection _buildings;
        private readonly IConstructionSettings _settings;
        private readonly VillagerTraining _training;
        private readonly ITreasury _treasury;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;

        private double _shownSecond = -1;

        public SpeedupMenuPresenter(IMenuViewFactory views, UIManager ui, Speedups speedups, Kingdom.Bag.Bag bag, ItemCollection items,
            ItemTiles tiles, GemRush rush, CityState city, BuildingCollection buildings, IConstructionSettings settings,
            VillagerTraining training, ITreasury treasury, UiIcons icons, NumberFormat numbers, Localizer localizer, IClock clock,
            ISoundService sounds, Kingdom.Goods.Workshops workshops, Codigames.Game.Data.Goods.GoodCollection goods, Kingdom.Army.Army army) : base(views)
        {
            _army = army;
            _workshops = workshops;
            _goods = goods;
            _ui = ui;
            _speedups = speedups;
            _bag = bag;
            _items = items;
            _tiles = tiles;
            _rush = rush;
            _city = city;
            _buildings = buildings;
            _settings = settings;
            _training = training;
            _treasury = treasury;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<SpeedupMenu>();

        public void Tick()
        {
            if (!IsShown) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _shownSecond) return;
            Refresh();
        }

        protected override void BindInternal(SpeedupMenu view) => Refresh();

        protected override void SubscribeToViewEventsInternal(SpeedupMenu view)
        {
            view.CloseTapped += RequestClose;
            view.AutoTapped += OnAuto;
            view.UseTapped += OnUse;
            view.FinishTapped += OnFinish;
        }

        protected override void UnsubscribeFromViewEventsInternal(SpeedupMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.AutoTapped -= OnAuto;
            view.UseTapped -= OnUse;
            view.FinishTapped -= OnFinish;
        }

        private void OnUse(string id)
        {
            var used = _speedups.Use(Data, id, 1, _clock.NowMs) == SpeedupRefusal.None;
            _sounds.Play(used ? SoundIds.BUTTON_PRESS : SoundIds.ERROR);
            Refresh();
        }

        private void OnAuto()
        {
            foreach (var (id, count) in _speedups.AutoPlan(Data, _clock.NowMs)) _speedups.Use(Data, id, count, _clock.NowMs);
            _sounds.Play(SoundIds.BUTTON_PRESS);
            Refresh();
        }

        private void OnFinish()
        {
            var now = _clock.NowMs;
            var done = Data.Kind switch
            {
                SpeedupKind.Construction => _rush.FinishJob(Data.JobId, now),
                SpeedupKind.Workshop => _workshops.Rush(Data.JobId, now) == Kingdom.Goods.WorkshopRefusal.None,
                SpeedupKind.Training when Data.JobId != null => _army.Rush(Data.JobId, now) == Kingdom.Army.ArmyRefusal.None,
                _ => _rush.FinishLine(now),
            };
            _sounds.Play(done ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            Refresh();
        }

        private void Refresh()
        {
            var now = _clock.NowMs;
            _shownSecond = Math.Floor(now / 1000.0);
            var left = _speedups.RemainingSeconds(Data, now);
            if (left == null || left <= 0)
            {
                // Done: the picker closes, where the player can see the job finish.
                RequestClose();
                return;
            }

            var (title, icon, progress, gems) = Facts(now);
            View.Show(new SpeedupScreenData
            {
                Icon = icon,
                Title = title,
                Progress = progress,
                Left = _numbers.Duration(Math.Ceiling(left.Value)),
                Auto = AutoWords(now),
                Rows = _speedups.For(Data).OfType<ItemAsset>()
                    .Select(i => new SpeedupRowData { Tile = _tiles.Tile(i, _bag.Count(i.Id)), Name = _localizer.Tr(i.Name) })
                    .ToList(),
                None = _localizer.Tr("No speed-ups for this in the Bag"),
                Use = _localizer.Tr("Use"),
                Finish = new[] { new PriceTerm(GemRush.GEMS, _numbers.Exact(gems), _treasury.Get(GemRush.GEMS) < gems) },
                CanFinish = _treasury.Get(GemRush.GEMS) >= gems,
            });
        }

        // What the timer is: a building's work (its name, and the level an upgrade reaches), or the Townhall's line.
        private (string Title, UnityEngine.Sprite Icon, float Progress, double Gems) Facts(double now)
        {
            if (Data.Kind == SpeedupKind.Construction)
            {
                var job = _city.Jobs.First(j => j.Id == Data.JobId);
                var district = _city.Districts.First(d => d.Id == job.DistrictId);
                var name = _localizer.Tr(_buildings.Get<BuildingAsset>(district.DefinitionId).DisplayName);
                var title = job.TargetLevel > 1 ? _localizer.Tr("{name} · level {n}", ("name", name), ("n", _numbers.Exact(job.TargetLevel))) : name;
                return (title, _icons.Get(district.DefinitionId), (float)Math.Min(1, (now - job.StartedAt) / (job.Seconds * 1000)),
                    _rush.JobCost(job.Id, now));
            }

            if (Data.Kind == SpeedupKind.Workshop)
            {
                // The good and its workshop: "Planks · Carpenter".
                var shop = _city.Districts.First(d => d.Id == Data.JobId);
                var good = _workshops.GoodOf(shop);
                var title = _localizer.Tr(good.Name) + " · " + _localizer.Tr(_buildings.Get<BuildingAsset>(shop.DefinitionId).DisplayName);
                return (title, _goods.Get<Codigames.Game.Data.Goods.GoodAsset>(good.Id).Icon, (float)_workshops.Progress(shop.Id, 0, now),
                    _workshops.RushCost(shop.Id, now));
            }

            if (Data.JobId != null)
            {
                // A hall's line: "Barracks · 12 training".
                var line = _city.Districts.First(d => d.Id == Data.JobId);
                var count = _army.Line(line.Id).Sum(i => i.Count);
                var hallName = _localizer.Tr(_buildings.Get<BuildingAsset>(line.DefinitionId).DisplayName);
                return (_localizer.Tr("{name} · {n} training", ("name", hallName), ("n", _numbers.Exact(count))), _icons.Get(line.DefinitionId),
                    (float)_army.HeadProgress(line.Id, now), _army.RushCost(line.Id, now));
            }

            var townhall = _settings.Townhall.Id;
            var head = _training.Current;
            var progress = head?.StartedAt is double started ? (float)Math.Min(1, (now - started) / (head.Seconds * 1000)) : 0f;
            var hall = _localizer.Tr(_buildings.Get<BuildingAsset>(townhall).DisplayName);
            return (_localizer.Tr("{name} · {n} training", ("name", hall), ("n", _numbers.Exact(_city.Trainees.Count))),
                _icons.Get(townhall), progress, _rush.LineCost(now));
        }

        // Auto's face: what it will spend, largest first — "2× 1h, 1× 15m".
        private string AutoWords(double now)
        {
            var plan = _speedups.AutoPlan(Data, now);
            if (plan.Count == 0) return string.Empty;
            var words = string.Join(", ", plan
                .OrderByDescending(p => _items.Get(p.Id).Seconds)
                .Select(p => _numbers.Exact(p.Count) + "× " + _numbers.Duration(_items.Get(p.Id).Seconds)));
            return _localizer.Tr("Auto · {plan}", ("plan", words));
        }
    }
}
