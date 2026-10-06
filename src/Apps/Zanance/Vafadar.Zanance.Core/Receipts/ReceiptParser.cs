using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vafadar.Core.Dates;
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

/// <summary>
/// Reads the total, the date and the merchant from the text of a receipt (on-device OCR, D-31). It prefers a line with a
/// total keyword (Total, Summe, Gesamt, Totale, جمع, مبلغ قابل پرداخت …) and falls back to the largest amount. Persian and
/// Arabic digits are accepted; dates may be Gregorian, Solar Hijri or lunar Hijri (a year from 1300 to 1500 is read in the
/// Hijri calendar that gives the date nearer to today: solar years are now around 1405, lunar years around 1447).
/// </summary>
public static partial class ReceiptParser
{
    // Whole words or phrases, lower case.
    private static readonly string[] StrongTotal =
    [
        "total", "grand total", "total due", "amount due", "balance due", "to pay", "summe", "gesamtsumme", "gesamtbetrag",
        "endbetrag", "rechnungsbetrag", "zahlbetrag", "zu zahlen", "invoice total", "amount payable", "jumlah",
        "جمع کل", "مبلغ قابل پرداخت", "قابل پرداخت", "مبلغ کل",
        "importe total", "total a pagar", "total pagado", "total de la compra", "importe a pagar", "monto total", "monto a pagar",
        "total ttc", "net à payer", "net a payer", "montant à payer", "montant a payer", "total à payer", "total a payer",
        "total à régler", "total a regler", "total réglé", "total regle", "total payé", "total paye", "montant total",
        "total toutes taxes comprises",
        "totale", "totale da pagare", "totale pagato", "totale complessivo", "totale documento", "totale scontrino",
        "importo totale", "importo da pagare", "importo pagato", "netto a pagare", "totale dovuto",
    ];

    private static readonly string[] WeakTotal =
        ["betrag", "gesamt", "جمع", "مبلغ", "importe", "monto", "montant", "à payer", "a payer", "importo", "da pagare"];

    // Phrases that name the total although they contain a word of NotTotal: Italian "TOTALE IVA INCLUSA" is the total
    // with tax ("totale iva" alone is the tax), and "NETTO A PAGARE" is the amount to pay (German "netto" is not).
    // They are taken out of a line only for the NotTotal check.
    private static readonly string[] TotalDespiteExclusion = ["iva inclusa", "iva compresa", "netto a pagare"];

    // Spanish "IVA" and "efectivo" are not listed: "TOTAL IVA INCLUIDO" and "TOTAL EFECTIVO" name the purchase total; a
    // tax-only row has no total word, and cash handed over is "efectivo entregado".
    private static readonly string[] NotTotal =
    [
        "sub", "subtotal", "zwischensumme", "mwst", "ust", "steuer", "netto", "nettobetrag", "tax", "vat", "rabatt", "ersparnis",
        "gesamtersparnis", "discount", "items", "artikel", "bar", "gegeben", "rückgeld", "change", "cash", "tip",
        "تخفیف", "مالیات", "تعداد", "باقیمانده", "دریافتی",
        "sub total", "base imponible", "descuento", "descuentos", "ahorro", "efectivo entregado", "recibido", "cambio", "vuelto",
        "vuelta", "propina",
        "sous total", "sous-total", "total ht", "total hors taxes", "hors taxes", "dont tva", "montant tva", "remise", "remises",
        "réduction", "reduction", "rabais", "escompte", "pourboire", "pourboires", "espèces reçues", "especes recues",
        "montant remis", "monnaie rendue", "rendu monnaie", "à rendre", "a rendre",
        // Italian "contanti" alone is not listed: "TOTALE CONTANTI" is the total paid in cash.
        "subtotale", "sub totale", "imponibile", "totale imponibile", "totale iva", "importo iva", "di cui iva", "sconto", "sconti",
        "sconto totale", "risparmio", "resto", "resto dovuto", "contanti ricevuti", "contanti consegnati", "importo ricevuto", "mancia",
    ];

    private static readonly string[] NotPrice =
    [
        "tel", "telefon", "fax", "plz", "iban", "bic", "nr", "no", "ust", "id", "steuernummer", "تلفن", "کد",
        "teléfono", "telefono", "código postal", "codigo postal", "nif", "cif", "rfc", "ruc", "cuit", "folio", "número de operación",
        "numero de operacion",
        "téléphone", "telephone", "tél", "code postal", "siret", "siren", "numéro de facture", "numero de facture",
        "numéro de ticket", "numero de ticket", "n° facture", "n° ticket",
        "cellulare", "cap", "partita iva", "p iva", "codice fiscale", "numero documento", "numero scontrino", "numero fattura",
        "documento n", "fattura n", "scontrino n",
    ];

    private static readonly string[] NotMerchant =
    [
        "receipt", "rechnung", "beleg", "quittung", "kassenbon", "bon", "invoice", "tel", "fax", "ust", "vat", "فاکتور", "رسید", "تلفن",
        "recibo", "factura", "comprobante", "ticket", "teléfono", "telefono", "nif", "cif", "rfc",
        "reçu", "recu", "facture", "ticket de caisse", "justificatif", "téléphone", "telephone", "tél", "siret", "siren",
        "numéro de facture", "numero de facture",
        "scontrino", "ricevuta", "fattura", "documento commerciale", "documento non fiscale", "partita iva", "p iva", "codice fiscale",
    ];

    /// <summary>Reads a receipt text.</summary>
    /// <param name="text">The recognised text.</param>
    /// <param name="today">The day the receipt is read, to tell solar from lunar Hijri years; default: today.</param>
    public static ReceiptSuggestion Parse(string? text, DateOnly? today = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ReceiptSuggestion(null, null, null);
        }

        // Persian and Arabic separators become Latin ones; OCR sometimes puts a space after the decimal separator ("2, 90").
        // A no-break space between digit groups ("1 234,56" in French) always groups thousands, so it is removed here;
        // a plain space is joined only on a total line (FindTotal), where it cannot glue a quantity to a price.
        var normalized = SplitDecimals().Replace(Digits.ToAscii(text).Replace('٫', '.').Replace('٬', ',').Replace('،', ','), "$1$2$3");
        normalized = NoBreakGroups().Replace(normalized, string.Empty);
        var lines = normalized.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ReceiptSuggestion(FindTotal(lines), FindDate(lines, today ?? DateOnly.FromDateTime(DateTime.Today)), FindMerchant(lines));
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
                // "Total TTC 24,00 (dont TVA 4,00)": the tax part of the line is no reason to drop its total.
                var line = TaxBreakdown().Replace(lines[i], " ");
                var tokens = Tokens(line);
                if (!keywords.Any(k => Has(tokens, k)) || NotTotal.Any(k => Has(WithoutTotalPhrases(tokens), k)))
                {
                    continue;
                }

                var amounts = Amounts(SpaceGroups().Replace(line, string.Empty), strict: false).ToList();
                if (amounts.Count == 0 && i + 1 < lines.Length && !NotTotal.Any(k => Has(Tokens(lines[i + 1]), k)))
                {
                    amounts = [.. Amounts(SpaceGroups().Replace(lines[i + 1], string.Empty), strict: false)];
                }

                if (amounts.Count > 0)
                {
                    return amounts[^1];
                }
            }
        }

        // No total line: the largest price-like amount, never a postal code, phone number or id, and never cash handed
        // over, change, tax or a discount (a receipt with only "cash 100,00 / change 12,00" has no readable total).
        var prices = lines.Where(l => !NotPrice.Any(k => Has(Tokens(l), k)) && !NotTotal.Any(k => Has(Tokens(l), k)))
            .SelectMany(l => Amounts(l, strict: true)).ToList();
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

    // The lower-case words of a line, padded with spaces so that whole words and phrases can be found. Keywords match the
    // Arabic letter forms of PDFs and OCR (ي, ى, ك) and words written with or without a half-space ("باقی‌مانده").
    private static string Tokens(string line) =>
        " " + string.Join(' ', WordPattern().Matches(FoldPersian(line.Normalize(NormalizationForm.FormC).ToLowerInvariant())).Select(m => m.Value)) + " ";

    private static string FoldPersian(string text) =>
        text.Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک').Replace("\u200C", string.Empty, StringComparison.Ordinal);

    private static bool Has(string tokens, string phrase) => tokens.Contains(" " + phrase + " ", StringComparison.Ordinal);

    private static string WithoutTotalPhrases(string tokens) =>
        TotalDespiteExclusion.Aggregate(tokens, (text, phrase) => text.Replace(" " + phrase + " ", " ", StringComparison.Ordinal));

    private static DateOnly? FindDate(string[] lines, DateOnly today)
    {
        foreach (var line in lines)
        {
            foreach (Match match in DatePattern().Matches(line))
            {
                if (TryDate(match, today, out var date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private static bool TryDate(Match match, DateOnly today, out DateOnly date)
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
                // Both Hijri calendars use these years; the receipt is the one nearer to today (it is neither decades old
                // nor decades ahead).
                DateOnly? solar = month is >= 1 and <= 12 && day >= 1 && day <= new PersianCalendar().GetDaysInMonth(year, month)
                    ? DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0))
                    : null;
                DateOnly? lunar = month is >= 1 and <= 12 && day >= 1 && day <= LunarHijri.DaysInMonth(year, month)
                    ? LunarHijri.ToDate(year, month, day)
                    : null;
                if ((solar ?? lunar) is not { } first)
                {
                    return false;
                }

                date = solar is { } s && lunar is { } l
                    ? (Math.Abs(s.DayNumber - today.DayNumber) <= Math.Abs(l.DayNumber - today.DayNumber) ? s : l)
                    : first;
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

    // A no-break or narrow no-break space followed by exactly three digits: a thousands group ("1 234,56").
    [GeneratedRegex(@"(?<=\d)[\u00A0\u202F](?=\d{3}(?!\d))", RegexOptions.CultureInvariant)]
    private static partial Regex NoBreakGroups();

    // A plain space inside "1 234,56" or "12 345": up to three digits, then groups of exactly three.
    [GeneratedRegex(@"(?<=(?<!\d)\d{1,3}(?: \d{3})*) (?=\d{3}(?!\d))", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceGroups();

    // A tax breakdown on a total line: French "(dont TVA 4,00)" or "dont TVA 4,00", Italian "(di cui IVA 4,33)".
    [GeneratedRegex(@"\(?\s*\b(?:dont\s+t\.?v\.?a|di\s+cui\s+iva)\b[^)]*\)?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex TaxBreakdown();

    // Letters (with "\u00B0" for "n\u00B0 facture") of the words a line is matched by.
    [GeneratedRegex(@"[\p{L}\p{M}\u200C\u00B0]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
