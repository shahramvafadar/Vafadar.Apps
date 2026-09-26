using System.Globalization;
using Vafadar.Core.Text;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.DataFiles;

/// <summary>How the direction of an amount is determined in a generic file (IO-08).</summary>
public enum SignMode
{
    /// <summary>Negative amounts are expenses, positive amounts income.</summary>
    NegativeIsExpense = 0,

    /// <summary>Every row is an expense (amounts are magnitudes).</summary>
    AllExpenses = 1,

    /// <summary>Every row is income.</summary>
    AllIncome = 2,
}

/// <summary>
/// The user's decisions for a generic file (IO-08): nothing ambiguous is guessed. Column indexes are zero-based;
/// <see langword="null"/> means "not in the file".
/// </summary>
public sealed record ImportMapping(
    Guid AccountId,
    int DateColumn,
    string DateFormat,
    PeriodCalendar Calendar,
    int AmountColumn,
    char DecimalSeparator,
    SignMode Sign,
    int? TitleColumn = null,
    int? CategoryColumn = null,
    int? NoteColumn = null);

/// <summary>A row of the preview. Invalid rows are shown with their reason, never dropped silently (IO-09).</summary>
/// <param name="Line">1-based line number in the file.</param>
/// <param name="Entry">The entry to create, or <see langword="null"/> when invalid.</param>
/// <param name="Error">Why the row is invalid (a key such as <c>Date</c>, <c>Amount</c>, <c>Account</c>).</param>
/// <param name="AlreadyImported">The entry id exists already (own format re-import) – skipped (IO-10, AT-52).</param>
/// <param name="PossibleDuplicate">Same account, date and amount exist – only a warning (IO-10, AT-53).</param>
/// <param name="BeforeOpening">Dated before the account's opening date and therefore not in its balance (IO-12).</param>
public sealed record ImportRow(int Line, LedgerEntry? Entry, string? Error, bool AlreadyImported, bool PossibleDuplicate, bool BeforeOpening);

/// <summary>Reads CSV files: the app's own export (with ids, transfers and refunds) or any file with a column mapping.</summary>
public static class CsvImport
{
    private static readonly PersianCalendar Persian = new();

    /// <summary>The date formats offered for generic files (IO-08).</summary>
    public static readonly IReadOnlyList<string> DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd"];

    /// <summary>Returns whether the header row is the app's own export format.</summary>
    public static bool IsOwnFormat(IReadOnlyList<string> header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return header.Count >= CsvExport.Columns.Count && CsvExport.Columns.Select((c, i) => string.Equals(header[i].Trim(), c, StringComparison.OrdinalIgnoreCase)).All(x => x);
    }

    /// <summary>Builds the preview of the app's own export: ids are kept so that importing twice creates nothing twice.</summary>
    public static IReadOnlyList<ImportRow> PreviewOwn(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<Category> categories,
        Func<Category, string> categoryName,
        IReadOnlyCollection<LedgerEntry> existing)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var ids = existing.Select(e => e.Id).ToHashSet();
        var byName = accounts.GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var result = new List<ImportRow>();

        for (var i = 1; i < rows.Count; i++)
        {
            var cells = rows[i];
            string Cell(int index) => index < cells.Count ? Csv.Unprotect(cells[index].Trim()) : string.Empty;
            var line = i + 1;

            if (!Guid.TryParse(Cell(0), out var id))
            {
                result.Add(Invalid(line, "Id"));
                continue;
            }

            if (!DateOnly.TryParseExact(Cell(1), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                result.Add(Invalid(line, "Date"));
                continue;
            }

            if (!Enum.TryParse<EntryKind>(Cell(2), ignoreCase: true, out var kind))
            {
                result.Add(Invalid(line, "Kind"));
                continue;
            }

            if (!byName.TryGetValue(Cell(5), out var account))
            {
                result.Add(Invalid(line, "Account"));
                continue;
            }

            if (!TryAmount(Cell(3), account.CurrencyCode, '.', out var amount) || amount <= 0)
            {
                result.Add(Invalid(line, "Amount"));
                continue;
            }

            var entry = new LedgerEntry(id)
            {
                Kind = kind,
                Date = date,
                AccountId = account.Id,
                Amount = amount,
                Title = Empty(Cell(10)),
                Payee = Empty(Cell(11)),
                Note = Empty(Cell(12)),
                Review = Enum.TryParse<ReviewState>(Cell(15), true, out var review) ? review : ReviewState.Confirmed,
                RefundOfId = Guid.TryParse(Cell(16), out var refundOf) ? refundOf : null,
                GroupId = Guid.TryParse(Cell(17), out var group) ? group : null,
                Source = EntrySource.Import,
            };

            if (kind == EntryKind.Transfer)
            {
                if (!byName.TryGetValue(Cell(6), out var destination))
                {
                    result.Add(Invalid(line, "Account"));
                    continue;
                }

                entry.ToAccountId = destination.Id;
                if (!string.Equals(destination.CurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase)
                    && TryAmount(Cell(7), destination.CurrencyCode, '.', out var toAmount))
                {
                    entry.ToAmount = toAmount;
                }
            }
            else if (Empty(Cell(9)) is { } name)
            {
                var categoryKind = kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense;
                entry.CategoryId = categories.FirstOrDefault(c => c.Kind == categoryKind && string.Equals(categoryName(c), name, StringComparison.OrdinalIgnoreCase))?.Id;
            }

            if (Empty(Cell(14)) is { } originalCurrency && TryAmount(Cell(13), originalCurrency, '.', out var original))
            {
                entry.OriginalAmount = original;
                entry.OriginalCurrencyCode = originalCurrency;
            }

            result.Add(new ImportRow(line, entry, null, ids.Contains(id), false, date < account.OpeningDate));
        }

        return result;
    }

    /// <summary>Builds the preview of a generic file with the user's mapping; the first row is the header.</summary>
    public static IReadOnlyList<ImportRow> PreviewGeneric(
        IReadOnlyList<IReadOnlyList<string>> rows,
        ImportMapping mapping,
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<Category> categories,
        Func<Category, string> categoryName,
        IReadOnlyCollection<LedgerEntry> existing)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(mapping);
        var account = accounts.FirstOrDefault(a => a.Id == mapping.AccountId);
        var existingKeys = existing.Where(e => e.AccountId == mapping.AccountId).Select(e => (e.Date, e.Amount, e.Kind)).ToHashSet();
        var result = new List<ImportRow>();

        for (var i = 1; i < rows.Count; i++)
        {
            var cells = rows[i];
            string Cell(int? index) => index is { } n && n < cells.Count ? Csv.Unprotect(cells[n].Trim()) : string.Empty;
            var line = i + 1;
            if (account is null)
            {
                result.Add(Invalid(line, "Account"));
                continue;
            }

            if (!TryDate(Cell(mapping.DateColumn), mapping.DateFormat, mapping.Calendar, out var date))
            {
                result.Add(Invalid(line, "Date"));
                continue;
            }

            if (!TryAmount(Cell(mapping.AmountColumn), account.CurrencyCode, mapping.DecimalSeparator, out var signed) || signed == 0)
            {
                result.Add(Invalid(line, "Amount"));
                continue;
            }

            var kind = mapping.Sign switch
            {
                SignMode.AllExpenses => EntryKind.Expense,
                SignMode.AllIncome => EntryKind.Income,
                _ => signed < 0 ? EntryKind.Expense : EntryKind.Income,
            };

            var entry = new LedgerEntry
            {
                Kind = kind,
                Date = date,
                AccountId = account.Id,
                Amount = Math.Abs(signed),
                Title = Empty(Cell(mapping.TitleColumn)),
                Note = Empty(Cell(mapping.NoteColumn)),
                Review = ReviewState.Unreviewed,
                Source = EntrySource.Import,
            };

            var categoryKind = kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
            var category = Empty(Cell(mapping.CategoryColumn)) is { } name
                ? categories.FirstOrDefault(c => c.Kind == categoryKind && string.Equals(categoryName(c), name, StringComparison.OrdinalIgnoreCase))
                : null;
            entry.CategoryId = category?.Id ?? categories.FirstOrDefault(c => c.Kind == categoryKind && c.SystemKey == DefaultCategories.Uncategorized)?.Id;

            // Equal date and amount is only a hint: two real purchases can look the same (AT-53).
            result.Add(new ImportRow(line, entry, null, false, existingKeys.Contains((date, entry.Amount, kind)), date < account.OpeningDate));
        }

        return result;
    }

    /// <summary>Parses a date in one of <see cref="DateFormats"/> in the Gregorian or Persian calendar, digits in any script.</summary>
    public static bool TryDate(string text, string format, PeriodCalendar calendar, out DateOnly date)
    {
        date = default;
        var ascii = Digits.ToAscii(text.Trim());
        if (calendar == PeriodCalendar.Gregorian)
        {
            return DateOnly.TryParseExact(ascii, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        // Persian dates: read the numbers in the chosen order, then convert (e.g. 1405/07/04).
        var parts = ascii.Split(['-', '.', '/'], StringSplitOptions.RemoveEmptyEntries);
        var order = format.Split(['-', '.', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || order.Length != 3)
        {
            return false;
        }

        int year = 0, month = 0, day = 0;
        for (var i = 0; i < 3; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            switch (order[i][0])
            {
                case 'y': year = value; break;
                case 'M': month = value; break;
                default: day = value; break;
            }
        }

        if (year is < 1 or > 9377 || month is < 1 or > 12 || day < 1 || day > Persian.GetDaysInMonth(year, month))
        {
            return false;
        }

        date = DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0));
        return true;
    }

    /// <summary>
    /// Parses a signed amount with an explicit decimal separator; the other of '.' and ',' is accepted only as a group
    /// separator in groups of three, so ambiguous values are rejected instead of guessed (AT-51).
    /// </summary>
    public static bool TryAmount(string text, string currencyCode, char decimalSeparator, out long minor)
    {
        minor = 0;
        var ascii = Digits.ToAscii(text.Trim()).Replace(" ", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (ascii.Length == 0)
        {
            return false;
        }

        var negative = ascii[0] is '-' or '−' || (ascii.StartsWith('(') && ascii.EndsWith(')'));
        ascii = ascii.Trim('(', ')', '-', '−', '+');
        var group = decimalSeparator == '.' ? ',' : '.';
        var parts = ascii.Split(decimalSeparator);
        if (parts.Length > 2)
        {
            return false;
        }

        var integer = parts[0];
        if (integer.Contains(group, StringComparison.Ordinal))
        {
            var groups = integer.Split(group);
            if (groups.Skip(1).Any(g => g.Length != 3) || groups[0].Length is 0 or > 3)
            {
                return false;
            }

            integer = string.Concat(groups);
        }

        var normalized = parts.Length == 2 ? $"{integer}.{parts[1]}" : integer;
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        if (decimal.Round(value, currency.MinorDigits) != value)
        {
            return false;
        }

        minor = MoneyAmount.ToMinor(negative ? -value : value, currency);
        return true;
    }

    private static ImportRow Invalid(int line, string error) => new(line, null, error, false, false, false);

    private static string? Empty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
