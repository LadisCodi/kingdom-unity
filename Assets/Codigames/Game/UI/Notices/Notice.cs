using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Game.UI.Notices
{
    public enum NoticeKind
    {
        News,
        State,
        More,
    }

    // One bubble and the card it opens (Docs/features/26-notices.md §4–§5): its picture, the seal's count, the card's
    // title, paragraph and wide picture, a group's rows, and where Go takes the player.
    public sealed class Notice
    {
        // "news:<group>", "state:<name>" or "more".
        public string Id { get; set; }
        public NoticeKind Kind { get; set; }
        public Sprite Art { get; set; }

        // A building's art carries sky over its roof: drawn larger, its ground kept in the face.
        public bool ArtIsBuilding { get; set; }

        // The +N's number, carved into the face instead of a picture.
        public string Carved { get; set; }

        // The red seal's number: a group of two or more.
        public int Count { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Sprite Picture { get; set; }
        public IReadOnlyList<NoticeRowData> Rows { get; set; } = Array.Empty<NoticeRowData>();
        public Action Go { get; set; }

        // A threat (a raid): its face a dark red disc, the creature on it.
        public bool Threat { get; set; }

        // Epoch milliseconds a standing notice counts down to, on a plaque under the bubble; null for none.
        public double? Until { get; set; }
    }

    // A line of a group's card, or of the +N's: its picture, name and line, and its own Go — or the notice it opens.
    public sealed class NoticeRowData
    {
        public Sprite Art { get; set; }
        public bool ArtIsBuilding { get; set; }
        public string Carved { get; set; }
        public string Name { get; set; }
        public string Line { get; set; }
        public Action Go { get; set; }
        public string Opens { get; set; }
    }
}
