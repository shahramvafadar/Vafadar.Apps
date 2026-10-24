using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Testing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-516: original SUPA booking semantics through the existing reviewed writer and durable Undo.</summary>
[Trait("AT", "AT-516")]
[Collection("Entry validation units")]
public sealed class SupaCsvFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Files : IDataFileInteraction
    {
        internal string Text = Fixture();
        internal BankCsvEncoding? Encoding;
        internal bool Cancel;
        internal Func<Task>? DuringPick;
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult<PickedDataFile?>(null);
        public async Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding)
        { Encoding = encoding; if (DuringPick is not null) await DuringPick(); return Cancel ? null : new("fictitious-supa4.csv", Text); }
        public Task<bool> ConfirmExportAsync(string reason) => throw new InvalidOperationException("Import must not export.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new InvalidOperationException("Import must not share.");
    }
    internal static string Fixture(int count = 3, string? field = null, string? value = null, bool identity = true)
    {
        string[] header = [.. (identity ? new[] { "Id", "AcctId" } : []), "BookgDt", "ValDt", "Amt", "AmtCcy", "CdtDbtInd",
            "BookgSts", "BookgTxt", "RmtdNm", "RmtInf", "Notes", "InstdAmt", "InstdAmtCcy", "TtlChrgsAndTaxAmt",
            "TtlChrgsAndTaxAmtCcy", "VatAmt", "BtchBookg", "BtchId", "RtrInfRsnCd", "BkTxCd"];
        var rows = new List<IReadOnlyList<string>> { header };
        for (var index = 0; index < count; index++)
        {
            var row = header.ToDictionary(name => name, _ => "");
            if (identity) { row["Id"] = "original-" + index; row["AcctId"] = "fictitious-source-0516"; }
            row["BookgDt"] = "2026-10-24"; row["ValDt"] = "2026-10-25"; row["Amt"] = index == 0 ? "100.00" : "12.34";
            row["AmtCcy"] = "EUR"; row["CdtDbtInd"] = index == 0 ? "CRDT" : "DBIT"; row["BookgSts"] = "BOOK";
            row["BookgTxt"] = "Original fictitious cashflow " + index; row["RmtdNm"] = "Fictitious Müller / نمونه";
            row["RmtInf"] = "Original, quoted \"reference\""; row["Notes"] = "First retained line\r\nSecond retained line";
            if (field is not null && index == count - 1) row[field] = value ?? "";
            rows.Add(header.Select(name => row[name]).ToArray());
        }
        return Csv.Write(rows);
    }
    private static async Task<ImportExportViewModel> ModelAsync(FlowFixture fixture, Files files, bool prepare = true)
    {
        if (prepare)
        {
            await fixture.Store.EnsureDefaultCategoriesAsync(Ct);
            await fixture.Store.SaveAccountAsync(new() { Name = "Original SUPA target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        }
        var model = new ImportExportViewModel(fixture.Store, fixture.Translator, new DateFormatter(fixture.Localization),
            fixture.Localization, fixture.Time, fixture.Services.GetRequiredService<HoldingStore>(), files, fixture.Platform);
        await model.LoadAsync(); return model;
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Original_utf8_booking_review_cancel_single_import_reimport_and_durable_undo_preserve_source_accounts(string language)
    {
        using var fixture = new FlowFixture();
        fixture.Localization.SetLanguage(fixture.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var files = new Files(); var model = await ModelAsync(fixture, files); model.BankEncodingIndex = 1;
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await model.PickSupaCommand.ExecuteAsync(null);
        Assert.Equal(BankCsvEncoding.Utf8, files.Encoding); Assert.True(model.IsBankPreset); Assert.False(model.ShowMapping);
        Assert.Equal(3, model.ImportCount); Assert.Equal(3, model.BankLines.Count); Assert.Contains("0516", model.BankSourceText!);
        Assert.Equal(fixture.Translator["SupaCsv_Warning"], model.BankPresetWarning);
        Assert.False(model.CanRunImport); model.BankAcknowledged = true; Assert.True(model.CanRunImport);
        fixture.Platform.Confirmations.Enqueue(false); await model.ImportCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture)); Assert.True(model.CanRunImport);
        fixture.Platform.Confirmations.Enqueue(true); await model.ImportCommand.ExecuteAsync(null);
        Assert.NotNull(model.ImportResult); Assert.Null(model.ImportError); Assert.False(model.CanRunImport);
        var entries = await fixture.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(3, entries.Count);
        Assert.Equal(10000, Assert.Single(entries, item => item.Kind == EntryKind.Income).Amount);
        Assert.Equal(2, entries.Count(item => item.Kind == EntryKind.Expense));
        Assert.All(entries.Where(item => item.Kind == EntryKind.Expense), item => Assert.Equal(1234, item.Amount));
        Assert.All(entries, item =>
        {
            Assert.Equal(ReviewState.Unreviewed, item.Review); Assert.Equal(EntrySource.Import, item.Source);
            Assert.Contains("Original, quoted \"reference\"", item.Note!); Assert.Contains("Second retained line", item.Note!);
            Assert.Contains("ValDt: 2026-10-25", item.Note!); Assert.DoesNotContain("fictitious-source-0516", item.Note!);
        });
        await model.PickSupaCommand.ExecuteAsync(null); Assert.Equal(0, model.ImportCount); Assert.False(model.CanRunImport);
        var reopened = await ModelAsync(fixture, files, prepare: false);
        fixture.Platform.Confirmations.Enqueue(true); await reopened.UndoCommand.ExecuteAsync(Assert.Single(reopened.Batches));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        FinancialSnapshotAssert.EqualAfterWrites(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Theory]
    [InlineData("BookgSts", "PDNG")][InlineData("BookgSts", "INFO")][InlineData("BookgSts", "OTHER")]
    [InlineData("BtchBookg", "true")][InlineData("BtchBookg", "false")][InlineData("BtchId", "original-parent")]
    [InlineData("RtrInfRsnCd", "AC01")][InlineData("BkTxCd", "NRTI")]
    [InlineData("AmtCcy", "USD")][InlineData("Amt", "-12.34")][InlineData("Amt", "+12.34")]
    [InlineData("Amt", "12,34")][InlineData("Amt", "1,234.00")][InlineData("Amt", "12.345")]
    [InlineData("Amt", "92233720368547758.08")][InlineData("CdtDbtInd", "CREDIT")]
    [InlineData("BookgDt", "2026-02-30")][InlineData("ValDt", "25.10.2026")]
    [InlineData("InstdAmt", "10.00")][InlineData("InstdAmtCcy", "USD")]
    [InlineData("TtlChrgsAndTaxAmt", "1.00")][InlineData("VatAmt", "-1.00")]
    [InlineData("Id", "original-0")]
    public async Task One_unsafe_source_row_remains_visible_and_blocks_the_entire_original_writer(string field, string value)
    {
        using var fixture = new FlowFixture(); var files = new Files { Text = Fixture(field: field, value: value) };
        var model = await ModelAsync(fixture, files); var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await model.PickSupaCommand.ExecuteAsync(null); Assert.True(model.HasPreview); Assert.Equal(3, model.BankLines.Count);
        Assert.NotEmpty(model.InvalidLines); model.BankAcknowledged = true; Assert.False(model.CanRunImport);
        await model.ImportCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Theory][InlineData("data")][InlineData("account")][InlineData("retire")][InlineData("selection")]
    public async Task Changed_original_inputs_or_consent_while_native_confirmation_waits_refuse_supa_import(string change)
    {
        using var fixture = new FlowFixture(); var model = await ModelAsync(fixture, new());
        await model.PickSupaCommand.ExecuteAsync(null); model.BankAcknowledged = true;
        fixture.Platform.PendingConfirmation = async () =>
        {
            if (change == "retire") model.RetireBankReview();
            else if (change == "selection") model.BankAcknowledged = false;
            else
            {
                await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                if (change == "account") (await db.Accounts.SingleAsync(Ct)).CurrencyCode = "USD";
                else db.Entries.Add(new() { AccountId = model.Account!.Id, Kind = EntryKind.Income, Amount = 7, Date = new(2026, 10, 24), Title = "Separate retained original row" });
                await db.SaveChangesAsync(Ct);
            }
            return true;
        };
        await model.ImportCommand.ExecuteAsync(null); Assert.NotNull(model.ImportError); Assert.False(model.CanRunImport);
        var entries = await fixture.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(change == "data" ? 1 : 0, entries.Count); Assert.DoesNotContain(entries, item => item.Source == EntrySource.Import);
    }

    [Fact]
    public void Missing_original_identity_keeps_equal_rows_distinct_and_requires_duplicate_review_instead_of_silent_skipping()
    {
        var rows = SupaCsv.Read(Fixture(identity: false));
        var account = new Account { Name = "Original anonymous source target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var first = SupaCsv.Preview(rows, account, [], [], new HashSet<Guid>());
        var repeat = SupaCsv.Preview(rows, account, [], first.Select(item => item.Entry!).ToArray(), new HashSet<Guid>());
        Assert.Equal(3, first.Select(item => item.Entry!.Id).Distinct().Count()); Assert.All(first, item => Assert.False(item.AlreadyImported));
        Assert.All(first.Skip(1), item => Assert.True(item.PossibleDuplicate));
        Assert.All(repeat, item => { Assert.False(item.AlreadyImported); Assert.True(item.PossibleDuplicate); });
        Assert.Null(SupaCsv.SourceSuffix(rows));
    }

    [Fact]
    public void Original_booking_status_default_reordered_columns_and_explicit_original_amount_preserve_the_same_row_identity()
    {
        var rows = SupaCsv.Read(Fixture(count: 1)); var header = rows[0].ToArray(); var values = rows[1].ToArray();
        values[Array.IndexOf(header, "BookgSts")] = ""; values[Array.IndexOf(header, "InstdAmt")] = "123.45";
        values[Array.IndexOf(header, "InstdAmtCcy")] = "USD";
        var account = new Account { Name = "Original target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var first = Assert.Single(SupaCsv.Preview([header, values], account, [], [], new HashSet<Guid>()));
        var second = Assert.Single(SupaCsv.Preview([header.Reverse().ToArray(), values.Reverse().ToArray()], account, [], [], new HashSet<Guid>()));
        Assert.Null(first.Error); Assert.Equal(first.Entry!.Id, second.Entry!.Id);
        Assert.Equal(10000, first.Entry.Amount); Assert.Equal(12345, first.Entry.OriginalAmount); Assert.Equal("USD", first.Entry.OriginalCurrencyCode);
    }

    [Fact]
    public async Task Retired_original_form_while_the_native_file_picker_waits_never_publishes_the_late_supa_source()
    {
        using var fixture = new FlowFixture(); var files = new Files(); var model = await ModelAsync(fixture, files);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        files.DuringPick = () => { model.RetireBankReview(); return Task.CompletedTask; };
        await model.PickSupaCommand.ExecuteAsync(null);
        Assert.False(model.IsBankPreset); Assert.False(model.HasPreview); Assert.False(model.CanRunImport);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Fact]
    public async Task Mixed_source_accounts_and_canceled_picker_never_change_the_original_financial_file()
    {
        using var fixture = new FlowFixture(); var files = new Files { Text = Fixture(field: "AcctId", value: "different-account") };
        var model = await ModelAsync(fixture, files); var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await model.PickSupaCommand.ExecuteAsync(null); Assert.NotNull(model.ImportError); Assert.False(model.CanRunImport);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        files.Cancel = true; await model.PickSupaCommand.ExecuteAsync(null); Assert.False(model.CanRunImport);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }
}
