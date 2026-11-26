using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// A time a visitor asked for (ADR 0021, R-PUB-04): a year of the common era or before it, a stretch of such
/// years, or a depth of time – so many years, thousand, million or billion years ago, or a stretch between two
/// such depths. There is one reader of what a visitor typed, <see cref="Read"/>, and every place of the site
/// that asks for a time uses it: the search, "what held at this time", and the stretch of a chart.
/// </summary>
public abstract partial record AskedTime
{
    /// <summary>The furthest year of an era that can be asked for; the calendar of the library ends there.</summary>
    public const int FurthestYear = CalendarPosition.MaxYear;

    private AskedTime()
    {
    }

    /// <summary>Gets the time as the reader takes it again: plain letters and digits, for an address.</summary>
    public abstract string Written { get; }

    /// <summary>
    /// Returns the stretch of the axis of years that is asked for. A depth of time is counted back from the
    /// year the visitor asks in.
    /// </summary>
    /// <param name="present">The year of the common era the visitor asks in.</param>
    public abstract YearSpan Span(int present);

    /// <summary>
    /// Reads what a visitor typed: "1200", "500 BCE", "-500", "600 to 500 BCE", "12 ka", "55 million years
    /// ago", "50-60 Ma", a year of a Hijri calendar ("1400 SH", "400 AH") and the same in Persian ("۵۰۰ پ.م"، "۵۰ تا ۶۰ میلیون سال پیش"). Digits of Persian
    /// and Arabic are read as digits. What is no time – no number, a year 0, a year beyond the calendar, a
    /// stretch that ends before it begins – is <see langword="null"/>, never an error.
    /// </summary>
    /// <param name="typed">What was typed.</param>
    public static AskedTime? Read(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed) || typed.Length > 80)
        {
            return null;
        }

        var text = Fold(typed);
        if (DepthOfTime().Match(text) is { Success: true } depth)
        {
            var unit = UnitOf(depth.Groups["unit"].Value);
            if (!Number(depth.Groups["first"].Value, out var first))
            {
                return null;
            }

            var second = first;
            if (depth.Groups["second"].Success && !Number(depth.Groups["second"].Value, out second))
            {
                return null;
            }

            var (older, younger) = (Math.Max(first, second) * unit, Math.Min(first, second) * unit);
            return older > AgePosition.MaxYears || older <= 0 ? null : new Ago(older, younger);
        }

        // A year of a Hijri calendar ("1400 SH", "۴۰۰ ه.ق") asks for the years of the common era it lies in.
        if (HijriYear().Match(text) is { Success: true } hijri)
        {
            var calendar = hijri.Groups["solar"].Success ? CalendarSystem.SolarHijri : CalendarSystem.LunarHijri;
            return int.TryParse(hijri.Groups["year"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var stated)
                && CalendarConversion.CommonEraYearsOf(calendar, stated) is { } covered && covered.Last <= FurthestYear
                ? new Years(new(Era.CommonEra, covered.First), new(Era.CommonEra, covered.Last))
                : null;
        }

        if (YearsOfEra().Match(text) is { Success: true } years)
        {
            var last = years.Groups["second"].Success ? Year(years.Groups["second"].Value, years.Groups["secondEra"].Value, null) : null;
            if (years.Groups["second"].Success && last is null)
            {
                return null;
            }

            // "600 to 500 BCE": the era that is said once is said of both, where the first would otherwise lie after the second.
            var first = Year(years.Groups["first"].Value, years.Groups["firstEra"].Value, last is { Era: Era.BeforeCommonEra } && years.Groups["firstEra"].Value.Length == 0 ? Era.BeforeCommonEra : null);
            if (first is null)
            {
                return null;
            }

            return (last ?? first).Value.Place < first.Value.Place ? null : new Years(first.Value, last ?? first.Value);
        }

        return null;
    }

    private static YearOfEra? Year(string digits, string era, Era? otherwise)
    {
        var before = digits.StartsWith('-');
        if (!int.TryParse(digits.TrimStart('-'), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number is < 1 or > FurthestYear)
        {
            return null;
        }

        var named = era switch
        {
            "" => (Era?)null,
            "bce" or "bc" or "ق.م" or "ق.م." or "پ.م" or "پ.م." or "پیش از میلاد" => Era.BeforeCommonEra,
            _ => Era.CommonEra,
        };
        if (before && named == Era.CommonEra)
        {
            return null;
        }

        return new(before ? Era.BeforeCommonEra : named ?? otherwise ?? Era.CommonEra, number);
    }

    private static bool Number(string digits, out decimal number) =>
        decimal.TryParse(digits, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number) && number > 0;

    private static decimal UnitOf(string unit) => unit switch
    {
        "ka" or "kya" or "thousand years ago" or "هزار سال پیش" => 1_000m,
        "ma" or "mya" or "million years ago" or "میلیون سال پیش" => 1_000_000m,
        "ga" or "bya" or "billion years ago" or "میلیارد سال پیش" => 1_000_000_000m,
        _ => 1m,
    };

    // Digits of every script as digits, separators of thousands gone, one kind of space, small letters.
    private static string Fold(string typed)
    {
        var text = new StringBuilder(typed.Length);
        foreach (var letter in typed.Trim().Normalize(NormalizationForm.FormKC))
        {
            var digit = CharUnicodeInfo.GetDecimalDigitValue(letter);
            text.Append(digit >= 0 ? (char)('0' + digit) : letter switch
            {
                '٫' => '.',
                '٬' or ' ' or ' ' => ',',
                '−' or '–' or '—' => '-',
                '‌' or ' ' => ' ',
                _ => char.ToLowerInvariant(letter),
            });
        }

        return Regex.Replace(Regex.Replace(text.ToString(), @"(?<=\d),(?=\d{3}(\D|$))", string.Empty), @"\s+", " ");
    }

    [GeneratedRegex(@"^(?<first>\d+(\.\d+)?)( ?(-|to|تا) ?(?<second>\d+(\.\d+)?))? ?(?<unit>ka|kya|ma|mya|ga|bya|thousand years ago|million years ago|billion years ago|years ago|هزار سال پیش|میلیون سال پیش|میلیارد سال پیش|سال پیش)$", RegexOptions.CultureInvariant)]
    private static partial Regex DepthOfTime();

    // "sh", "ah" and the Persian words and short forms, with or without the joiner and the points.
    [GeneratedRegex(@"^(?<year>\d+) ?((?<solar>sh|ه[\u200d ]?\.? ?ش\.?|هجری (شمسی|خورشیدی)|شمسی|خورشیدی)|(?<lunar>ah|ه[\u200d ]?\.? ?ق\.?|هجری قمری|قمری))$", RegexOptions.CultureInvariant)]
    private static partial Regex HijriYear();

    private const string EraWords = @"bce|bc|ce|ad|ق\.م\.?|پ\.م\.?|پیش از میلاد|میلادی|م\.?";

    [GeneratedRegex(@"^(?<first>-?\d+) ?(?<firstEra>" + EraWords + @")?( ?(-|to|تا|\.\.) ?(?<second>-?\d+) ?(?<secondEra>" + EraWords + @")?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex YearsOfEra();

    /// <summary>A year, or a stretch of years from the first to the last, both among them.</summary>
    /// <param name="First">The first year.</param>
    /// <param name="Last">The last year; the first, where one year is asked for.</param>
    public sealed record Years(YearOfEra First, YearOfEra Last) : AskedTime
    {
        /// <summary>Gets a value indicating whether one year is asked for.</summary>
        public bool IsOneYear => First == Last;

        /// <inheritdoc />
        public override string Written => IsOneYear ? Of(First) : Of(First) + " to " + Of(Last);

        /// <inheritdoc />
        public override YearSpan Span(int present) => new(First.Place, Last.Place + 1);

        private static string Of(YearOfEra year) => year.Number.ToString(CultureInfo.InvariantCulture) + (year.Era == Era.BeforeCommonEra ? " BCE" : string.Empty);
    }

    /// <summary>A depth of time: so many years before the year the visitor asks in.</summary>
    /// <param name="Oldest">The further end, in years.</param>
    /// <param name="Youngest">The nearer end, in years; the same, where one depth is asked for.</param>
    public sealed record Ago(decimal Oldest, decimal Youngest) : AskedTime
    {
        /// <summary>
        /// Gets the unit the depth is said in: the largest in which both ends are round – at most two places
        /// after the point – and years otherwise, so that "1,028 years ago" is not said as "1.028 thousand".
        /// </summary>
        public AgeUnit Unit =>
            Oldest >= 1_000_000_000m && Round(1_000_000_000m) ? AgeUnit.BillionYears
            : Oldest >= 1_000_000m && Round(1_000_000m) ? AgeUnit.MillionYears
            : Oldest >= 1_000m && Round(1_000m) ? AgeUnit.ThousandYears
            : AgeUnit.Years;

        /// <summary>Gets the number of years of the unit the depth is said in.</summary>
        public decimal UnitSize => Unit switch
        {
            AgeUnit.BillionYears => 1_000_000_000m,
            AgeUnit.MillionYears => 1_000_000m,
            AgeUnit.ThousandYears => 1_000m,
            _ => 1m,
        };

        /// <inheritdoc />
        public override string Written
        {
            get
            {
                var (size, name) = (UnitSize, Unit switch { AgeUnit.BillionYears => "Ga", AgeUnit.MillionYears => "Ma", AgeUnit.ThousandYears => "ka", _ => "years ago" });
                string Of(decimal years) => (years / size).ToString("0.######", CultureInfo.InvariantCulture);
                return (Oldest == Youngest ? Of(Oldest) : Of(Youngest) + " to " + Of(Oldest)) + " " + name;
            }
        }

        /// <summary>
        /// Gets how far to each side one depth of time is taken. A depth is a rounded number, and "ago" is
        /// counted from different years by different writers (the year of the writing, 1950, 2000): somebody
        /// who asks for "55 million years ago" asks for what is said to be 55 million years old. So one depth
        /// is taken with half of the place of its last digit that is not a nought to each side – half a
        /// million years for 55 million – and never with less than a hundred years. A stretch between two
        /// depths says its ends itself and is taken as it is.
        /// </summary>
        public decimal Slack
        {
            get
            {
                if (Oldest != Youngest)
                {
                    return 0;
                }

                var place = 1m;
                while (place * 10 <= Oldest && decimal.Truncate(Oldest / (place * 10)) * place * 10 == Oldest)
                {
                    place *= 10;
                }

                return Math.Max(place / 2, 100m);
            }
        }

        /// <inheritdoc />
        public override YearSpan Span(int present)
        {
            var now = YearAxis.StartOf(Era.CommonEra, present);
            return new(now - Oldest - Slack, now - Youngest + Slack + 1);
        }

        private bool Round(decimal unit) => decimal.Round(Oldest / unit, 2) * unit == Oldest && decimal.Round(Youngest / unit, 2) * unit == Youngest;
    }
}

/// <summary>
/// Where a position of time lies on the one axis of years the site asks by (ADR 0021): a year of the common
/// era or before it where the calendar axis puts it, and a depth of time so many years before the year it
/// counts from. A position of a telling, or of an era the axis does not know, has no place there.
/// </summary>
public static class TimePlaces
{
    /// <summary>Returns the place of a position, or <see langword="null"/> when it has none on the axis of years.</summary>
    /// <param name="position">The position.</param>
    /// <param name="present">
    /// The year of the common era a depth of time is counted from when it does not say itself what it counts
    /// from ("years ago"); <see langword="null"/> where a depth of time is not to be placed at all.
    /// </param>
    public static YearSpan? Of(TimePosition position, int? present)
    {
        if (position is not AgePosition age)
        {
            return YearAxis.Of(position);
        }

        if (present is null && !age.Reference.IsSpecified)
        {
            return null;
        }

        var from = YearAxis.StartOf(Era.CommonEra, age.Reference.CommonEraYear ?? present!.Value) - age.Years;
        return new(from, from + 1);
    }

    /// <summary>
    /// Returns whether a position is less than exact about its years: said to be "about", counted in
    /// radiocarbon years, or a depth of time that does not say what it is counted from.
    /// </summary>
    public static bool IsLoose(TimePosition? position) => position switch
    {
        CalendarPosition calendar => calendar.IsApproximate,
        AgePosition age => age.IsApproximate || !age.Reference.IsSpecified || age.Basis != AgeBasis.CalendarYears,
        _ => false,
    };
}
