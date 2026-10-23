using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original own-file preview, writer and export retain literal text and refuse unknown text contracts.</summary>
[Trait("AT", "AT-55")]
public sealed class CsvLiteralFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class Files : IDataFileInteraction
    {
        internal PickedDataFile? Selected;
        internal readonly List<string> Exported = [];
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult(Selected);
        public Task<bool> ConfirmExportAsync(string reason) => Task.FromResult(true);
        public Task ShareCsvAsync(string fileName, string title, string content)
        { Exported.Add(content); return Task.CompletedTask; }
    }

    private static ImportExportViewModel Model(FlowFixture fixture, Files files) =>
        new(fixture.Store, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization,
            fixture.Time, fixture.Services.GetRequiredService<HoldingStore>(), files, fixture.Platform);

    private static LedgerEntry Row(Account account) => new()
    {
        AccountId = account.Id, Kind = EntryKind.Expense, Amount = 100, Date = new(2026, 10, 9),
        Title = "'=Original purchase", Payee = "'@Original payee", Note = "  original\nlast line  ",
        ReimbursableAmount = 50, ReimbursedBy = "'-Original employer",
    };

    [Fact]
    public async Task Original_import_and_export_keep_the_literal_account_and_complete_source_text()
    {
        using var fixture = new FlowFixture();
        await fixture.Store.GetSettingsAsync(Ct);
        var original = new Account { Name = "'=Cash", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 10000 };
        var other = new Account { Name = "=Cash", CurrencyCode = "EUR", OpeningDate = original.OpeningDate, OpeningBalance = 20000 };
        await fixture.Store.SaveAccountAsync(original, Ct);
        await fixture.Store.SaveAccountAsync(other, Ct);
        var source = Row(original);
        var files = new Files { Selected = new("fictitious-literal.csv", CsvExport.Write([source],
            new Dictionary<Guid, Account> { [original.Id] = original, [other.Id] = other }, _ => string.Empty, true)) };
        var vm = Model(fixture, files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport);

        await vm.ImportCommand.ExecuteAsync(null);

        var stored = Assert.Single(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(original.Id, stored.AccountId);
        Assert.Equal(source.Title, stored.Title);
        Assert.Equal(source.Payee, stored.Payee);
        Assert.Equal(source.Note, stored.Note);
        Assert.Equal(source.ReimbursedBy, stored.ReimbursedBy);
        Assert.Equal(9900, LedgerCalculator.Balance(original, [stored], source.Date));
        Assert.Equal(20000, LedgerCalculator.Balance(other, [stored], source.Date));
        Assert.False(vm.CanRunImport);
        await vm.ExportCommand.ExecuteAsync(null);
        var rows = Csv.Read(Assert.Single(files.Exported), ',');
        var reread = CsvImport.PreviewOwn(rows, [original, other], [], _ => string.Empty, []).Single().Entry!;
        Assert.Equal(original.Id, reread.AccountId);
        Assert.Equal(source.Title, reread.Title);
        Assert.Equal(source.Note, reread.Note);
        Assert.Equal("3", rows[1][^1]);
        Assert.Empty(fixture.Platform.Failures);
    }

    [Fact]
    public async Task Original_holdings_writer_preserves_literal_identity_quantity_basis_and_notes()
    {
        using var fixture = new FlowFixture();
        await fixture.Store.GetSettingsAsync(Ct);
        var type = new AssetType { Name = "'=Original asset", Kind = AssetKind.Other, Dimension = AssetDimension.Count,
            CountUnitName = "'unit", PriceCurrencyCode = "EUR", Note = "  original type note  " };
        var location = new AssetLocation { Name = "'@Original safe" };
        var opening = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening,
            Date = new(2026, 10, 9), Quantity = 3000, BasisAmount = 3000, Note = "'=Original event note" };
        var files = new Files { Selected = new("fictitious-literal-holdings.csv", HoldingsCsv.Write([type], [location], [opening], [], true)) };
        var vm = Model(fixture, files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport);

        await vm.ImportCommand.ExecuteAsync(null);

        var store = fixture.Services.GetRequiredService<HoldingStore>();
        var savedType = Assert.Single(await store.GetTypesAsync(Ct));
        var savedLocation = Assert.Single(await store.GetLocationsAsync(Ct));
        var savedEvent = Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Equal(type.Name, savedType.Name);
        Assert.Equal(type.CountUnitName, savedType.CountUnitName);
        Assert.Equal(type.Note, savedType.Note);
        Assert.Equal(location.Name, savedLocation.Name);
        Assert.Equal(opening.Quantity, savedEvent.Quantity);
        Assert.Equal(opening.BasisAmount, savedEvent.BasisAmount);
        Assert.Equal(opening.Note, savedEvent.Note);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(fixture.Platform.Failures);
    }

    [Fact]
    public async Task Ambiguous_legacy_account_is_visible_as_invalid_without_an_importable_financial_row()
    {
        using var fixture = new FlowFixture();
        await fixture.Store.GetSettingsAsync(Ct);
        var original = new Account { Name = "'=Cash", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var other = new Account { Name = "=Cash", CurrencyCode = "EUR", OpeningDate = original.OpeningDate };
        await fixture.Store.SaveAccountAsync(original, Ct);
        await fixture.Store.SaveAccountAsync(other, Ct);
        var rows = Csv.Read(CsvExport.Write([Row(original)], new Dictionary<Guid, Account>
            { [original.Id] = original, [other.Id] = other }, _ => string.Empty, true), ',')
            .Select(row => row.ToArray()).ToArray();
        rows[1][5] = "'=Cash";
        rows[1][^1] = "2";
        var files = new Files { Selected = new("fictitious-ambiguous-legacy.csv", Csv.Write(rows)) };
        var vm = Model(fixture, files);
        await vm.LoadAsync();

        await vm.PickCommand.ExecuteAsync(null);

        Assert.False(vm.CanRunImport);
        Assert.Equal(0, vm.ImportCount);
        Assert.Single(vm.InvalidLines);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(fixture.Platform.Failures);
    }

    [Fact]
    public async Task Explicit_generic_interpretation_preserves_literal_title_and_note_through_the_original_writer()
    {
        using var fixture = new FlowFixture();
        await fixture.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "Cash", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await fixture.Store.SaveAccountAsync(account, Ct);
        const string title = "'=Original generic purchase";
        const string note = "  original generic note  ";
        var files = new Files { Selected = new("fictitious-generic.csv", Csv.Write([new[] { "date", "amount", "title", "note" },
            new[] { "2026-10-09", "-1.00", title, note }])) };
        var vm = Model(fixture, files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        vm.CalendarIndex = 0;
        vm.SeparatorIndex = 0;
        vm.DateFormat = "yyyy-MM-dd";
        await vm.PreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport);

        await vm.ImportCommand.ExecuteAsync(null);

        var stored = Assert.Single(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(account.Id, stored.AccountId);
        Assert.Equal(title, stored.Title);
        Assert.Equal(note, stored.Note);
        Assert.Equal(100, stored.Amount);
        Assert.Empty(fixture.Platform.Failures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_text_contract_keeps_the_original_review_and_never_posts_money(bool holdings)
    {
        using var fixture = new FlowFixture();
        await fixture.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "Cash", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await fixture.Store.SaveAccountAsync(account, Ct);
        var csv = CsvExport.Write([Row(account)], new Dictionary<Guid, Account> { [account.Id] = account }, _ => string.Empty, true);
        var files = new Files { Selected = new("original.csv", csv) };
        var vm = Model(fixture, files);
        await vm.LoadAsync();
        await vm.PickCommand.ExecuteAsync(null);
        var preview = vm.PreviewText;
        var unknown = Csv.Read(holdings ? HoldingsCsv.Write([], [new AssetLocation { Name = "Safe" }], [], [], true) : csv, ',')
            .Select(row => row.ToArray()).ToArray();
        unknown[1][^1] = "4";
        files.Selected = new("unknown.csv", Csv.Write(unknown));

        await vm.PickCommand.ExecuteAsync(null);

        Assert.Equal("original.csv", vm.FileName);
        Assert.Equal(preview, vm.PreviewText);
        Assert.True(vm.CanRunImport);
        Assert.IsType<FormatException>(Assert.Single(fixture.Platform.Failures));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await fixture.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
    }
}
