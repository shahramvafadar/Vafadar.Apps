using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vafadar.Core.Dates;
using Vafadar.Core.Text;

namespace Vafadar.Zanance.Core.Receipts;

/// <summary>Values read from a receipt. Every value is a suggestion the user reviews before anything is saved (D-31).</summary>
/// <param name="Amount">A found purchase total in the receipt's unit; null when review is required (not yet minor units).</param>
/// <param name="Date">The purchase date.</param>
/// <param name="Merchant">The shop or company, usually the first line.</param>
public sealed record ReceiptSuggestion(decimal? Amount, DateOnly? Date, string? Merchant)
{
    /// <summary>Gets whether the numeric evidence is usable or needs review.</summary>
    public ReceiptAmountStatus AmountStatus { get; init; }

    /// <summary>Gets at most four complete total candidates, with their original rows and units.</summary>
    public IReadOnlyList<ReceiptAmountCandidate> Candidates { get; init; } = [];

    /// <summary>Gets the relevant rows when a damaged number cannot form a candidate.</summary>
    public string? AmountSource { get; init; }

    /// <summary>Gets a value indicating whether nothing usable was found.</summary>
    public bool IsEmpty => Amount is null && Date is null && Merchant is null && AmountStatus == ReceiptAmountStatus.NotFound;
}

/// <summary>
/// Reads the total, the date and the merchant from the text of a receipt (on-device OCR, D-31). It prefers a line with a
/// total keyword (Total, Summe, Gesamt, Totale, جمع, مبلغ قابل پرداخت …). Item prices never substitute for a total. Persian and
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
    private static readonly string[] TotalDespiteExclusion = ["iva inclusa", "iva compresa", "iva incluido", "iva incluida", "tva comprise", "tva incluse", "netto a pagare"];

    // Inclusive-tax phrases name the gross total; "TOTAL IVA/TVA" alone names tax. Cash handed over is
    // "efectivo entregado", not the purchase-total phrase "TOTAL EFECTIVO".
    private static readonly string[] NotTotal =
    [
        "sub", "subtotal", "zwischensumme", "mwst", "ust", "steuer", "netto", "nettobetrag", "tax", "vat", "rabatt", "ersparnis",
        "gesamtersparnis", "discount", "items", "artikel", "bar", "gegeben", "rückgeld", "change", "cash", "tip",
        "تخفیف", "مالیات", "تعداد", "باقیمانده", "دریافتی",
        "sub total", "base imponible", "total iva", "total tva", "descuento", "descuentos", "ahorro", "efectivo entregado", "recibido", "cambio", "vuelto",
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
    /// <param name="uncertainLayout">Whether the document reader found an ambiguous relationship between columns.</param>
    public static ReceiptSuggestion Parse(string? text, DateOnly? today = null, bool uncertainLayout = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ReceiptSuggestion(null, null, null);
        }

        // Persian and Arabic separators become Latin ones; OCR sometimes puts a space after the decimal separator ("2, 90").
        // A no-break space between digit groups ("1 234,56" in French) always groups thousands, so it is removed here;
        // a plain space is joined only on a total line (FindTotal), where it cannot glue a quantity to a price.
        var normalized = SplitDecimals().Replace(FoldPersian(Digits.ToAscii(text)).Replace('٫', '.').Replace('٬', ',').Replace('،', ','), "$1$2$3");
        normalized = NoBreakGroups().Replace(normalized, string.Empty);
        // Keep page boundaries as an unmatchable row so neighbouring-page totals cannot borrow an amount.
        var lines = normalized.Replace("\f", "\n[page boundary]\n", StringComparison.Ordinal)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var evidence = FindTotal(lines, uncertainLayout);
        return new ReceiptSuggestion(evidence.Amount, FindDate(lines, today ?? DateOnly.FromDateTime(DateTime.Today)), FindMerchant(lines))
        {
            AmountStatus = evidence.AmountStatus,
            Candidates = evidence.Candidates,
            AmountSource = evidence.AmountSource,
        };
    }

    /// <summary>Reads an amount like <c>1.234,56</c>, <c>1,234.56</c>, <c>12,50</c> or <c>125,000</c>.</summary>
    public static bool TryAmount(string text, out decimal amount)
    {
        amount = 0;
        var value = text.Trim();
        if (value.Length == 0 || !CompleteNumber().IsMatch(value))
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

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount >= 0;
    }

    private static ReceiptSuggestion FindTotal(string[] lines, bool uncertainLayout)
    {
        var sources = new List<string>();
        var paymentSources = new List<string>();
        foreach (var keywords in new[] { StrongTotal, WeakTotal })
        {
            var candidates = new List<ReceiptAmountCandidate>();
            var damaged = uncertainLayout;
            for (var i = 0; i < lines.Length; i++)
            {
                // Remove bounded tax annotations, not everything after "inkl.": the gross amount may follow it.
                var line = InclusiveTax().Replace(TaxBreakdown().Replace(lines[i], " "), " ");
                var tokens = Tokens(line);
                if (!keywords.Any(k => Has(tokens, k)) || NotTotal.Any(k => Has(WithoutTotalPhrases(tokens), k))
                    || NonMoney.Any(k => Has(tokens, k)) || UnitPriceLine().IsMatch(line))
                {
                    continue;
                }

                if (PaymentOnly.Any(k => Has(tokens, k)))
                {
                    paymentSources.Add(lines[i]);
                    continue;
                }

                sources.Add(lines[i]);
                var amountLine = line;
                var candidateSource = lines[i];
                // Only a price/unit-only next row is evidence for a detached total. A document number, another label
                // or the next page must never become the total of the preceding row.
                if (!line.Any(char.IsDigit) && i + 1 < lines.Length && PriceOnly().IsMatch(lines[i + 1])
                    && !NotPrice.Concat(NotTotal).Concat(NonMoney).Any(k => Has(Tokens(lines[i + 1]), k)))
                {
                    amountLine = lines[i + 1];
                    candidateSource += "\n" + amountLine;
                    sources.Add(amountLine);
                }

                var identifier = IdentifierTail().Match(amountLine);
                if (identifier.Success)
                {
                    amountLine = amountLine[..identifier.Index];
                }

                amountLine = SpaceGroups().Replace(amountLine, string.Empty);
                amountLine = TimePattern().Replace(DatePattern().Replace(amountLine, m => new string(' ', m.Length)), m => new string(' ', m.Length));
                var matches = AmountPattern().Matches(amountLine);
                var unitMatches = CurrencyUnits().Matches(amountLine).Cast<Match>()
                    .Where(u => Vafadar.Zanance.Core.Money.Currencies.TryGet(u.Value.ToUpperInvariant(), out _)
                        || u.Value is "€" or "£" or "$" or "ریال" or "تومان").ToArray();
                damaged |= unitMatches.Select(u => Unit(u.Value)).Distinct().Count() > 1;
                var prefixUnits = unitMatches.Length > 0 && matches.Count > 0 && unitMatches[0].Index < matches[0].Index;
                // Reject the complete damaged token. A later valid number must not hide an OCR letter in the total.
                damaged |= NumericToken().Matches(amountLine).Cast<Match>()
                    .Any(m => !CompleteNumber().IsMatch(m.Value) && !m.Value.All(char.IsDigit));
                var valid = new List<ReceiptAmountCandidate>();
                foreach (Match match in matches)
                {
                    var after = amountLine[(match.Index + match.Length)..].TrimStart();
                    var before = amountLine[..match.Index].TrimEnd();
                    if (before.EndsWith('-') || before.EndsWith('−') || after.StartsWith('-') || after.StartsWith('−')
                        || before.EndsWith('(') && after.StartsWith(')'))
                    {
                        damaged = true;
                        continue;
                    }

                    if (after.StartsWith('%') || MeasureSuffix().IsMatch(after))
                    {
                        continue;
                    }

                    if (!TryAmount(match.Value, out var amount) || amount >= 100_000_000_000m)
                    {
                        damaged = true;
                        continue;
                    }

                    var unitMatch = unitMatches.Where(u => u.Index >= match.Index + match.Length
                        ? string.IsNullOrWhiteSpace(amountLine[(match.Index + match.Length)..u.Index])
                        : u.Index + u.Length <= match.Index && string.IsNullOrWhiteSpace(amountLine[(u.Index + u.Length)..match.Index]))
                        .OrderBy(u => prefixUnits ? u.Index > match.Index : u.Index < match.Index)
                        .ThenBy(u => u.Index >= match.Index + match.Length ? u.Index - match.Index - match.Length : match.Index - u.Index - u.Length)
                        .FirstOrDefault();
                    unitMatch ??= unitMatches.Length == 1 ? unitMatches[0] : null;
                    // More than one printed unit beside a single number has no unambiguous association.
                    var unit = unitMatch is not null ? Unit(unitMatch.Value)
                        : (Code: (string?)null, Factor: 1, Label: unitMatches.Length > 1 ? "?" : (string?)null);
                    valid.Add(new ReceiptAmountCandidate(amount, candidateSource, unit.Code, unit.Factor, unit.Label));
                    if (Vafadar.Zanance.Core.Money.Currencies.TryGet(unit.Code, out var currency) && currency.MinorDigits == 3
                        && match.Value.Count(c => c is '.' or ',') == 1 && match.Value.Length - Math.Max(match.Value.LastIndexOf('.'), match.Value.LastIndexOf(',')) - 1 == 3)
                    {
                        // Without a receipt locale, 12.345 KWD may mean 12.345 or 12,345. Keep both interpretations for
                        // review rather than turning a legitimate three-decimal currency into a confident thousand.
                        valid.Add(new ReceiptAmountCandidate(amount / 1000, candidateSource, unit.Code, unit.Factor, unit.Label));
                        damaged = true;
                    }

                    damaged |= unit.Label is not null && unit.Code is null;
                }

                // A malformed OCR token is not permission to salvage a trailing fragment, nor to read the next row.
                damaged |= valid.Count == 0;
                candidates.AddRange(valid);
            }

            if (sources.Count == 0)
            {
                continue;
            }

            var distinct = candidates.DistinctBy(c => (c.Amount, c.CurrencyCode, c.MajorUnitFactor, c.UnitLabel)).ToArray();
            var found = distinct.Length == 1 && !damaged;
            return new ReceiptSuggestion(found ? distinct[0].Amount : null, null, null)
            {
                AmountStatus = found ? ReceiptAmountStatus.Found : ReceiptAmountStatus.Review,
                Candidates = distinct.Take(4).ToArray(),
                AmountSource = string.Join('\n', sources.Distinct().Take(4)),
            };
        }

        return new ReceiptSuggestion(null, null, null)
        {
            AmountStatus = paymentSources.Count > 0 ? ReceiptAmountStatus.Review : ReceiptAmountStatus.NotFound,
            AmountSource = paymentSources.Count > 0 ? string.Join('\n', paymentSources.Distinct().Take(4)) : null,
        };
    }

    private static (string? Code, int Factor, string? Label) Unit(string value)
    {
        return value switch
        {
            "€" => ("EUR", 1, "EUR"),
            "£" => ("GBP", 1, "GBP"),
            "$" => (null, 1, "$"),
            "ریال" => ("IRR", 1, "ریال"),
            "تومان" => ("IRR", 10, "تومان"),
            _ => (value.ToUpperInvariant(), 1, value.ToUpperInvariant()),
        };
    }

    private static readonly string[] NonMoney = ["points", "loyalty", "punkte", "puntos", "punti", "امتیاز", "liters", "litres", "liter", "litri", "لیتر", "kg", "weight", "وزن", "quantity", "qty", "unit price", "einzelpreis", "stückpreis", "prix unitaire", "precio unitario", "prezzo unitario"];

    private static readonly string[] PaymentOnly = ["balance due", "remaining", "deposit", "prepayment", "paid", "pagado", "pagato", "payé", "paye", "réglé", "regle", "مانده", "پیش پرداخت"];

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
            return l != "[page boundary]" && l.Count(char.IsLetter) >= 3
                && !NotMerchant.Any(k => Has(tokens, k))
                && !StrongTotal.Concat(WeakTotal).Any(k => Has(tokens, k))
                && !l.Contains("www.", StringComparison.OrdinalIgnoreCase) && !l.Contains('@');
        })?.Trim();

    [GeneratedRegex(@"(?<![\p{L}\p{N}.,+−-])[+−-]?\d+(?:[.,]\d+)*[−-]?(?![\p{L}\p{N}.,])", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"^(?:\d+|\d+[.,]\d{1,2}|\d{1,3}(?:[.,]\d{3})+(?:[.,]\d{1,2})?)$", RegexOptions.CultureInvariant)]
    private static partial Regex CompleteNumber();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[+−-]?\d[\p{L}\p{N}.,]*[−-]?", RegexOptions.CultureInvariant)]
    private static partial Regex NumericToken();

    [GeneratedRegex(@"^(?:[A-Z]{3}|[€£$]|ریال|تومان)?\s*\d+(?:[.,\s]\d+)*\s*(?:[A-Z]{3}|[€£$]|ریال|تومان)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex PriceOnly();

    [GeneratedRegex(@"(?:\b[A-Z]{3}\b|[€£$]|ریال|تومان)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex CurrencyUnits();

    [GeneratedRegex(@"^(?:[A-Z]{3}\s*)?\s*/\s*(?:l|kg|g|liter|litre)\b|^(?:l|kg|g|ml|grams?|liters?|litres?|litri)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex MeasureSuffix();

    [GeneratedRegex(@"/\s*(?:l|kg|g|liter|litre)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex UnitPriceLine();

    [GeneratedRegex(@"\b(?:beleg\s*(?:nr|nummer)|receipt\s*#|nr\.?|no\.?|id|tel(?:efon|ephone)?|fax|folio|nif|siret|numero\s+(?:documento|scontrino|fattura)|numéro\s+de\s+(?:facture|ticket))\s*[:.#]?\s*[\p{L}\d]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex IdentifierTail();

    [GeneratedRegex(@"\b\d{1,4}[./-]\d{1,2}[./-]\d{2,4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\b", RegexOptions.CultureInvariant)]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"(\d)[ \u00A0\u202F]*([.,])[ \u00A0\u202F]*(\d{1,2})(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex SplitDecimals();

    // A no-break or narrow no-break space followed by exactly three digits: a thousands group ("1 234,56").
    [GeneratedRegex(@"(?<=\d)[\u00A0\u202F](?=\d{3}(?!\d))", RegexOptions.CultureInvariant)]
    private static partial Regex NoBreakGroups();

    // A plain space inside "1 234,56" or "12 345": up to three digits, then groups of exactly three.
    [GeneratedRegex(@"(?<=(?<!\d)\d{1,3}(?: \d{3})*) (?=\d{3}(?!\d))", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceGroups();

    // A tax breakdown on a total line: French "(dont TVA 4,00)" or "dont TVA 4,00", Italian "(di cui IVA 4,33)".
    [GeneratedRegex(@"\([^)]*\b(?:mwst|ust|tax|vat|tva|iva)\b[^)]*\)|\b(?:dont\s+t\.?v\.?a|di\s+cui\s+iva)\s+\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex TaxBreakdown();

    [GeneratedRegex(@"\b(?:inkl\.?|inklusive|including)\s*(?:\d+(?:[.,]\d+)?\s*%\s*)?(?:mwst|ust|tax|vat)\.?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex InclusiveTax();

    // Letters (with "\u00B0" for "n\u00B0 facture") of the words a line is matched by.
    [GeneratedRegex(@"[\p{L}\p{M}\u200C\u00B0]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
