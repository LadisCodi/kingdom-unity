using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Data.City;
using Codigames.Game.Data.Sites;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Menus;
using Codigames.Kingdom.City.State;
using Codigames.Kingdom.Notices;
using Codigames.Kingdom.Sites;
using Codigames.Modules.Localization;
using Codigames.Modules.UI;

namespace Codigames.Game.UI.Notices
{
    // What the notices say (Docs/features/26-notices.md §2–§6): the column's bubbles in the order it shows them — the
    // news newest first, folded under a +N past the dial — and what each one's card holds. Nothing here changes the
    // game but Go.
    public class NoticeBoard
    {
        public const string MORE = "more";
        private const string NEWS = "news:";

        private readonly Inbox _inbox;
        private readonly INoticeSettings _settings;
        private readonly CityState _city;
        private readonly BuildingCollection _buildings;
        private readonly IProvinceSites _sites;
        private readonly ProvinceSitesAsset _siteArt;
        private readonly UiIcons _icons;
        private readonly NumberFormat _numbers;
        private readonly Localizer _localizer;
        private readonly UIManager _ui;

        public NoticeBoard(Inbox inbox, INoticeSettings settings, CityState city, BuildingCollection buildings, IProvinceSites sites,
            ProvinceSitesAsset siteArt, UiIcons icons, NumberFormat numbers, Localizer localizer, UIManager ui)
        {
            _inbox = inbox;
            _settings = settings;
            _city = city;
            _buildings = buildings;
            _sites = sites;
            _siteArt = siteArt;
            _icons = icons;
            _numbers = numbers;
            _localizer = localizer;
            _ui = ui;
        }

        // Every news notice, newest first.
        public IReadOnlyList<Notice> All => _inbox.Groups.Select(NewsNotice).Where(n => n != null).ToList();

        // The column: at most the dial's bubbles; past it one fewer, and the +N on top for the rest.
        public IReadOnlyList<Notice> Column
        {
            get
            {
                var all = All;
                if (all.Count <= _settings.Shown) return all;
                var kept = all.Take(_settings.Shown - 1).ToList();
                return new[] { More(all, all.Count - kept.Count) }.Concat(kept).ToList();
            }
        }

        // A card's notice, by its id; null once it has nothing to say.
        public Notice Of(string id)
        {
            if (id == MORE)
            {
                var all = All;
                return all.Count == 0 ? null : More(all, all.Count);
            }

            return Group(id) is { } group ? NewsNotice(group) : null;
        }

        // Opening a news's card reads its group: the bubble goes at once, and the card keeps what it said.
        public void Read(string id)
        {
            if (Group(id) is { } group) _inbox.Read(group);
        }

        private static NewsGroup? Group(string id)
            => id != null && id.StartsWith(NEWS) && Enum.TryParse<NewsGroup>(id.Substring(NEWS.Length), out var group) ? group : null;

        private Notice More(IReadOnlyList<Notice> all, int more) => new()
        {
            Id = MORE,
            Kind = NoticeKind.More,
            Carved = "+" + _numbers.Exact(more),
            Title = _localizer.Tr("Notices"),
            Rows = all.Select(n => new NoticeRowData
            {
                Art = n.Art, ArtIsBuilding = n.ArtIsBuilding, Name = n.Title, Line = string.Empty, Opens = n.Id,
            }).ToList(),
        };

        // A group of news as a notice: one card, or one line each.
        private Notice NewsNotice(NewsGroup group)
        {
            var lines = _inbox.Of(group).Select(Line).Where(l => l != null).ToList();
            if (lines.Count == 0) return null;
            var lead = lines[0];
            var one = lines.Count == 1;
            return new Notice
            {
                Id = NEWS + group,
                Kind = NoticeKind.News,
                Art = lead.Row.Art,
                ArtIsBuilding = lead.Row.ArtIsBuilding,
                Count = lines.Count,
                Title = one ? lead.Title : GroupTitle(group, lines.Count),
                Body = one ? lead.Body : string.Empty,
                Picture = one ? lead.Row.Art : null,
                Rows = one ? Array.Empty<NoticeRowData>() : lines.Select(l => l.Row).ToList(),
                Go = one ? lead.Row.Go : null,
            };
        }

        private string GroupTitle(NewsGroup group, int count) => group switch
        {
            NewsGroup.Built => _localizer.Tr("{n} buildings finished", ("n", _numbers.Exact(count))),
            NewsGroup.Sighted => _localizer.Tr("{n} new places", ("n", _numbers.Exact(count))),
            _ => _localizer.Tr("The chain is done"),
        };

        private sealed class NewsLine
        {
            public NoticeRowData Row;
            public string Title;
            public string Body;
        }

        private NewsLine Line(News news) => news.Group switch
        {
            NewsGroup.Built => Built(news),
            NewsGroup.Sighted => Sighted(news),
            NewsGroup.ChainDone => new NewsLine
            {
                Row = new NoticeRowData
                {
                    Art = _icons.Get("crest"),
                    Name = _localizer.Tr("Your kingdom stands on its own"),
                    Line = _localizer.Tr("The chain is done"),
                },
                Title = _localizer.Tr("The chain is done"),
                Body = _localizer.Tr("No more guidance — build whatever you like from here."),
            },
            _ => null,
        };

        private NewsLine Built(News news)
        {
            var district = _city.Districts.FirstOrDefault(d => d.Id == news.District);
            if (district == null) return null;
            var building = _buildings.Get<BuildingAsset>(district.DefinitionId);
            var name = _localizer.Tr(building.DisplayName);
            var built = news.Level == 1;
            var level = _numbers.Exact(news.Level);
            return new NewsLine
            {
                Row = new NoticeRowData
                {
                    Art = building.ArtFor(news.Level),
                    ArtIsBuilding = true,
                    Name = name + " #" + _numbers.Exact(district.Ordinal),
                    Line = built ? _localizer.Tr("Built") : _localizer.Tr("Now level {n}", ("n", level)),
                    Go = () => Open<DistrictCardMenu>(district.Id),
                },
                Title = built ? _localizer.Tr("Construction complete!") : _localizer.Tr("Upgrade complete!"),
                Body = built ? _localizer.Tr(building.Description) : _localizer.Tr("{name} is now level {n}.", ("name", name), ("n", level)),
            };
        }

        private NewsLine Sighted(News news)
        {
            if (_sites.Landmarks.FirstOrDefault(l => l.Id == news.Site) is { } landmark)
            {
                var kind = _siteArt.KindOf(landmark.Kind);
                return Site(kind?.Art, _localizer.Tr(kind?.Name ?? landmark.Kind), _localizer.Tr("A place of power!"),
                    _localizer.Tr("Clear a path to it and claim it."), () => Open<LandmarkCardMenu>(landmark.Id));
            }

            if (_sites.Abandoned.FirstOrDefault(r => r.Id == news.Site) is { } ruin)
            {
                var building = _buildings.Get<BuildingAsset>(ruin.District);
                return Site(building.RuinArt, _localizer.Tr(ruin.Name), _localizer.Tr("An abandoned building!"),
                    _localizer.Tr("Clear the fog off it, then repair it."), () => Open<RuinCardMenu>(ruin.Id));
            }

            return null;
        }

        private static NewsLine Site(UnityEngine.Sprite art, string name, string title, string what, Action go) => new()
        {
            Row = new NoticeRowData { Art = art, Name = name, Line = title.Trim('¡', '!'), Go = go },
            Title = title,
            Body = name + ". " + what,
        };

        // Go closes the card first, then opens the subject's own card, which brings the camera to it.
        private async void Open<TMenu>(string id) where TMenu : IMenuView
        {
            await _ui.CloseAll();
            _ = _ui.ShowMenu<TMenu, string>(id);
        }
    }
}
