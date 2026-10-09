using System.Collections.Generic;
using System.Text;
using Codigames.Game.UI.Data;

namespace Codigames.Game.UI.Widgets
{
    // A price as one line of text: each currency's icon inline (TextMeshPro's default sprite asset, Art/UI/InlineIcons,
    // names its glyphs by currency id) and its figure, in clay when short. Two spacings, the web's two prices: a
    // button's (.k-btn-cost, terms 10 px apart) and a buy box's (.k-price, 18 px apart, 6 px from mark to figure).
    // Gaps are in em so they follow the role's size.
    public static class PriceLine
    {
        private const string SHORT = "#D4553E";
        private const string GAP = "<space=0.58em>";
        private const string WIDE_GAP = "<space=1.04em>";
        private const string WIDE_MARK = "<space=0.29em>";

        public static string Of(IReadOnlyList<PriceTerm> terms, bool wide = false)
        {
            var line = new StringBuilder();
            for (var i = 0; i < terms.Count; i++)
            {
                if (i > 0) line.Append(wide ? WIDE_GAP : GAP);
                Append(line, terms[i], wide);
            }
            return line.ToString();
        }

        // A buy box's line with something after the price — its wait, an hourglass and a muted time.
        public static string Of(IReadOnlyList<PriceTerm> terms, bool wide, string trailing)
            => string.IsNullOrEmpty(trailing) ? Of(terms, wide) : Of(terms, wide) + (terms.Count > 0 ? wide ? WIDE_GAP : GAP : "") + trailing;

        public static string Of(PriceTerm term) => Append(new StringBuilder(), term, false).ToString();

        private static StringBuilder Append(StringBuilder line, PriceTerm term, bool wide)
        {
            line.Append("<sprite name=\"").Append(term.Currency).Append("\">");
            if (wide) line.Append(WIDE_MARK);
            return term.IsShort
                ? line.Append("<color=").Append(SHORT).Append('>').Append(term.Amount).Append("</color>")
                : line.Append(term.Amount);
        }
    }
}
