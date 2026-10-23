using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

public sealed class CsvLiteralTextTests
{
    [Fact]
    [Trait("AT", "AT-55")]
    public void Exported_literal_apostrophe_keeps_the_original_account_when_a_formula_like_alias_exists()
    {
        var original = new Account { Name = "'=Cash", CurrencyCode = "EUR" };
        var other = new Account { Name = "=Cash", CurrencyCode = "EUR" };
        var entry = Entry(original);
        var rows = Csv.Read(CsvExport.Write([entry], new Dictionary<Guid, Account>
            { [original.Id] = original, [other.Id] = other }, _ => string.Empty, true), ',');

        var preview = CsvImport.PreviewOwn(rows, [original, other], [], _ => string.Empty, []);

        Assert.Null(preview.Single().Error);
        Assert.Equal(original.Id, preview.Single().Entry!.AccountId);
    }

    [Theory]
    [InlineData("'=literal")]
    [InlineData("''=literal")]
    [InlineData("'@literal")]
    [InlineData("'-literal")]
    [InlineData("'plain")]
    [InlineData("''plain")]
    [InlineData("  =1+1")]
    [InlineData("\n=1+1")]
    [InlineData("  original\nlast line  ")]
    [InlineData("  ")]
    [Trait("AT", "AT-55")]
    public void Own_export_preserves_literal_text_without_removing_an_original_apostrophe_or_whitespace(string text)
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var entry = Entry(account);
        entry.Title = text;
        entry.Payee = text;
        entry.Note = text;
        entry.ReimbursableAmount = 50;
        entry.ReimbursedBy = text;
        var rows = Csv.Read(CsvExport.Write([entry], new Dictionary<Guid, Account>
            { [account.Id] = account }, _ => string.Empty, true), ',');

        var imported = CsvImport.PreviewOwn(rows, [account], [], _ => string.Empty, []).Single().Entry!;

        Assert.Equal(text, imported.Title);
        Assert.Equal(text, imported.Payee);
        Assert.Equal(text, imported.Note);
        Assert.Equal(text, imported.ReimbursedBy);
    }

    [Theory]
    [InlineData("  original note  ")]
    [InlineData("  ")]
    [Trait("AT", "AT-55")]
    public void Holdings_export_keeps_literal_apostrophes_in_original_names_and_exact_optional_notes(string note)
    {
        var type = new AssetType { Name = "'=Gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500, Note = note };
        var location = new AssetLocation { Name = "'@Safe" };
        var rows = Csv.Read(HoldingsCsv.Write([type], [location], [], [], true), ',');

        var content = HoldingsCsv.Read(rows, [], []);

        Assert.Empty(content.Errors);
        Assert.Equal(type.Name, content.Types.Single().Name);
        Assert.Equal(note, content.Types.Single().Note);
        Assert.Equal(location.Name, content.Locations.Single().Name);
    }

    [Theory]
    [InlineData("'=legacy", "=legacy")]
    [InlineData("''=legacy", "''=legacy")]
    [InlineData("  legacy  ", "legacy")]
    [Trait("AT", "AT-55")]
    public void Version_two_rows_keep_their_original_protection_meaning(string encoded, string expected)
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var rows = Csv.Read(CsvExport.Write([Entry(account)], new Dictionary<Guid, Account>
            { [account.Id] = account }, _ => string.Empty, true), ',').Select(row => row.ToArray()).ToArray();
        rows[1][10] = encoded;
        rows[1][^1] = "2";

        var imported = CsvImport.PreviewOwn(rows, [account], [], _ => string.Empty, []).Single().Entry!;

        Assert.Equal(expected, imported.Title);
    }

    [Theory]
    [InlineData("'=legacy", "=legacy")]
    [InlineData("''=legacy", "''=legacy")]
    [InlineData("  legacy  ", "legacy")]
    [Trait("AT", "AT-55")]
    public void Version_two_holdings_names_keep_their_original_protection_meaning(string encoded, string expected)
    {
        var location = new AssetLocation { Name = "Safe" };
        var rows = Csv.Read(HoldingsCsv.Write([], [location], [], [], true), ',').Select(row => row.ToArray()).ToArray();
        rows[1][3] = encoded;
        rows[1][^1] = "2";

        var content = HoldingsCsv.Read(rows, [], []);

        Assert.Empty(content.Errors);
        Assert.Equal(expected, content.Locations.Single().Name);
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void A_transfer_keeps_the_literal_destination_instead_of_a_different_formula_like_account()
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var destination = new Account { Name = "'+Savings", CurrencyCode = "EUR" };
        var other = new Account { Name = "+Savings", CurrencyCode = "EUR" };
        var entry = Entry(account);
        entry.Kind = EntryKind.Transfer;
        entry.ToAccountId = destination.Id;
        var rows = Csv.Read(CsvExport.Write([entry], new Dictionary<Guid, Account>
            { [account.Id] = account, [destination.Id] = destination, [other.Id] = other }, _ => string.Empty, true), ',');

        var imported = CsvImport.PreviewOwn(rows, [account, destination, other], [], _ => string.Empty, []).Single().Entry!;

        Assert.Equal(account.Id, imported.AccountId);
        Assert.Equal(destination.Id, imported.ToAccountId);
        Assert.Equal(entry.Amount, imported.Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AT", "AT-55")]
    public void Legacy_literal_account_identity_is_refused_when_both_protected_and_literal_names_exist(bool destination)
    {
        var cash = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var literal = new Account { Name = "'=Account", CurrencyCode = "EUR" };
        var decoded = new Account { Name = "=Account", CurrencyCode = "EUR" };
        var entry = Entry(destination ? cash : literal);
        if (destination) { entry.Kind = EntryKind.Transfer; entry.ToAccountId = literal.Id; }
        var accounts = new Dictionary<Guid, Account> { [cash.Id] = cash, [literal.Id] = literal, [decoded.Id] = decoded };
        var rows = Csv.Read(CsvExport.Write([entry], accounts, _ => string.Empty, true), ',')
            .Select(row => row.ToArray()).ToArray();
        rows[1][destination ? 6 : 5] = "'=Account";
        rows[1][^1] = "2";

        var preview = CsvImport.PreviewOwn(rows, [cash, literal, decoded], [], _ => string.Empty, []);

        Assert.Equal("Account", preview.Single().Error);
        Assert.Null(preview.Single().Entry);
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void An_unknown_entries_text_version_is_refused_before_account_meaning_is_guessed()
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var rows = Csv.Read(CsvExport.Write([Entry(account)], new Dictionary<Guid, Account>
            { [account.Id] = account }, _ => string.Empty, true), ',').Select(row => row.ToArray()).ToArray();
        rows[1][^1] = "4";

        Assert.Throws<FormatException>(() => CsvImport.PreviewOwn(rows, [account], [], _ => string.Empty, []));
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void An_unknown_holdings_text_version_is_refused_instead_of_silently_decoding_its_names()
    {
        var rows = Csv.Read(HoldingsCsv.Write([], [new AssetLocation { Name = "Safe" }], [], [], true), ',')
            .Select(row => row.ToArray()).ToArray();
        rows[1][^1] = "4";

        Assert.Throws<FormatException>(() => HoldingsCsv.Read(rows, [], []));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("AT", "AT-55")]
    public void A_shifted_or_duplicated_version_marker_cannot_hide_an_unknown_contract(bool holdings, bool duplicate)
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var text = holdings ? HoldingsCsv.Write([], [new AssetLocation { Name = "Safe" }], [], [], true)
            : CsvExport.Write([Entry(account)], new Dictionary<Guid, Account> { [account.Id] = account }, _ => string.Empty, true);
        var rows = Csv.Read(text, ',').Select(row => row.ToList()).ToArray();
        if (duplicate)
        {
            rows[0].Add("format_version");
            rows[1].Add("2");
        }
        else
        {
            rows[0].Insert(rows[0].Count - 1, "future_column");
            rows[1].Insert(rows[1].Count - 1, string.Empty);
            rows[1][^1] = "4";
        }

        Assert.Throws<FormatException>(() =>
        {
            if (holdings) HoldingsCsv.Read(rows, [], []);
            else CsvImport.PreviewOwn(rows, [account], [], _ => string.Empty, []);
        });
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void A_named_version_marker_after_an_optional_column_preserves_literal_account_identity()
    {
        var account = new Account { Name = "'=Cash", CurrencyCode = "EUR" };
        var other = new Account { Name = "=Cash", CurrencyCode = "EUR" };
        var rows = Csv.Read(CsvExport.Write([Entry(account)], new Dictionary<Guid, Account>
            { [account.Id] = account, [other.Id] = other }, _ => string.Empty, true), ',').Select(row => row.ToList()).ToArray();
        rows[0].Insert(rows[0].Count - 1, "optional_column");
        rows[1].Insert(rows[1].Count - 1, string.Empty);

        var imported = CsvImport.PreviewOwn(rows, [account, other], [], _ => string.Empty, []).Single().Entry!;

        Assert.Equal(account.Id, imported.AccountId);
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void A_versionless_original_header_keeps_the_legacy_protected_account_contract()
    {
        var account = new Account { Name = "=Cash", CurrencyCode = "EUR" };
        var row = Entry(account);
        row.Title = "=Original legacy title";
        var rows = Csv.Read(CsvExport.Write([row], new Dictionary<Guid, Account>
            { [account.Id] = account }, _ => string.Empty, true), ',')
            .Select(cells => (IReadOnlyList<string>)cells.Take(CsvExport.BaseColumns.Count).ToArray()).ToArray();

        var imported = CsvImport.PreviewOwn(rows, [account], [], _ => string.Empty, []).Single().Entry!;

        Assert.Equal(account.Id, imported.AccountId);
        Assert.Equal(row.Title, imported.Title);
        Assert.Equal(row.Amount, imported.Amount);
    }

    [Theory]
    [InlineData("'=literal")]
    [InlineData("''=literal")]
    [InlineData("  original\nlast line  ")]
    [Trait("AT", "AT-55")]
    public void Generic_mapping_keeps_literal_source_text_without_assuming_our_export_protection(string text)
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var rows = Csv.Read(Csv.Write([new[] { "date", "amount", "title", "note" },
            new[] { "2026-10-23", "-1.00", text, text }]), ',');
        var mapping = new ImportMapping(account.Id, 0, "yyyy-MM-dd", PeriodCalendar.Gregorian,
            1, '.', SignMode.NegativeIsExpense, TitleColumn: 2, NoteColumn: 3);

        var entry = CsvImport.PreviewGeneric(rows, mapping, [account], [], _ => string.Empty, [], []).Single().Entry!;

        Assert.Equal(text, entry.Title);
        Assert.Equal(text, entry.Note);
        Assert.Equal(100, entry.Amount);
    }

    [Fact]
    [Trait("AT", "AT-55")]
    public void Generic_mapping_preserves_literal_category_identity_instead_of_its_formula_like_alias()
    {
        var account = new Account { Name = "Cash", CurrencyCode = "EUR" };
        var original = new Category { Kind = CategoryKind.Expense, SystemKey = "Food" };
        var other = new Category { Kind = CategoryKind.Expense, SystemKey = "Other" };
        var rows = Csv.Read("date,amount,category\r\n2026-10-23,-1.00,'=Category\r\n", ',');
        var mapping = new ImportMapping(account.Id, 0, "yyyy-MM-dd", PeriodCalendar.Gregorian,
            1, '.', SignMode.NegativeIsExpense, CategoryColumn: 2);

        var entry = CsvImport.PreviewGeneric(rows, mapping, [account], [original, other],
            category => category.Id == original.Id ? "'=Category" : "=Category", [], []).Single().Entry!;

        Assert.Equal(original.Id, entry.CategoryId);
    }

    private static LedgerEntry Entry(Account account) => new()
    {
        AccountId = account.Id, Kind = EntryKind.Expense, Amount = 100, Date = new DateOnly(2026, 10, 23),
    };
}
