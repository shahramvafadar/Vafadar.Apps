using Vafadar.Zanance.Testing;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-297: original German bank-software source contract through the real preview/writer/Undo path.</summary>
[Trait("AT", "AT-297")]
public sealed class BankCsvFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Files : IDataFileInteraction
    {
        internal string Text = Fixture();
        internal Func<Task>? DuringPick;
        internal bool Cancel;
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult<PickedDataFile?>(null);
        public async Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding)
        { if (DuringPick is not null) await DuringPick(); return Cancel ? null : new("fictitious-starmoney-giro.csv", Text); }
        public Task<bool> ConfirmExportAsync(string reason) => throw new InvalidOperationException("Bank import never exports.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new InvalidOperationException("Bank import never shares.");
    }
    internal static string Fixture(int count = 3, string? changeColumn = null, string? changeValue = null)
    {
        string[] header = ["BuchDatum", "Betrag", "BtgWaehr", "AgName1", "Storno", "ID", "QkIBAN", "BuchText", "WertDatum", "OCMTBetr", "OCMTWaehr", "Kommentar", "Kategorie",
            .. Enumerable.Range(1, 14).Select(i => "VWZ" + i), "Lfd.Nr", "Splittbuchung - Originalbetrag"];
        var rows = new List<IReadOnlyList<string>> { header };
        for (var i = 0; i < count; i++)
        {
            var values = header.ToDictionary(h => h, _ => "");
            values["BuchDatum"] = "09.10.2026"; values["Betrag"] = i == 0 ? "2.345,67" : "12,34-"; values["BtgWaehr"] = "EUR";
            values["ID"] = (100 + i).ToString(System.Globalization.CultureInfo.InvariantCulture); values["QkIBAN"] = "DE00000000000000004321";
            values["AgName1"] = "Fictitious Müller / نمونه"; values["BuchText"] = "Fictitious cashflow " + i;
            values["VWZ1"] = "Fictitious first reference"; values["VWZ14"] = "Fictitious last reference";
            values["WertDatum"] = "10.10.2026"; values["Kommentar"] = "Fictitious retained comment"; values["Kategorie"] = "Unmapped source label";
            if (changeColumn is not null && i == count - 1) values[changeColumn] = changeValue ?? "";
            rows.Add(header.Select(h => values[h]).ToArray());
        }
        return Csv.Write(rows, ';');
    }
    private static async Task<ImportExportViewModel> ModelAsync(FlowFixture f, Files files)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious original bank target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        var vm = new ImportExportViewModel(f.Store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time,
            f.Services.GetRequiredService<HoldingStore>(), files, f.Platform);
        await vm.LoadAsync(); return vm;
    }

    [Theory][InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Explicit_original_cashflows_confirm_cancel_import_reimport_and_durable_undo_without_changing_original_accounts(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var files = new Files(); var vm = await ModelAsync(f, files); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.True(vm.IsBankPreset); Assert.False(vm.ShowMapping); Assert.False(vm.BankAcknowledged); Assert.False(vm.CanRunImport);
        Assert.Equal(3, vm.ImportCount); Assert.Equal(3, vm.BankLines.Count); Assert.Contains("4321", vm.BankSourceText!);
        vm.BankAcknowledged = true; Assert.True(vm.CanRunImport);
        f.Platform.Confirmations.Enqueue(false); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(vm.CanRunImport);
        f.Platform.Confirmations.Enqueue(true); await vm.ImportCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ImportResult); Assert.Null(vm.ImportError); Assert.False(vm.CanRunImport);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(3, entries.Count);
        Assert.Equal(234567, Assert.Single(entries, e => e.Kind == EntryKind.Income).Amount);
        Assert.All(entries.Where(e => e.Kind == EntryKind.Expense), e => Assert.Equal(1234, e.Amount));
        Assert.All(entries, e => { Assert.Equal(ReviewState.Unreviewed, e.Review); Assert.Contains("Fictitious last reference", e.Note!); Assert.Contains("Fictitious retained comment", e.Note!); Assert.Contains("WertDatum: 10.10.2026", e.Note!); Assert.Contains("Unmapped source label", e.Note!); Assert.DoesNotContain("DE00000000000000004321", e.Note!); });
        var ids = entries.Select(e => e.Id).ToHashSet(); var batch = Assert.Single(vm.Batches);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(ids.Order(), (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Select(e => e.Id).Order());
        await vm.PickStarMoneyCommand.ExecuteAsync(null); Assert.Equal(0, vm.ImportCount); Assert.False(vm.CanRunImport);
        // Reopening the real model proves Undo does not depend on retained preview memory.
        var reopened = new ImportExportViewModel(f.Store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time,
            f.Services.GetRequiredService<HoldingStore>(), files, f.Platform);
        await reopened.LoadAsync(); f.Platform.Confirmations.Enqueue(true); await reopened.UndoCommand.ExecuteAsync(Assert.Single(reopened.Batches));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); FinancialSnapshotAssert.EqualAfterWrites(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("BtgWaehr", "USD")][InlineData("Storno", "J")][InlineData("ID", "100")][InlineData("Betrag", "12,345")][InlineData("BuchDatum", "31.02.2026")]
    public async Task One_invalid_bank_row_blocks_the_whole_import_without_hiding_valid_rows(string column, string value)
    {
        using var f = new FlowFixture(); var files = new Files { Text = Fixture(changeColumn: column, changeValue: value) }; var vm = await ModelAsync(f, files);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.True(vm.HasPreview); Assert.NotEmpty(vm.InvalidLines); vm.BankAcknowledged = true; Assert.False(vm.CanRunImport);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("data")][InlineData("account")][InlineData("retire")][InlineData("selection")]
    public async Task Actual_native_confirmation_cannot_publish_a_stale_bank_review(string change)
    {
        using var f = new FlowFixture(); var vm = await ModelAsync(f, new()); await vm.PickStarMoneyCommand.ExecuteAsync(null); vm.BankAcknowledged = true;
        f.Platform.PendingConfirmation = async () =>
        {
            if (change == "retire") vm.RetireBankReview();
            else if (change == "selection") vm.BankAcknowledged = false;
            else
            {
                await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                if (change == "account") (await db.Accounts.SingleAsync(Ct)).CurrencyCode = "USD";
                else db.Entries.Add(new() { AccountId = vm.Account!.Id, Amount = 555, Kind = EntryKind.Income, Date = new(2026, 10, 9), Title = "Fictitious separate later entry" });
                await db.SaveChangesAsync(Ct);
            }
            return true;
        };
        await vm.ImportCommand.ExecuteAsync(null); Assert.False(vm.CanRunImport);
        if (change == "retire") { Assert.Null(vm.ImportError); Assert.Empty(f.Platform.Failures); }
        else Assert.NotNull(vm.ImportError);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(change == "data" ? 1 : 0, entries.Count);
        Assert.DoesNotContain(entries, e => e.Source == EntrySource.Import);
    }

    [Fact]
    public async Task Every_source_page_is_available_and_target_changes_require_a_fresh_bound_review()
    {
        using var f = new FlowFixture(); var vm = await ModelAsync(f, new() { Text = Fixture(27) }); await vm.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.Equal(25, vm.BankLines.Count); vm.BankNextCommand.Execute(null); Assert.Equal(2, vm.BankLines.Count);
        vm.BankPreviousCommand.Execute(null); Assert.Equal(25, vm.BankLines.Count);
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious other bank account", CurrencyCode = "USD", OpeningDate = new(2026, 1, 1) }, Ct);
        await vm.LoadAsync(); vm.Account = vm.Accounts.Single(a => a.CurrencyCode == "USD");
        Assert.False(vm.HasPreview); Assert.False(vm.CanRunImport); Assert.False(vm.BankAcknowledged);
        await vm.PreviewCommand.ExecuteAsync(null); Assert.NotEmpty(vm.InvalidLines); vm.BankAcknowledged = true; Assert.False(vm.CanRunImport);
    }

    [Fact]
    public async Task Encoding_is_explicit_strict_and_preserves_european_names_and_euro_text()
    {
        var text = Fixture().Replace(" / نمونه", "").Replace("Fictitious retained comment", "Müller € — original");
        var legacy = CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes(text);
        Assert.Throws<DecoderFallbackException>(() => StarMoneyCsv.Decode(legacy, BankCsvEncoding.Utf8));
        Assert.Equal(text, StarMoneyCsv.Decode(legacy, BankCsvEncoding.Windows1252));
        Assert.Equal(text, StarMoneyCsv.Decode(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray(), BankCsvEncoding.Utf8));
        Assert.Throws<FormatException>(() => StarMoneyCsv.Decode(new byte[] { 239, 187, 191, 65 }, BankCsvEncoding.Windows1252));
        Assert.Throws<FormatException>(() => StarMoneyCsv.Decode(new byte[] { 255, 254, 65 }, BankCsvEncoding.Utf8));
    }

    [Theory][InlineData("split")][InlineData("unknown")][InlineData("quote")][InlineData("missing")][InlineData("columns")]
    public void Unsupported_structure_is_refused_as_a_whole_before_financial_preview(string change)
    {
        var text = change switch
        {
            "split" => Fixture(changeColumn: "Lfd.Nr", changeValue: "1"),
            "unknown" => Fixture().Replace("BuchDatum", "PendingDate"),
            "quote" => Fixture() + "\"unfinished",
            "missing" => Fixture().Replace(";VWZ14;", ";UnknownRemittance;"),
            _ => Fixture() + "wrong;count\r\n"
        };
        Assert.Throws<FormatException>(() => StarMoneyCsv.Read(text));
    }

    [Fact]
    public async Task Known_source_ids_never_replace_later_user_edits_and_equal_cashflows_remain_distinct()
    {
        using var f = new FlowFixture(); var files = new Files(); var vm = await ModelAsync(f, files); await vm.PickStarMoneyCommand.ExecuteAsync(null); vm.BankAcknowledged = true;
        f.Platform.Confirmations.Enqueue(true); await vm.ImportCommand.ExecuteAsync(null);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { var income = await db.Entries.SingleAsync(e => e.Kind == EntryKind.Income, Ct); income.Title = "Fictitious later user correction"; income.Amount = 777; await db.SaveChangesAsync(Ct); }
        await vm.PickStarMoneyCommand.ExecuteAsync(null); Assert.Equal(0, vm.ImportCount);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(777, Assert.Single(entries, e => e.Kind == EntryKind.Income).Amount);
        Assert.Equal(2, entries.Count(e => e.Kind == EntryKind.Expense)); Assert.Contains(vm.BankLines, line => line.Detail.Contains(f.Translator["BankCsv_Known"], StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cancelled_picker_retains_original_review_but_a_retired_picker_cannot_publish_a_late_source()
    {
        using var f = new FlowFixture(); var files = new Files(); var vm = await ModelAsync(f, files);
        await vm.PickStarMoneyCommand.ExecuteAsync(null); vm.BankAcknowledged = true;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var caption = vm.BankSourceText;
        files.Cancel = true; await vm.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport); Assert.Equal(caption, vm.BankSourceText);
        files.Cancel = false; files.DuringPick = () => { vm.RetireBankReview(); return Task.CompletedTask; };
        await vm.PickStarMoneyCommand.ExecuteAsync(null); Assert.False(vm.CanRunImport); Assert.False(vm.HasPreview);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData(AccountType.Loan)][InlineData(AccountType.Lent)][InlineData(AccountType.Asset)][InlineData(AccountType.CreditCard)]
    public async Task Giro_cashflow_preset_cannot_turn_loan_principal_or_asset_credit_into_income(AccountType type)
    {
        using var f = new FlowFixture(); var vm = await ModelAsync(f, new());
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { (await db.Accounts.SingleAsync(Ct)).Type = type; await db.SaveChangesAsync(Ct); }
        await vm.PickStarMoneyCommand.ExecuteAsync(null); vm.BankAcknowledged = true;
        Assert.False(vm.CanRunImport); Assert.Equal(3, vm.InvalidLines.Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_opening_boundary_remains_visible_and_mutated_proposed_rows_never_reach_the_writer()
    {
        using var f = new FlowFixture(); var vm = await ModelAsync(f, new());
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { (await db.Accounts.SingleAsync(Ct)).OpeningDate = new(2026, 11, 1); await db.SaveChangesAsync(Ct); }
        await vm.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.Contains(f.Translator.Format("Import_BeforeOpening", 3), vm.WarningText!);
        var path = await f.Store.CaptureImportFileAsync(Ct); var review = await f.Store.ReadImportReviewAsync(path, Ct);
        var entries = StarMoneyCsv.Preview(StarMoneyCsv.Read(Fixture()), Assert.Single(review.Inputs.Accounts), review.Inputs.Categories,
            review.Inputs.Entries, review.ConsumedIds).Select(r => r.Entry!).ToList();
        review.Bind(entries); entries[0].Amount++;
        await Assert.ThrowsAsync<Vafadar.Zanance.Data.Importing.ImportReviewChangedException>(() => f.Store.ImportReviewedAsync(entries, [], review, Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
