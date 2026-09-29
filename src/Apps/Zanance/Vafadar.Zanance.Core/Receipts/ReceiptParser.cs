using System.Globalization;
using System.Text.RegularExpressions;
using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Receipts;

/// <summary>Values read from a receipt. Every value is a suggestion the user reviews before anything is saved (D-31).</summary>
/// <param name="Amount">The total, as a number in the receipt's notation (not yet in minor units).</param>
/// <param name="Date">The purchase date.</param>
/// <param name="Merchant">The shop or company, usually the first line.</param>
public sealed record ReceiptSuggestion(decimal? Amount, DateOnly? Date, string? Merchant)
{
    /// <summary>Gets a value indicating whether nothing usable was found.</summary>
    public bool IsEmpty => Amount is null && Date is null && Merchant is null;
}

/// <summary>A recognised word or line with its box, in any unit that grows downwards and to the right.</summary>
public readonly record struct ReceiptWord(double Top, double Bottom, double Left, string Text)
{
    /// <summary>Gets the vertical centre.</summary>
    public double Centre => (Top + Bottom) / 2;
}

/// <summary>
/// Reads the total, the date and the merchant from the text of a receipt (on-device OCR, D-31). It prefers a line with a
/// total keyword (Total, Summe, Gesamt, جمع, مبلغ قابل پرداخت …) and falls back to the largest amount. Persian and
/// Arabic digits are accepted; dates may be Gregorian or Solar Hijri (a year from 1300 to 1500).
/// </summary>
public static partial class ReceiptParser
{
    // Whole words or phrases, lower case.
    private static readonly string[] StrongTotal =
    [
        "total", "grand total", "total due", "amount due", "balance due", "to pay", "summe", "gesamtsumme", "gesamtbetrag",
        "endbetrag", "zu zahlen", "jumlah", "جمع کل", "مبلغ قابل پرداخت", "قابل پرداخت", "مبلغ کل",
    ];

    private static readonly string[] WeakTotal = ["betrag", "gesamt", "جمع", "مبلغ"];

    private static readonly string[] NotTotal =
    [
        "sub", "subtotal", "zwischensumme", "mwst", "ust", "steuer", "netto", "nettobetrag", "tax", "vat", "rabatt", "ersparnis",
        "gesamtersparnis", "discount", "items", "artikel", "bar", "gegeben", "rückgeld", "change", "cash", "tip",
        "تخفیف", "مالیات", "تعداد", "باقیمانده", "دریافتی",
    ];

    private static readonly string[] NotPrice = ["tel", "telefon", "fax", "plz", "iban", "bic", "nr", "no", "ust", "id", "steuernummer", "تلفن", "کد"];

    private static readonly string[] NotMerchant = ["receipt", "rechnung", "beleg", "quittung", "kassenbon", "bon", "invoice", "tel", "fax", "ust", "vat", "فاکتور", "رسید", "تلفن"];

    /// <summary>Reads a receipt text.</summary>
    public static ReceiptSuggestion Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ReceiptSuggestion(null, null, null);
        }

        // Persian and Arabic separators become Latin ones; OCR sometimes puts a space after the decimal separator ("2, 90").
        var normalized = SplitDecimals().Replace(Digits.ToAscii(text).Replace('٫', '.').Replace('٬', ',').Replace('،', ','), "$1$2$3");
        var lines = normalized.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ReceiptSuggestion(FindTotal(lines), FindDate(lines), FindMerchant(lines));
    }

    /// <summary>
    /// Builds the text rows of a receipt from recognised words. OCR engines return text in blocks or columns, so a price
    /// can end up far from its label; words whose vertical centres lie within half a line height form one row, read in
    /// reading order (by <see cref="ReceiptWord.Left"/>, right to left for rows in Arabic script).
    /// </summary>
    public static string? Rows(IEnumerable<ReceiptWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var rows = new List<List<ReceiptWord>>();
        foreach (var word in words.OrderBy(w => w.Centre))
        {
            var height = Math.Max(word.Bottom - word.Top, double.Epsilon);
            var row = rows.LastOrDefault();
            if (row is not null && Math.Abs(word.Centre - row.Average(w => w.Centre)) < height / 2)
            {
                row.Add(word);
            }
            else
            {
                rows.Add([word]);
            }
        }

        return rows.Count == 0 ? null : string.Join('\n', rows.Select(r => string.Join(' ', (IsRightToLeft(r) ? r.OrderByDescending(w => w.Left) : r.OrderBy(w => w.Left)).Select(w => w.Text))));
    }

    // A row written mostly in Arabic script (Persian) is read from right to left.
    private static bool IsRightToLeft(List<ReceiptWord> row)
    {
        var text = string.Concat(row.Select(w => w.Text));
        return text.Count(c => c is >= '\u0600' and <= '\u06FF') > text.Count(char.IsAsciiLetter);
    }

    /// <summary>Reads an amount like <c>1.234,56</c>, <c>1,234.56</c>, <c>12,50</c> or <c>125,000</c>.</summary>
    public static bool TryAmount(string text, out decimal amount)
    {
        amount = 0;
        var value = text.Trim().TrimStart('-', '+');
        if (value.Length == 0)
        {
            return false;
        }

        var lastComma = value.LastIndexOf(',');
        var lastDot = value.LastIndexOf('.');
        string normalized;
        if (lastComma >= 0 && lastDot >= 0)
        {
            // The later separator is the decimal one: 1.234,56 or 1,234.56.
            normalized = lastComma > lastDot ? value.Replace(".", string.Empty).Replace(',', '.') : value.Replace(",", string.Empty);
        }
        else if (lastComma >= 0 || lastDot >= 0)
        {
            var separator = lastComma >= 0 ? ',' : '.';
            var index = Math.Max(lastComma, lastDot);
            var decimals = value.Length - index - 1;
            var groups = value.Count(c => c == separator);

            // One or two digits after a single separator are cents; three digits (or several separators) are thousands.
            normalized = groups == 1 && decimals is 1 or 2 ? value.Replace(separator, '.') : value.Replace(separator.ToString(), string.Empty);
        }
        else
        {
            normalized = value;
        }

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }

    private static decimal? FindTotal(string[] lines)
    {
        // A strong total keyword first, then a weak one; the last matching line wins because a subtotal comes earlier.
        // Tax, discount, item-count, cash-given and change lines never hold the total.
        foreach (var keywords in new[] { StrongTotal, WeakTotal })
        {
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var tokens = Tokens(lines[i]);
                if (!keywords.Any(k => Has(tokens, k)) || NotTotal.Any(k => Has(tokens, k)))
                {
                    continue;
                }

                var amounts = Amounts(lines[i], strict: false).ToList();
                if (amounts.Count == 0 && i + 1 < lines.Length && !NotTotal.Any(k => Has(Tokens(lines[i + 1]), k)))
                {
                    amounts = [.. Amounts(lines[i + 1], strict: false)];
                }

                if (amounts.Count > 0)
                {
                    return amounts[^1];
                }
            }
        }

        // No total line: the largest price-like amount, never a postal code, phone number or id.
        var prices = lines.Where(l => !NotPrice.Any(k => Has(Tokens(l), k))).SelectMany(l => Amounts(l, strict: true)).ToList();
        return prices.Count > 0 ? prices.Max() : null;
    }

    private static IEnumerable<decimal> Amounts(string line, bool strict)
    {
        // Dates and times are not amounts.
        var text = TimePattern().Replace(DatePattern().Replace(line, " "), " ");
        foreach (Match match in AmountPattern().Matches(text))
        {
            // Strict: only numbers written like prices (with decimals or thousands groups).
            if (strict && !match.Value.Contains(',') && !match.Value.Contains('.'))
            {
                continue;
            }

            if (TryAmount(match.Value, out var amount) && amount < 100_000_000_000m)
            {
                yield return amount;
            }
        }
    }

    // The lower-case words of a line, padded with spaces so that whole words and phrases can be found.
    private static string Tokens(string line) =>
        " " + string.Join(' ', WordPattern().Matches(line.ToLowerInvariant()).Select(m => m.Value)) + " ";

    private static bool Has(string tokens, string phrase) => tokens.Contains(" " + phrase + " ", StringComparison.Ordinal);

    private static DateOnly? FindDate(string[] lines)
    {
        foreach (var line in lines)
        {
            foreach (Match match in DatePattern().Matches(line))
            {
                if (TryDate(match, out var date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private static bool TryDate(Match match, out DateOnly date)
    {
        date = default;
        var parts = match.Value.Split(['.', '/', '-']);
        if (parts.Length != 3 || !parts.All(p => int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
        {
            return false;
        }

        var numbers = parts.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        int year, month, day;
        if (parts[0].Length == 4)
        {
            (year, month, day) = (numbers[0], numbers[1], numbers[2]);
        }
        else
        {
            (day, month, year) = (numbers[0], numbers[1], numbers[2] < 100 ? 2000 + numbers[2] : numbers[2]);
        }

        try
        {
            if (year is >= 1300 and <= 1500)
            {
                date = DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0));
                return true;
            }

            if (year is >= 2000 and <= 2100 && month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month))
            {
                date = new DateOnly(year, month, day);
                return true;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // Not a valid date in that calendar.
        }

        return false;
    }

    private static string? FindMerchant(string[] lines) =>
        lines.Take(4).FirstOrDefault(l =>
        {
            var tokens = Tokens(l);
            return l.Count(char.IsLetter) >= 3
                && !NotMerchant.Any(k => Has(tokens, k))
                && !StrongTotal.Concat(WeakTotal).Any(k => Has(tokens, k))
                && !l.Contains("www.", StringComparison.OrdinalIgnoreCase) && !l.Contains('@');
        })?.Trim();

    [GeneratedRegex(@"\d{1,3}(?:[.,]\d{3})+(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"\b\d{1,4}[./-]\d{1,2}[./-]\d{2,4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\b", RegexOptions.CultureInvariant)]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"(\d)([.,]) +(\d{2})\b", RegexOptions.CultureInvariant)]
    private static partial Regex SplitDecimals();

    [GeneratedRegex(@"[\p{L}\p{M}\u200C]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
