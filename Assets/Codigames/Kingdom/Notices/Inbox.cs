using System;
using System.Collections.Generic;
using System.Linq;
using Codigames.Kingdom.Notices.State;

namespace Codigames.Kingdom.Notices
{
    // The news inbox (Docs/features/26-notices.md §1, §7): filed newest first, the same event once, capped; a group is
    // read all at once, when its bubble is opened.
    public class Inbox
    {
        private readonly NoticesState _state;
        private readonly INoticeSettings _settings;

        public Inbox(NoticesState state, INoticeSettings settings)
        {
            _state = state;
            _settings = settings;
        }

        // A news filed or a group read.
        public event Action Changed;

        public IReadOnlyList<News> All => _state.News;

        public void Post(News news)
        {
            if (_state.News.Any(n => n.Key == news.Key)) return;
            var at = _state.News.FindIndex(n => n.At <= news.At);
            _state.News.Insert(at < 0 ? _state.News.Count : at, news);
            if (_state.News.Count > _settings.Kept) _state.News.RemoveRange(_settings.Kept, _state.News.Count - _settings.Kept);
            Changed?.Invoke();
        }

        // Files a news that adds up: the same event again raises its count instead of filing a second.
        public void Tally(News news)
        {
            var filed = _state.News.FirstOrDefault(n => n.Key == news.Key);
            if (filed == null)
            {
                Post(news);
                return;
            }

            filed.Count += news.Count;
            Changed?.Invoke();
        }

        // A group's news, newest first.
        public IReadOnlyList<News> Of(NewsGroup group) => _state.News.Where(n => n.Group == group).ToList();

        // The groups holding news, newest first; a tie in the groups' own order.
        public IReadOnlyList<NewsGroup> Groups
            => _state.News.GroupBy(n => n.Group).OrderByDescending(g => g.Max(n => n.At)).ThenBy(g => g.Key).Select(g => g.Key).ToList();

        public void Read(NewsGroup group)
        {
            if (_state.News.RemoveAll(n => n.Group == group) > 0) Changed?.Invoke();
        }
    }
}
