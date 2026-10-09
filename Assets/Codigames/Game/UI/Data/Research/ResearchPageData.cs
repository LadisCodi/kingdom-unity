using System.Collections.Generic;
using Codigames.Game.UI.Research;

namespace Codigames.Game.UI.Data.Research
{
    // A book's whole page, laid out: its height in page pixels, its chapter headings and its connectors.
    // The cards come separately, as they change far more often.
    public class ResearchPageData
    {
        public ResearchPageData(string tome, float height, IReadOnlyList<ChapterData> chapters, IReadOnlyList<EdgePiece> edges)
        {
            Tome = tome;
            Height = height;
            Chapters = chapters;
            Edges = edges;
        }

        public string Tome { get; }
        public float Height { get; }
        public IReadOnlyList<ChapterData> Chapters { get; }
        public IReadOnlyList<EdgePiece> Edges { get; }
    }
}
