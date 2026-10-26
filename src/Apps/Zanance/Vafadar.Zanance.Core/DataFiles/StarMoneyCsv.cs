using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>Explicit text encoding for an original bank-software file, never guessed from currency or UI language.</summary>
public enum BankCsvEncoding
{
    /// <summary>Strict UTF-8, with an optional UTF-8 byte-order mark.</summary>
    Utf8,
    /// <summary>Explicit Windows Western European encoding for legacy exports.</summary>
    Windows1252,
}

/// <summary>A versioned StarMoney Giro cashflow preset from the original export-field specification (D-297).</summary>
public static class StarMoneyCsv
{
    /// <summary>Technical file bound, unrelated to commercial quotas.</summary>
    public const int MaximumBytes = 8 * 1024 * 1024;
    /// <summary>Technical row bound for a bounded complete review, never silent truncation.</summary>
    public const int MaximumRows = 10000;
    /// <summary>Decodes only the explicit encoding; replacement characters and conflicting BOMs are refused.</summary>
    public static string Decode(byte[] bytes, BankCsvEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > MaximumBytes || !Enum.IsDefined(encoding)) throw new FormatException("Bank file bound or encoding is invalid.");
        var utf8Bom = bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 });
        if (bytes.AsSpan().StartsWith(new byte[] { 255, 254 }) || bytes.AsSpan().StartsWith(new byte[] { 254, 255 }) || utf8Bom && encoding != BankCsvEncoding.Utf8)
            throw new FormatException("The selected encoding conflicts with the byte-order mark.");
        var decoder = encoding == BankCsvEncoding.Utf8 ? new UTF8Encoding(false, true)
            : CodePagesEncodingProvider.Instance.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)!;
        var text = decoder.GetString(bytes, utf8Bom ? 3 : 0, bytes.Length - (utf8Bom ? 3 : 0));
        if (text.Any(c => c == '\uFFFD' || char.IsControl(c) && c is not '\r' and not '\n' and not '\t')) throw new FormatException("Bank file contains invalid text.");
        return text;
    }

    /// <summary>Reads the original semicolon contract, retaining every row and refusing unknown or split layouts.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaximumBytes) throw new FormatException("Bank file exceeds its text bound.");
        ValidateSyntax(text);
        var rows = Csv.Read(text, ';');
        if (rows.Count is < 2 or > MaximumRows + 1 || rows.Any(r => r.Count > 128 || r.Any(c => c.Length > 4096))) throw new FormatException("Bank file exceeds its row or field bound.");
        var header = rows[0].Select(c => c.Trim()).ToArray();
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        { "Saldo", "SdoWaehr", "AgBlz", "AgKto", "AgName1", "Storno", "OrigBtg", "Betrag", "BtgWaehr", "OCMTBetr", "OCMTWaehr", "Textschl",
          "BuchDatum", "WertDatum", "Primanota", "Kategorie", "Unterkat", "Kostenst", "BuchText", "Lfd.Nr", "ID", "Steuersatz", "SteuersatzWaehr",
          "Steuerbetrag", "SteuerbetragWaehr", "Fibu-Nr.", "Kommentar", "QkBIC", "QkIBAN" };
        foreach (var i in Enumerable.Range(1, 14)) allowed.Add("VWZ" + i);
        if (header.Any(h => !allowed.Contains(h) && !h.StartsWith("Splittbuchung - ", StringComparison.Ordinal))) throw new FormatException("Unknown bank export fields require a separately reviewed format revision.");
        if (header.Distinct(StringComparer.Ordinal).Count() != header.Length) throw new FormatException("Ambiguous duplicate bank columns.");
        foreach (var name in new[] { "BuchDatum", "Betrag", "BtgWaehr", "AgName1", "Storno", "ID", "QkIBAN" })
            if (!header.Contains(name, StringComparer.Ordinal)) throw new FormatException("Unknown StarMoney Giro export revision.");
        if (!Enumerable.Range(1, 14).All(i => header.Contains("VWZ" + i, StringComparer.Ordinal))) throw new FormatException("Incomplete original remittance columns.");
        if (rows.Skip(1).Any(r => r.Count != header.Length)) throw new FormatException("A bank row has a different column count.");
        var splitColumns = header.Select((name, index) => (name, index)).Where(p => p.name.StartsWith("Splittbuchung", StringComparison.Ordinal) || p.name == "Lfd.Nr").ToArray();
        if (rows.Skip(1).Any(r => splitColumns.Any(p => !string.IsNullOrWhiteSpace(r[p.index]))))
            throw new FormatException("Split exports require their original relationship and cannot be flattened as independent cashflows.");
        return rows;
    }

    /// <summary>Maps only explicitly confirmed posted ordinary cashflows; no transfer, refund or reversal is inferred.</summary>
    public static IReadOnlyList<ImportRow> Preview(IReadOnlyList<IReadOnlyList<string>> rows, Account account,
        IReadOnlyList<Category> categories, IReadOnlyList<LedgerEntry> existing, IReadOnlySet<Guid> consumed)
    {
        var header = rows[0].Select((name, index) => (name: name.Trim(), index)).ToDictionary(p => p.name, p => p.index, StringComparer.Ordinal);
        string Cell(IReadOnlyList<string> row, string name) => header.TryGetValue(name, out var index) ? row[index].Trim() : "";
        var sources = rows.Skip(1).Select(r => Cell(r, "QkIBAN").Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (sources.Length != 1 || sources[0].Length is < 15 or > 34 || sources[0].Any(c => !char.IsAsciiLetterOrDigit(c))) throw new FormatException("Choose an original single-source Giro export.");
        var sourceIds = new HashSet<string>(StringComparer.Ordinal); var known = existing.Select(e => e.Id).Concat(consumed).ToHashSet();
        var duplicates = existing.Where(e => e.AccountId == account.Id).Select(e => (e.Date, e.Amount, e.Kind)).ToHashSet();
        var result = new List<ImportRow>();
        for (var index = 1; index < rows.Count; index++)
        {
            var row = rows[index]; var sourceId = Cell(row, "ID"); string? error = account.Type is AccountType.Cash or AccountType.Checking or AccountType.Savings ? null : "Account";
            if (sourceId.Length is < 1 or > 10 || !long.TryParse(sourceId, NumberStyles.None, CultureInfo.InvariantCulture, out var numericId) || numericId <= 0) error = "Id";
            else { sourceId = numericId.ToString(CultureInfo.InvariantCulture); if (!sourceIds.Add(sourceId)) error = "Id"; }
            if (Cell(row, "Storno").Length != 0) error = "BankRelationship";
            var currency = Cell(row, "BtgWaehr");
            var knownCurrency = Currencies.TryGet(currency, out _);
            if (currency != account.CurrencyCode || !knownCurrency) error = "Currency";
            if (!CsvImport.TryDate(Cell(row, "BuchDatum"), "dd.MM.yyyy", PeriodCalendar.Gregorian, out var date)) error = "Date";
            long signed = 0;
            if (knownCurrency && (!CsvImport.TryAmount(Cell(row, "Betrag"), currency, ',', out signed) || signed == 0)) error = "Amount";
            if (error is not null) { result.Add(new(index + 1, null, error, false, false, false)); continue; }
            // Source ids belong to this software history/account, not to a universal bank transaction namespace.
            var identity = SHA256.HashData(Encoding.UTF8.GetBytes($"zanance/starmoney-giro/1\n{account.Id:D}\n{sources[0]}\n{sourceId}"));
            var kind = signed < 0 ? EntryKind.Expense : EntryKind.Income;
            var entry = new LedgerEntry(new Guid(identity.AsSpan(0, 16)))
            {
                AccountId = account.Id, Date = date, Amount = Math.Abs(signed), Kind = kind,
                Payee = Empty(Cell(row, "AgName1")), Title = Empty(Cell(row, "BuchText")),
                Note = string.Join(Environment.NewLine, Enumerable.Range(1, 14).Select(i => Cell(row, "VWZ" + i)).Where(s => s.Length > 0)
                    .Append("StarMoney Giro / ID " + sourceId).Concat(new[] { "WertDatum", "Kommentar", "Kategorie", "Unterkat", "Kostenst" }
                        .Where(name => Cell(row, name).Length > 0).Select(name => name + ": " + Cell(row, name)))),
                CategoryId = categories.FirstOrDefault(c => c.SystemKey == DefaultCategories.Uncategorized && c.Kind == (signed < 0 ? CategoryKind.Expense : CategoryKind.Income))?.Id,
                Source = EntrySource.Import, Review = ReviewState.Unreviewed,
            };
            var original = Cell(row, "OCMTBetr"); var originalCurrency = Cell(row, "OCMTWaehr");
            if (original.Length != 0 || originalCurrency.Length != 0)
            {
                if (!Currencies.TryGet(originalCurrency, out _) || !CsvImport.TryAmount(original, originalCurrency, ',', out var originalMinor) || originalMinor == 0)
                { result.Add(new(index + 1, null, "Currency", false, false, false)); continue; }
                entry.OriginalAmount = Math.Abs(originalMinor); entry.OriginalCurrencyCode = originalCurrency;
            }
            result.Add(new(index + 1, entry, null, known.Contains(entry.Id), duplicates.Contains((date, entry.Amount, kind)), date < account.OpeningDate));
        }
        return result;
    }
    private static void ValidateSyntax(string text)
    {
        var quoted = false; var closed = false; var start = true; var columns = 1; var rows = 1; var length = 0;
        // D-571: the established field bound counts decoded data, excluding delimiters and CSV quote syntax.
        void CountCharacter()
        { if (++length > 4096) throw new FormatException("A bank field exceeds its bound."); }
        for (var i = text.StartsWith('\uFEFF') ? 1 : 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { CountCharacter(); i++; } else { quoted = false; closed = true; } }
                else CountCharacter();
                continue;
            }
            if (c == '"') { if (!start || closed) throw new FormatException("Malformed bank CSV quotation."); quoted = true; start = false; continue; }
            if (c == ';')
            { if (++columns > 128) throw new FormatException("Too many bank columns."); start = true; closed = false; length = 0; continue; }
            if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                if (++rows > MaximumRows + 2) throw new FormatException("Too many bank records.");
                start = true; closed = false; length = 0; columns = 1; continue;
            }
            if (closed) throw new FormatException("Unexpected content after a bank CSV quote.");
            CountCharacter(); start = false;
        }
        if (quoted) throw new FormatException("Incomplete bank CSV quotation.");
    }
    private static string? Empty(string text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
