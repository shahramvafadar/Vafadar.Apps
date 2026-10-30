using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original CSV page/file, native export and Undo continuations with actual isolated SQLite.</summary>
public sealed class DataFileWorkspaceBoundaryTests
{
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Original = original;
        internal TaskCompletionSource? Held;
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationToken Token;
        internal int Reads;
        public ZananceDbContext CreateDbContext() => Original.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        { Reads++; Token = ct; Entered.TrySetResult(); if (Held is not null) await Held.Task.WaitAsync(ct); return await Original.CreateDbContextAsync(ct); }
    }
    private sealed class Files : IDataFileInteraction
    {
        internal Func<Task<bool>>? PendingConfirm;
        internal Func<Task<PickedDataFile?>>? PendingPick;
        internal readonly List<string> Shares = [];
        public Task<PickedDataFile?> PickCsvAsync(string title) => PendingPick?.Invoke() ?? Task.FromResult<PickedDataFile?>(null);
        public Task<bool> ConfirmExportAsync(string reason) => PendingConfirm?.Invoke() ?? Task.FromResult(true);
        public Task ShareCsvAsync(string name, string title, string content) { Shares.Add(content); return Task.CompletedTask; }
    }
    private static (ImportExportViewModel Model, Factory Factory, Files Files) Create(FlowFixture f)
    {
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>(); var files = new Files();
        var model = new ImportExportViewModel(new(factory, f.Time, access), f.Translator, new DateFormatter(f.Localization),
            f.Localization, f.Time, new(factory, access), files, f.Platform);
        return (model, factory, files);
    }
    [Fact]
    public async Task Native_export_confirmation_cannot_publish_after_the_original_page_departs()
    {
        using var f = new FlowFixture(); var (model, _, files) = Create(f); await model.LoadAsync();
        files.PendingConfirm = () => { model.RetireBankReview(); return Task.FromResult(true); };
        await model.ExportCommand.ExecuteAsync(null); Assert.Empty(files.Shares);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Actual_CSV_page_departure_cancels_the_pending_original_database_acquisition()
    {
        using var f = new FlowFixture(); var (model, factory, _) = Create(f); factory.Held = new();
        var load = model.LoadAsync(); await factory.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        model.RetireBankReview(); var cancelled = factory.Token.IsCancellationRequested; factory.Held.SetResult(); await load;
        Assert.True(cancelled);
    }
    [Fact]
    public async Task A_replacement_CSV_file_is_refused_before_initializing_its_preferences()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var (model, factory, _) = Create(f); await model.LoadAsync();
        factory.Original = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); await model.LoadAsync();
        await using var db = await other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, await db.Settings.CountAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Read_only_CSV_reload_does_not_replace_the_explicit_raw_mapping_calendar()
    {
        using var f = new FlowFixture(); var (model, _, _) = Create(f); await model.LoadAsync(); Assert.True(model.CalendarNames.Count > 1);
        model.CalendarIndex = 1; await model.LoadAsync(); Assert.Equal(1, model.CalendarIndex);
    }
    [Fact]
    public async Task Native_Undo_confirmation_after_page_departure_cannot_delete_the_original_imported_row()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken);
        var account = new Account { Name = "Fictitious original Undo source", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 };
        await f.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken);
        var result = await f.Store.ImportAsync([new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = 100, Title = "D653 original Undo boundary" }], TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Imported); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        var (model, _, _) = Create(f); await model.LoadAsync(); var batch = Assert.Single(model.Batches);
        f.Platform.PendingConfirmation = () => { model.RetireBankReview(); return Task.FromResult(true); };
        await model.UndoCommand.ExecuteAsync(batch);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Retired_picker_cannot_replace_the_retained_original_filename()
    {
        using var f = new FlowFixture(); var (model, _, files) = Create(f); await model.LoadAsync();
        files.PendingPick = () => { model.Deactivate(); return Task.FromResult<PickedDataFile?>(new("retired.csv", "date;amount\r\n2026-10-09;-1\r\n")); };
        await model.PickCommand.ExecuteAsync(null); Assert.Null(model.FileName); Assert.False(model.CanUseDataFiles);
        model.SetPresentationActive(true); await model.LoadAsync(); Assert.True(model.CanUseDataFiles);
    }
    [Fact]
    public async Task Old_public_Load_cannot_reactivate_a_terminal_native_root()
    {
        using var f = new FlowFixture(); var (model, factory, _) = Create(f); await model.LoadAsync(); var reads = factory.Reads;
        model.RetireRoot(); model.SetPresentationActive(true); await model.LoadAsync();
        Assert.Equal(reads, factory.Reads); Assert.False(model.CanUseDataFiles);
    }
    [Fact]
    public async Task Original_Undo_writer_rolls_back_when_native_owner_retires_inside_the_transaction()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken);
        var account = new Account { Name = "Fictitious transactional Undo", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken);
        var imported = await f.Store.ImportAsync([new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = 100 }], TestContext.Current.CancellationToken);
        var batchId = Assert.IsType<Guid>(imported.BatchId); var workspace = await f.Store.ReadDataFileWorkAsync(TestContext.Current.CancellationToken); var calls = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() => f.Store.TryUndoImportAsync(batchId,
            TestContext.Current.CancellationToken, workspace.ReviewUndo(batchId), () => { if (++calls >= 6) throw new OperationCanceledException("Fictitious native retirement."); }));
        Assert.Equal(6, calls); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Single(await f.Store.GetImportBatchesAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Holdings_writer_refuses_a_retired_native_owner_before_inserting_metadata()
    {
        using var f = new FlowFixture(); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var content = Vafadar.Zanance.Core.DataFiles.HoldingsCsv.Read([], [], []);
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ImportAsync(content, TestContext.Current.CancellationToken,
            validateNative: () => throw new OperationCanceledException("Fictitious retired page.")));
        Assert.Empty(await store.GetTypesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task A_changed_native_mapping_caption_or_currency_requires_explicit_current_selection(bool currency)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken);
        var account = new Account { Name = "D653 original mapping", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken);
        var (model, _, files) = Create(f); await model.LoadAsync();
        files.PendingPick = () => Task.FromResult<PickedDataFile?>(new("original-map.csv", "date;amount;title\r\n2026-10-09;-12.34;Original raw mapping\r\n"));
        await model.PickCommand.ExecuteAsync(null); model.CalendarIndex = 0;
        if (currency) account.CurrencyCode = "USD"; else account.Name = "D653 changed mapping";
        await f.Store.SaveAccountAsync(account, TestContext.Current.CancellationToken);
        await model.PreviewCommand.ExecuteAsync(null); Assert.False(model.CanRunImport); Assert.NotNull(model.ImportError);
        await model.LoadAsync(); model.Account = Assert.Single(model.Accounts);
        await model.PreviewCommand.ExecuteAsync(null); Assert.True(model.CanRunImport);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
