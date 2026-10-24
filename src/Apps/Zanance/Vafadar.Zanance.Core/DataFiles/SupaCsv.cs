using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>Explicit SUPA4 CSV Ntry cashflow review; never a payment order, direct-bank format or inferred relationship.</summary>
public static class SupaCsv
{
    private static readonly CsvReadLimits Limits = new()
    {
        MaximumBytes = StarMoneyCsv.MaximumBytes, MaximumCharacters = StarMoneyCsv.MaximumBytes,
        MaximumRows = StarMoneyCsv.MaximumRows + 1, MaximumColumns = 128,
        MaximumCells = (StarMoneyCsv.MaximumRows + 1) * 128, MaximumFieldCharacters = 4096,
    };
    private static readonly string[] SourceFields = ["AcctId", "OwnrAcctIBAN", "OwnrAcctNo", "OwnrAcctBIC", "OwnrAcctBankCode"];
    private static readonly HashSet<string> Fields = new(StringComparer.Ordinal)
    {
        "Id", "AcctId", "OwnrAcctCcy", "OwnrAcctIBAN", "OwnrAcctNo", "OwnrAcctBIC", "OwnrAcctBankCode", "OwnrVrtlAcctIBAN",
        "BookgDt", "ValDt", "TxDt", "Amt", "AmtCcy", "CdtDbtInd", "InstdAmt", "InstdAmtCcy",
        "TtlChrgsAndTaxAmt", "TtlChrgsAndTaxAmtCcy", "EndToEndId", "MndtId", "CdtrId", "RmtInf", "PurpCd",
        "BookgTxt", "PrimaNotaNo", "BankRef", "BkTxCd", "RmtdNm", "RmtdUltmtNm", "RmtdAcctCtry", "RmtdAcctIBAN",
        "RmtdAcctNo", "RmtdAcctBIC", "RmtdAcctBankCode", "BookgSts", "GVC", "GVCExtension", "RtrInfRsnCd",
        "BtchBookg", "BtchId", "Category", "CategoryDt", "Notes", "ReadStatus", "Flag", "VatAmt",
    };

    /// <summary>Reads all original comma-separated columns under existing complete-review bounds; refuses an unknown contract.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > Limits.MaximumCharacters) throw new CsvReadLimitException(CsvReadLimit.Characters, Limits.MaximumCharacters);
        if (text.Any(c => c == '\uFFFD' || char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            throw new FormatException("Invalid SUPA text.");
        var rows = Csv.Read(text, ',', Limits);
        if (rows.Count < 2) throw new FormatException("A SUPA booking export needs a header and records.");
        var header = rows[0];
        if (header.Any(name => !Fields.Contains(name)) || header.Distinct(StringComparer.Ordinal).Count() != header.Count)
            throw new FormatException("Unknown or ambiguous SUPA columns require explicit mapping.");
        foreach (var name in new[] { "BookgDt", "ValDt", "Amt", "AmtCcy", "CdtDbtInd" })
            if (!header.Contains(name, StringComparer.Ordinal)) throw new FormatException("Not a posted SUPA Ntry contract.");
        if (rows.Skip(1).Any(row => row.Count != header.Count)) throw new FormatException("Incomplete SUPA record.");
        return rows;
    }

    /// <summary>Returns at most the supplied account identifier's last four characters, or null when omitted.</summary>
    public static string? SourceSuffix(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var columns = Columns(rows); var source = Source(rows, columns);
        if (source.Length == 0) return null;
        var value = Cell(rows[1], columns, "OwnrAcctIBAN");
        if (value.Length == 0) value = Cell(rows[1], columns, "OwnrAcctNo");
        if (value.Length == 0) value = Cell(rows[1], columns, "AcctId");
        return value.Length == 0 ? null : value[^Math.Min(4, value.Length)..];
    }

    /// <summary>Maps explicitly confirmed ordinary posted cashflows through the existing reviewed import writer.</summary>
    public static IReadOnlyList<ImportRow> Preview(IReadOnlyList<IReadOnlyList<string>> rows, Account account,
        IReadOnlyList<Category> categories, IReadOnlyList<LedgerEntry> existing, IReadOnlySet<Guid> consumed)
    {
        var columns = Columns(rows); var source = Source(rows, columns);
        var known = existing.Select(item => item.Id).Concat(consumed).ToHashSet();
        var duplicateAmounts = existing.Where(item => item.AccountId == account.Id).Select(item => (item.Date, item.Amount, item.Kind)).ToHashSet();
        var ids = new HashSet<string>(StringComparer.Ordinal); var result = new List<ImportRow>(); var unidentified = new HashSet<int>();
        for (var index = 1; index < rows.Count; index++)
        {
            var row = rows[index]; string Value(string name) => Cell(row, columns, name);
            string? error = account.Type is AccountType.Cash or AccountType.Checking or AccountType.Savings ? null : "Account";
            var id = Value("Id");
            if (id.Length > 35 || id.Length != 0 && !ids.Add(id)) error = "Id";
            var status = Value("BookgSts");
            if (status.Length != 0 && status != "BOOK" || Value("BtchBookg").Length != 0 || Value("BtchId").Length != 0
                || Value("RtrInfRsnCd").Length != 0 || Value("BkTxCd") == "NRTI") error = "BankRelationship";
            var currency = Value("AmtCcy");
            if (currency != account.CurrencyCode || !Currencies.TryGet(currency, out _)
                || Value("OwnrAcctCcy").Length != 0 && Value("OwnrAcctCcy") != currency) error = "Currency";
            if (!DateOnly.TryParseExact(Value("BookgDt"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || !DateOnly.TryParseExact(Value("ValDt"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) error = "Date";
            var direction = Value("CdtDbtInd");
            if (direction is not ("CRDT" or "DBIT")) error = "Amount";
            if (!TryMagnitude(Value("Amt"), currency, out var amount) || amount == 0) error = "Amount";
            long? original = null; string? originalCurrency = null;
            if (Value("InstdAmt").Length != 0 || Value("InstdAmtCcy").Length != 0)
            {
                originalCurrency = Value("InstdAmtCcy");
                if (!TryMagnitude(Value("InstdAmt"), originalCurrency, out var originalMinor) || originalMinor == 0) error = "Currency";
                else original = originalMinor;
            }
            if ((Value("TtlChrgsAndTaxAmt").Length != 0 || Value("TtlChrgsAndTaxAmtCcy").Length != 0)
                && !TryMagnitude(Value("TtlChrgsAndTaxAmt"), Value("TtlChrgsAndTaxAmtCcy"), out _)) error = "Currency";
            if (Value("VatAmt").Length != 0 && !TryMagnitude(Value("VatAmt"), currency, out _)) error = "Amount";
            if (error is not null) { result.Add(new(index + 1, null, error, false, false, false)); continue; }
            // Only an original software id together with an original source account proves repeat identity.
            // Missing optional identity never causes two equal real bookings to be silently skipped (D-516).
            var stable = id.Length != 0 && source.Length != 0;
            var identity = stable ? new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(
                IdentityParts(["zanance/supa-ntry/4", account.Id.ToString("D"), source, id]))).AsSpan(0, 16)) : Guid.NewGuid();
            var kind = direction == "DBIT" ? EntryKind.Expense : EntryKind.Income;
            var entry = new LedgerEntry(identity)
            {
                AccountId = account.Id, Date = date, Amount = amount, Kind = kind,
                Title = Empty(Value("BookgTxt")), Payee = Empty(Value("RmtdNm")),
                OriginalAmount = original, OriginalCurrencyCode = originalCurrency,
                CategoryId = categories.FirstOrDefault(item => item.SystemKey == DefaultCategories.Uncategorized
                    && item.Kind == (kind == EntryKind.Expense ? CategoryKind.Expense : CategoryKind.Income))?.Id,
                // Preserve explicit narrative/counterparty evidence without duplicating structured owner-account identifiers.
                Note = string.Join(Environment.NewLine, new[] { Value("RmtInf"), "SUPA CSV4 / " + (id.Length == 0 ? "no original id" : id) }
                    .Concat(columns.OrderBy(pair => pair.Value).Where(pair => pair.Key != "AcctId" && !pair.Key.StartsWith("Ownr", StringComparison.Ordinal)
                        && pair.Key is not ("Id" or "Amt" or "AmtCcy" or "CdtDbtInd" or "BookgDt" or "BookgTxt" or "RmtdNm" or "RmtInf")
                        && row[pair.Value].Length != 0).Select(pair => pair.Key + ": " + row[pair.Value])).Where(value => value.Length != 0)),
                Source = EntrySource.Import, Review = ReviewState.Unreviewed,
            };
            if (!stable) unidentified.Add(index + 1);
            result.Add(new(index + 1, entry, null, stable && known.Contains(identity), duplicateAmounts.Contains((date, amount, kind)), date < account.OpeningDate));
        }
        var equal = result.Where(item => item.Entry is not null)
            .GroupBy(item => (item.Entry!.Date, item.Entry.Amount, item.Entry.Kind)).Where(group => group.Count() > 1)
            .SelectMany(group => group.Where(item => unidentified.Contains(item.Line)).Select(item => item.Line)).ToHashSet();
        return result.Select(item => equal.Contains(item.Line) ? item with { PossibleDuplicate = true } : item).ToArray();
    }

    private static Dictionary<string, int> Columns(IReadOnlyList<IReadOnlyList<string>> rows) =>
        rows[0].Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index, StringComparer.Ordinal);
    private static string Cell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> columns, string name) =>
        columns.TryGetValue(name, out var index) ? row[index] : "";
    private static string Source(IReadOnlyList<IReadOnlyList<string>> rows, IReadOnlyDictionary<string, int> columns)
    {
        string Identity(IReadOnlyList<string> row) => IdentityParts(SourceFields.Select(name => Cell(row, columns, name)));
        var sources = rows.Skip(1).Select(Identity).Distinct(StringComparer.Ordinal).ToArray();
        if (sources.Length != 1) throw new FormatException("A SUPA file containing different source accounts needs separate review.");
        return new[] { "AcctId", "OwnrAcctIBAN", "OwnrAcctNo" }.Any(name => Cell(rows[1], columns, name).Length != 0) ? sources[0] : "";
    }
    private static bool TryMagnitude(string text, string currency, out long value)
    {
        value = 0;
        return Currencies.TryGet(currency, out _) && text.Length != 0 && text[0] is >= '0' and <= '9'
            && text.Count(character => character == '.') <= 1 && text.All(character => char.IsAsciiDigit(character) || character == '.')
            && text[^1] != '.' && CsvImport.TryAmount(text, currency, '.', out value) && value >= 0;
    }
    // Original text can contain delimiters; length-prefix every part instead of conflating different source ids.
    private static string IdentityParts(IEnumerable<string> values) => string.Concat(values.Select(value =>
        value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value));
    private static string? Empty(string value) => value.Length == 0 ? null : value;
}
