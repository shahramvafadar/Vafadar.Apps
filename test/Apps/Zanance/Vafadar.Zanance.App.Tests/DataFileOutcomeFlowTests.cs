using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-168: actual CSV review/import/Undo sources retain refusals and retire accepted payloads before follow-through.</summary>
[Trait("AT", "AT-168")]
public sealed partial class DataFileOutcomeFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string path) => Current;
    }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Refuse;
        public ZananceDbContext CreateDbContext() => Refuse ? throw new IOException("Fictitious reader refusal.") : original.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Refuse
            ? Task.FromException<ZananceDbContext>(new IOException("Fictitious reader refusal.")) : original.CreateDbContextAsync(cancellationToken);
    }
    private sealed class Files : IDataFileInteraction
    {
        internal PickedDataFile? Picked;
        internal Func<Task<PickedDataFile?>>? PendingPick;
        internal Func<Task<bool>>? PendingConfirm;
        internal bool Confirm = true;
        internal readonly List<(string Name, string Title, string Text)> Shares = [];
        public Task<PickedDataFile?> PickCsvAsync(string title) => PendingPick?.Invoke() ?? Task.FromResult(Picked);
        public Task<bool> ConfirmExportAsync(string reason) => PendingConfirm?.Invoke() ?? Task.FromResult(Confirm);
        public Task ShareCsvAsync(string fileName, string title, string content) { Shares.Add((fileName, title, content)); return Task.CompletedTask; }
    }
    private static (ZananceStore Store, HoldingStore Holdings, Factory Factory, ImportExportViewModel Vm) Create(FlowFixture f, Access access, Files files)
    {
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, f.Time, access); var holdings = new HoldingStore(factory, access);
        return (store, holdings, factory, new(store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, holdings, files, f.Platform));
    }
    private static void Tier(FlowFixture f, Access access, bool allowed) => access.Current = new(f.Directory.Combine("zanance.db"),
        new(ProductPlan.Free, new(allowed ? EntitlementScopeKind.PersonalProfile : EntitlementScopeKind.SharedSpace,
            Guid.Parse("89898989-2323-4545-6767-898989898989"))));
    private static async Task<(Account Account, LedgerEntry Entry)> LedgerAsync(FlowFixture f, Files files)
    {
        await f.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), OpeningBalance = 90000 };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 9), Amount = 1234,
            Title = "Fictitious complete raw purchase", Note = "Fictitious complete note\nRetained second line", Tags = ["fictitious", "review"] };
        files.Picked = new("fictitious-owned.csv", CsvExport.Write([entry], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
        return (account, entry);
    }
    private static async Task HoldingsAsync(FlowFixture f, Files files)
    {
        await f.Store.GetSettingsAsync(Ct);
        var type = new AssetType { Name = "Fictitious original holding", Kind = AssetKind.Other, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        var location = new AssetLocation { Name = "Fictitious original location" };
        var entry = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening, Date = new(2026, 10, 9), Quantity = 1000, Note = "Fictitious retained holding note" };
        files.Picked = new("fictitious-holdings.csv", HoldingsCsv.Write([type], [location], [entry], [], true));
    }
    private static string Draft(ImportExportViewModel vm) => JsonSerializer.Serialize(new
    { vm.FileName, vm.PreviewText, vm.WarningText, vm.ShowMapping, vm.HasPreview, vm.SkipDuplicates, vm.Account, vm.DateColumn, vm.AmountColumn,
        vm.DateFormat, vm.CalendarIndex, vm.SignIndex, vm.SeparatorIndex, vm.ImportCount, Invalid = vm.InvalidLines.ToArray(),
        Overlaps = vm.Overlaps.Select(row => new { row.Preview, row.SelectedIndex, row.Outcome }).ToArray() });

    [Theory]
    [InlineData("en", false)][InlineData("fa", false)][InlineData("de", false)][InlineData("es", false)][InlineData("fr", false)][InlineData("it", false)]
    [InlineData("en", true)][InlineData("fa", true)][InlineData("de", true)][InlineData("es", true)][InlineData("fr", true)][InlineData("it", true)]
    public async Task Original_permission_rejection_retains_complete_review_and_explicit_retry_imports_once(string language, bool holdings)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        var files = new Files(); if (holdings) await HoldingsAsync(f, files); else await LedgerAsync(f, files);
        var access = new Access(); var (store, assetStore, _, vm) = Create(f, access, files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); Assert.True(vm.CanRunImport);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Tier(f, access, false); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Contains(f.Translator["Commercial_RequiresMembership"], vm.ImportError!);
        Assert.Contains(f.Translator["Commercial_DraftKept"], vm.ImportError!);
        Assert.Equal(draft, Draft(vm)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(vm.CanRunImport);
        Tier(f, access, true); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Null(vm.ImportError); Assert.NotNull(vm.ImportResult); Assert.False(vm.CanRunImport); Assert.False(vm.HasPreview);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ImportCommand.ExecuteAsync(null);
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(vm.CanReviewPreview); Assert.False(vm.HasPreview);
        if (holdings) Assert.Single(await assetStore.GetEventsAsync(cancellationToken: Ct)); else Assert.Single(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Original_ledger_commit_and_undo_results_survive_publication_failure_and_cannot_resubmit(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        var files = new Files(); await LedgerAsync(f, files); var (store, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var before = await GoalChooserFlowTests.SnapshotAsync(f); var notified = 0;
        store.Changed += (_, _) => { notified++; throw new IOException("Fictitious committed subscriber refusal."); };
        await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator.Format("Import_Done", 1, 0), vm.ImportResult);
        Assert.Equal(f.Translator["Import_SavedFollowUpFailed"], vm.ImportError); Assert.False(vm.CanRunImport); Assert.False(vm.HasPreview);
        var originalBatch = Assert.Single(vm.Batches); var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, notified);
        f.Platform.Confirmations.Enqueue(true); await vm.UndoCommand.ExecuteAsync(originalBatch);
        Assert.Equal(f.Translator["Import_UndoDone"], vm.ImportResult); Assert.Equal(f.Translator["Import_UndoFollowUpFailed"], vm.UndoError);
        Assert.Empty(vm.Batches); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(2, notified);
        Assert.False(vm.UndoCommand.CanExecute(originalBatch)); await vm.UndoCommand.ExecuteAsync(originalBatch); Assert.Equal(2, notified);
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Original_holdings_commit_retains_exact_counts_and_prevents_repeated_review_after_subscriber_failure(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        var files = new Files(); await HoldingsAsync(f, files); var (_, holdings, _, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var notifications = 0;
        holdings.Changed += (_, _) => { notifications++; throw new IOException("Fictitious accepted holding publication failure."); };
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(f.Translator["Import_SavedFollowUpFailed"], vm.ImportError);
        Assert.Contains("3", vm.ImportResult!); Assert.Single(await holdings.GetTypesAsync(Ct)); Assert.Single(await holdings.GetLocationsAsync(Ct)); Assert.Single(await holdings.GetEventsAsync(cancellationToken: Ct));
        var after = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ImportCommand.ExecuteAsync(null); await vm.RefreshPreviewCommand.ExecuteAsync(null);
        Assert.Equal(after, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, notifications); Assert.False(vm.CanRunImport);
    }

    [Fact]
    public async Task Accepted_import_retires_payload_before_real_history_read_failure_without_erasing_old_batches()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files); var (store, _, factory, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); store.Changed += (_, _) => factory.Refuse = true;
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(f.Translator["Import_SavedFollowUpFailed"], vm.ImportError); Assert.NotNull(vm.ImportResult);
        Assert.False(vm.CanRunImport); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var after = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(after, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Cancelled_picker_and_busy_declined_undo_preserve_complete_review_and_history()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files); var (_, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var picked = new TaskCompletionSource<PickedDataFile?>(TaskCreationOptions.RunContinuationsAsynchronously); files.PendingPick = () => picked.Task;
        var pending = vm.PickCommand.ExecuteAsync(null); Assert.True(vm.IsBusy); Assert.False(vm.ImportCommand.CanExecute(null)); Assert.False(vm.ExportCommand.CanExecute(null));
        picked.SetResult(null); await pending; Assert.Equal(draft, Draft(vm)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        files.PendingPick = null; await vm.ImportCommand.ExecuteAsync(null); var batch = Assert.Single(vm.Batches);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => consent.Task; pending = vm.UndoCommand.ExecuteAsync(batch); Assert.True(vm.IsBusy);
        Assert.False(vm.PickCommand.CanExecute(null)); Assert.False(vm.UndoCommand.CanExecute(batch)); consent.SetResult(false); await pending;
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(batch, Assert.Single(vm.Batches));
    }

    [Fact]
    public async Task Fresh_review_failure_keeps_exact_file_and_draft_and_blocks_import_until_successful_review()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files); var (_, _, factory, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        factory.Refuse = true; await vm.RefreshPreviewCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Import_ReviewFailed"], vm.ImportError); Assert.Equal(draft, Draft(vm)); Assert.False(vm.CanRunImport);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); factory.Refuse = false; await vm.RefreshPreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport); Assert.Null(vm.ImportError); Assert.Equal(draft, Draft(vm));
    }

    [Fact]
    public async Task Original_export_holds_confirmation_and_keeps_neutral_names_complete_csv_and_security_port()
    {
        using var f = new FlowFixture(); var files = new Files(); var (_, entry) = await LedgerAsync(f, files); await f.Store.SaveEntryAsync(entry, Ct);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync();
        var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); files.PendingConfirm = () => consent.Task;
        var pending = vm.ExportCommand.ExecuteAsync(null); Assert.True(vm.IsBusy); Assert.False(vm.PickCommand.CanExecute(null));
        consent.SetResult(false); await pending; Assert.Empty(files.Shares); Assert.False(vm.IsBusy);
        files.PendingConfirm = null; await vm.ExportCommand.ExecuteAsync(null); var csv = Assert.Single(files.Shares);
        Assert.StartsWith("zanance-export-20261009-0900", csv.Name); Assert.EndsWith(".csv", csv.Name); Assert.Contains(entry.Note!, csv.Text);
        Assert.Equal(f.Translator["Export_Title"], csv.Title);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Actual_sql_failure_rolls_back_the_whole_original_import_and_retains_review(bool holdings)
    {
        using var f = new FlowFixture(); var files = new Files(); if (holdings) await HoldingsAsync(f, files); else await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        if (holdings) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER owned_refusal BEFORE INSERT ON AssetEvents BEGIN SELECT RAISE(ABORT, 'Owned holding refusal'); END", Ct);
        else await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER owned_refusal BEFORE INSERT ON Entries BEGIN SELECT RAISE(ABORT, 'Owned ledger refusal'); END", Ct);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Import_SaveFailed"], vm.ImportError); Assert.True(vm.CanRunImport); Assert.Equal(draft, Draft(vm));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); await db.Database.ExecuteSqlRawAsync("DROP TRIGGER owned_refusal", Ct);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Null(vm.ImportError); Assert.False(vm.CanRunImport);
    }

    [Fact]
    public async Task Explicit_aggregate_choice_and_complete_metadata_survive_permission_refusal_then_durable_import_and_undo()
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, detail) = await LedgerAsync(f, files);
        var aggregate = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 31), Amount = 5000,
            IsAggregated = true, AggregatedFrom = new(2026, 10, 1), AggregatedTo = new(2026, 10, 31),
            Title = "Fictitious complete aggregate", Note = "Fictitious complete aggregate note", Tags = ["retained"] };
        Assert.True((await f.Store.SaveEntryAsync(aggregate, Ct)).Succeeded);
        var access = new Access(); var (_, _, _, vm) = Create(f, access, files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var choice = Assert.Single(vm.Overlaps); Assert.False(vm.CanRunImport); choice.SelectedIndex = 0; Assert.True(vm.CanRunImport);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f); Tier(f, access, false); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(draft, Draft(vm)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Tier(f, access, true);
        await vm.ImportCommand.ExecuteAsync(null); var after = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(aggregate.Amount - detail.Amount, after.Single(row => row.Id == aggregate.Id).Amount);
        Assert.Equal(aggregate.Note, after.Single(row => row.Id == aggregate.Id).Note); Assert.Equal(detail.Note, after.Single(row => row.Id == detail.Id).Note);
        f.Platform.Confirmations.Enqueue(true); await vm.UndoCommand.ExecuteAsync(Assert.Single(vm.Batches));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Accepted_undo_removes_only_its_original_row_before_history_failure_and_blocks_a_retained_old_command()
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        await f.Store.ImportAsync([new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 8), Amount = 567, Title = "Fictitious older imported row" }], Ct);
        var (store, _, factory, vm) = Create(f, new(), files); await vm.LoadAsync(); var older = Assert.Single(vm.Batches);
        await vm.PickCommand.ExecuteAsync(null); await vm.ImportCommand.ExecuteAsync(null); var target = vm.Batches.Single(row => row.BatchId != older.BatchId);
        store.Changed += (_, _) => factory.Refuse = true; f.Platform.Confirmations.Enqueue(true); await vm.UndoCommand.ExecuteAsync(target);
        Assert.Equal(f.Translator["Import_UndoFollowUpFailed"], vm.UndoError); Assert.Equal(older, Assert.Single(vm.Batches)); Assert.False(vm.UndoCommand.CanExecute(target));
        var after = await GoalChooserFlowTests.SnapshotAsync(f); await vm.UndoCommand.ExecuteAsync(target); Assert.Equal(after, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

}
