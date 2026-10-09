using System;

namespace Codigames.Modules.Localization
{
    // Numbers as a player reads them, in the player's culture: 25,000 and 4.99 in English, 25.000 and 4,99
    // in Spanish. Nothing else calls ToString on a number the player reads.
    public class NumberFormat
    {
        private readonly Localizer _localizer;

        public NumberFormat(Localizer localizer)
        {
            _localizer = localizer;
        }

        // Grouped, with up to maxDecimals decimals (at least minDecimals). Spanish groups only from five figures
        // (4100, 25.000), as the CLDR's minimum grouping digits say and the web's Intl.NumberFormat writes it.
        public string Number(double value, int maxDecimals = 0, int minDecimals = 0)
        {
            var rounded = Math.Round(value, maxDecimals, MidpointRounding.AwayFromZero);
            var pattern = Math.Abs(rounded) < GroupingFrom ? "0" : "#,##0";
            if (maxDecimals > 0) pattern += "." + new string('0', minDecimals) + new string('#', maxDecimals - minDecimals);

            return rounded.ToString(pattern, _localizer.Culture);
        }

        // The least number that is written grouped in the player's language.
        private double GroupingFrom => _localizer.Culture.TwoLetterISOLanguageName == "es" ? 10_000 : 1_000;

        // The exact figure, grouped: 25,000. For a prize or a price the game knows exactly.
        public string Exact(double value) => Number(value, 1);

        // A wallet number short enough for the header: 9,999, then 12k, then 1.2M. Display only.
        public string Count(double value)
        {
            var abs = Math.Abs(value);
            if (abs < 10_000) return Number(value, 1);
            if (abs < 1_000_000) return Number(Math.Truncate(value / 1000)) + "k";

            var millions = value / 1_000_000;
            return Number(millions, Math.Abs(millions) < 10 ? 1 : 0) + "M";
        }

        // A count cut short for a tight slot: 950, 1.2k, 29k. Floored, so a store one short of full never
        // reads as full.
        public string Short(double value)
        {
            var abs = Math.Abs(value);
            if (abs >= 1000 && abs < 10_000) return Number(Math.Floor(value / 100) / 10, 1) + "k";
            return Count(Math.Floor(value));
        }

        // A price in dollars: $4.99, $2,000.00.
        public string Usd(int cents) => "$" + Number(cents / 100.0, 2, 2);

        // A duration in its two largest units: 45s, 2m 5s, 3h 10m, 1d 4h.
        public string Duration(double seconds)
        {
            if (seconds <= 0) return _localizer.Tr("instant");
            if (seconds < 60) return Number(Math.Round(seconds)) + "s";

            if (seconds < 3600)
            {
                var m = Math.Floor(seconds / 60);
                var s = Math.Round(seconds % 60);
                if (s >= 60) return Number(m + 1) + "m";
                return s > 0 ? $"{Number(m)}m {Number(s)}s" : Number(m) + "m";
            }

            if (seconds < 86_400)
            {
                var h = Math.Floor(seconds / 3600);
                var m = Math.Round(seconds % 3600 / 60);
                if (m >= 60) return Number(h + 1) + "h";
                return m > 0 ? $"{Number(h)}h {Number(m)}m" : Number(h) + "h";
            }

            var d = Math.Floor(seconds / 86_400);
            var hours = Math.Round(seconds % 86_400 / 3600);
            if (hours >= 24) return Number(d + 1) + "d";
            return hours > 0 ? $"{Number(d)}d {Number(hours)}h" : Number(d) + "d";
        }

        // A countdown that does not change width every second: seconds only in the last minute, whole
        // minutes (rounded up) below an hour, two units above.
        public string Countdown(double seconds)
            => seconds < 60 ? Duration(seconds) : Duration(Math.Ceiling(seconds / 60) * 60);
    }
}
