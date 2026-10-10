using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.City;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Game.UI.Store;
using Codigames.Kingdom.City;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Economy;
using Codigames.Kingdom.Store;
using Codigames.Modules.Audio;
using Codigames.Modules.Clock;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // The builder sheet's logic (the web's builderSheet + Game.offerBuilder): a refusal for want of a builder opens it
    // and opens the offer for one; a free builder's row starts the job it was raised for, a busy one's Finish frees it
    // for Gems, Hire takes on the next. A popup over whatever raised it.
    public class BuilderMenuPresenter : AbstractDataMenuPresenter<BuilderMenu, BuilderOrder>, IPopupMenuPresenter, ITickable
    {
        private const string GEMS = "Gems";
        private const string FREE = "free";
        private const string HIRE = "hire";
        private const string JOB = "job:";

        private readonly UIManager _ui;
        private readonly CityState _city;
        private readonly Construction _construction;
        private readonly Builders _builders;
        private readonly GemRush _rush;
        private readonly BuildingCollection _buildings;
        private readonly ITreasury _treasury;
        private readonly Offers _offers;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly IClock _clock;
        private readonly ISoundService _sounds;
        private double _second = -1;

        public BuilderMenuPresenter(IMenuViewFactory views, UIManager ui, CityState city, Construction construction, Builders builders, GemRush rush,
            BuildingCollection buildings, ITreasury treasury, Offers offers, UiIcons icons, NumberFormat numbers, Localizer localizer, IClock clock,
            ISoundService sounds) : base(views)
        {
            _ui = ui;
            _city = city;
            _construction = construction;
            _builders = builders;
            _rush = rush;
            _buildings = buildings;
            _treasury = treasury;
            _offers = offers;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _clock = clock;
            _sounds = sounds;
        }

        public void RequestClose() => _ = _ui.HideMenu<BuilderMenu>();

        public void Tick()
        {
            if (View == null || !IsShown) return;
            var second = Math.Floor(_clock.NowMs / 1000.0);
            if (second == _second) return;
            _second = second;
            Refresh();
        }

        protected override void BindInternal(BuilderMenu view)
        {
            // The refusal is the offer.
            _offers.Trigger("buildersBusy", _clock.NowMs);
            Refresh();
        }

        protected override void SubscribeToViewEventsInternal(BuilderMenu view)
        {
            view.CloseTapped += RequestClose;
            view.RowTapped += OnRow;
        }

        protected override void UnsubscribeFromViewEventsInternal(BuilderMenu view)
        {
            view.CloseTapped -= RequestClose;
            view.RowTapped -= OnRow;
        }

        private void Refresh()
        {
            var now = _clock.NowMs;
            var jobs = _city.Jobs.ToList();
            var free = Math.Max(0, _builders.Count - jobs.Count);
            var ask = Data?.Start != null ? Data : null;
            var rows = new List<(string, StoreWideData)>();
            for (var i = 0; i < free; i++)
            {
                var offered = i == 0 && ask != null;
                rows.Add((FREE + i, new StoreWideData
                {
                    Name = _localizer.Tr("builder::Free"),
                    Art = _icons.Get("builders"),
                    Line = offered ? ask.What : _localizer.Tr("Ready for the next job"),
                    Label = offered ? ask.Verb : null,
                    Price = offered ? ask.Price : Array.Empty<PriceTerm>(),
                    Enabled = true,
                    Bare = !offered,
                }));
            }

            foreach (var job in jobs)
            {
                var district = _city.Districts.FirstOrDefault(d => d.Id == job.DistrictId);
                if (district == null) continue;
                var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
                var left = Math.Ceiling(_construction.RemainingSeconds(job.Id, now) ?? 0);
                var cost = _rush.JobCost(job.Id, now);
                var task = job.TargetLevel > 1 && district.Built
                    ? _localizer.Tr("Upgrading to Lv {n}", ("n", _numbers.Exact(job.TargetLevel)))
                    : _localizer.Tr("Building");
                rows.Add((JOB + job.Id, new StoreWideData
                {
                    Name = _localizer.Capitalized(_localizer.Tr(building.DisplayName))
                           + (_construction.MaxCount(building) != 1 ? " #" + _numbers.Number(district.Ordinal) : string.Empty),
                    Art = building.ArtFor(Math.Max(1, district.Level)),
                    Line = task + " · <sprite name=\"hourglass\">" + _numbers.Duration(left),
                    Label = _localizer.Tr("Finish"),
                    Price = new[] { new PriceTerm(GEMS, _numbers.Count(cost), _treasury.Get(GEMS) < cost) },
                    Enabled = true,
                }));
            }

            for (var i = _builders.Count; i < _builders.Ceiling; i++)
            {
                var next = i == _builders.Count;
                rows.Add((next ? HIRE : "socket" + i, new StoreWideData
                {
                    Name = Nth(i),
                    Art = _icons.Get("builders"),
                    Line = string.Empty,
                    Label = _localizer.Tr("Hire"),
                    Price = next ? new[] { new PriceTerm(GEMS, _numbers.Count(_builders.GemCost), _treasury.Get(GEMS) < _builders.GemCost) } : Array.Empty<PriceTerm>(),
                    Enabled = true,
                    Bare = !next,
                }));
            }

            // The job's own words without their lead-in, in either language: build the Sawmill, construir el Aserradero.
            var subject = ask?.What == null ? null : System.Text.RegularExpressions.Regex.Replace(ask.What, "^(Ready to|Listo para) ", "");
            var head = free > 0
                ? (free == 1 ? _localizer.Tr("A builder is free") : _localizer.Tr("{n} builders are free", ("n", _numbers.Exact(free))))
                : _builders.Count == 1 ? _localizer.Tr("Your builder is busy") : _localizer.Tr("All {n} builders are busy", ("n", _numbers.Exact(_builders.Count)));
            var note = free > 0
                ? _localizer.Tr("{what} now, or keep it for later.", ("what", _localizer.Capitalized(subject ?? _localizer.Tr("it"))))
                : _localizer.Tr("Nothing waits in line — finish a job to free a builder.");
            View.Show(_localizer.Tr("Builders"), head, note, rows,
                _builders.AtCeiling ? _localizer.Tr("{n} is as large as a crew gets.", ("n", _numbers.Exact(_builders.Ceiling))) : null);
        }

        private string Nth(int i) => i switch
        {
            0 => _localizer.Tr("A first builder"),
            1 => _localizer.Tr("A second builder"),
            2 => _localizer.Tr("A third builder"),
            3 => _localizer.Tr("A fourth builder"),
            4 => _localizer.Tr("A fifth builder"),
            5 => _localizer.Tr("A sixth builder"),
            6 => _localizer.Tr("A seventh builder"),
            7 => _localizer.Tr("An eighth builder"),
            _ => _localizer.Tr("A {n}th builder", ("n", _numbers.Exact(i + 1))),
        };

        private async void OnRow(string id)
        {
            var now = _clock.NowMs;
            if (id.StartsWith(FREE))
            {
                var start = Data?.Start;
                if (start == null) return;
                await _ui.HideMenu<BuilderMenu>();
                start();
                return;
            }

            if (id == HIRE) _sounds.Play(_builders.Buy() == BuyBuilderResult.Purchased ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            else if (id.StartsWith(JOB)) _sounds.Play(_rush.FinishJob(id.Substring(JOB.Length), now) ? SoundIds.GEM_SPEND : SoundIds.ERROR);
            Refresh();
        }
    }
}
