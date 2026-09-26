using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.DataFiles;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.DataFiles;

public sealed class CsvTests
{
    private readonly Account _checking = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Account _savings = new() { Name = "Savings", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Category _food = new() { Kind = CategoryKind.Expense, SystemKey = "Food" };
    private readonly Category _uncategorized = new() { Kind = CategoryKind.Expense, SystemKey = DefaultCategories.Uncategorized };

    private Dictionary<Guid, Account> Accounts => new() { [_checking.Id] = _checking, [_savings.Id] = _savings };

    [Fact]
    [Trait("AT", "AT-55")]
    public void Multi_line_quoted_and_formula_like_text_survives_and_is_neutralised()
    {
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 1_250, Date = new DateOnly(2026, 5, 1), Title = "=HYPERLINK(\"x\")", Note = "line 1\nline \"2\", end" };

        var text = CsvExport.Write([entry], Accounts, _ => "Food", includeNotes: true);
        var rows = Csv.Read(text, Csv.DetectSeparator(text));

        Assert.Equal(2, rows.Count);
        Assert.Equal("'=HYPERLINK(\"x\")", rows[1][10]);
        Assert.Equal("line 1\nline \"2\", end", rows[1][12]);
        Assert.Equal("=HYPERLINK(\"x\")", Csv.Unprotect(rows[1][10]));
        Assert.Equal("12.50", rows[1][3]);
    }

    [Fact]
    [Trait("AT", "AT-52")]
    public void The_own_export_is_recognised_and_re_import_is_marked_as_already_imported()
    {
        var expense = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 4_380, Date = new DateOnly(2026, 5, 2), CategoryId = _food.Id };
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = _checking.Id, ToAccountId = _savings.Id, Amount = 10_000, Date = new DateOnly(2026, 5, 3) };
        var text = CsvExport.Write([expense, transfer], Accounts, id => id == _food.Id ? "Food" : string.Empty, includeNotes: false);
        var rows = Csv.Read(text, ',');

        Assert.True(CsvImport.IsOwnFormat(rows[0]));
        var preview = CsvImport.PreviewOwn(rows, [_checking, _savings], [_food], _ => "Food", [expense]);

        Assert.All(preview, r => Assert.Null(r.Error));
        Assert.True(preview[0].AlreadyImported);
        Assert.False(preview[1].AlreadyImported);
        Assert.Equal(expense.Id, preview[0].Entry!.Id);
        Assert.Equal(_food.Id, preview[0].Entry!.CategoryId);
        Assert.Equal(EntryKind.Transfer, preview[1].Entry!.Kind);
        Assert.Equal(_savings.Id, preview[1].Entry!.ToAccountId);
    }

    [Theory]
    [Trait("AT", "AT-51")]
    [InlineData("1.234,56", ',', true, 123_456)]
    [InlineData("1,234.56", '.', true, 123_456)]
    [InlineData("-12,50", ',', true, -1_250)]
    [InlineData("(12.50)", '.', true, -1_250)]
    [InlineData("1,23", '.', false, 0)]
    [InlineData("12.345", '.', false, 0)]
    [InlineData("۱۲٬۵۰۰", '.', true, 1_250_000)]
    public void Amounts_are_parsed_with_the_chosen_separator_and_ambiguity_is_rejected(string text, char separator, bool ok, long expected)
    {
        Assert.Equal(ok, CsvImport.TryAmount(text, "EUR", separator, out var minor));
        Assert.Equal(expected, minor);
    }

    [Theory]
    [Trait("AT", "AT-51")]
    [InlineData("03/04/2026", "dd/MM/yyyy", 2026, 4, 3)]
    [InlineData("03/04/2026", "MM/dd/yyyy", 2026, 3, 4)]
    [InlineData("2026-04-03", "yyyy-MM-dd", 2026, 4, 3)]
    public void Dates_follow_the_chosen_format(string text, string format, int year, int month, int day)
    {
        Assert.True(CsvImport.TryDate(text, format, PeriodCalendar.Gregorian, out var date));
        Assert.Equal(new DateOnly(year, month, day), date);
        Assert.False(CsvImport.TryDate("31/31/2026", format, PeriodCalendar.Gregorian, out _));
    }

    [Fact]
    public void Persian_dates_are_converted()
    {
        Assert.True(CsvImport.TryDate("۱۴۰۵/۰۷/۰۴", "yyyy/MM/dd", PeriodCalendar.Persian, out var date));
        Assert.Equal(new DateOnly(2026, 9, 26), date);
        Assert.False(CsvImport.TryDate("1404/12/30", "yyyy/MM/dd", PeriodCalendar.Persian, out _));
    }

    [Fact]
    [Trait("AT", "AT-53")]
    public void Equal_date_and_amount_is_a_warning_not_a_removal()
    {
        var existing = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 500, Date = new DateOnly(2026, 6, 1) };
        var rows = Csv.Read("date;amount;text\r\n2026-06-01;-5,00;Coffee\r\n2026-06-01;-5,00;Coffee\r\nnot a date;1;x\r\n", ';');
        var mapping = new ImportMapping(_checking.Id, 0, "yyyy-MM-dd", PeriodCalendar.Gregorian, 1, ',', SignMode.NegativeIsExpense, TitleColumn: 2);

        var preview = CsvImport.PreviewGeneric(rows, mapping, [_checking], [_food, _uncategorized], c => c.SystemKey ?? string.Empty, [existing]);

        Assert.Equal(3, preview.Count);
        Assert.True(preview[0].PossibleDuplicate);
        Assert.True(preview[1].PossibleDuplicate);
        Assert.NotNull(preview[0].Entry);
        Assert.NotNull(preview[1].Entry);
        Assert.Equal("Date", preview[2].Error);
        Assert.Equal(EntryKind.Expense, preview[0].Entry!.Kind);
        Assert.Equal(_uncategorized.Id, preview[0].Entry!.CategoryId);
        Assert.Equal(ReviewState.Unreviewed, preview[0].Entry!.Review);
    }
}
