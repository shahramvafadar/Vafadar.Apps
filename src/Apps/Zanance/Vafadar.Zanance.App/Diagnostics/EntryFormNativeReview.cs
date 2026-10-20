#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Qualifies the actual original editor against retained fictitious files without replaying accepted work.</summary>
internal static class EntryFormNativeReview
{
    /// <summary>Retains one seeded source, complete original row baseline and completed observations.</summary>
    internal sealed record Original(ServiceProvider Fixture, Guid Transfer, Guid Expense, Guid Archived, string Rows,
        string Folder, List<object> Cases, HashSet<string> CompletedStages);

    /// <summary>Gates only an actual context lease; records whether read connections opened on the presentation thread.</summary>
    internal sealed class Factory(IDbContextFactory<ZananceDbContext> actual) : IDbContextFactory<ZananceDbContext>
    {
        internal TaskCompletionSource? Gate;
        internal Exception? FailOnce;
        internal int Calls;
        internal int OpensOnPresentationThread;
        internal bool ObserveReads = true;
        private readonly int _presentation = Environment.CurrentManagedThreadId;
        public ZananceDbContext CreateDbContext() => Observe(actual.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            Calls++; if (Gate is { } gate) await gate.Task.WaitAsync(ct);
            if (FailOnce is { } failure) { FailOnce = null; throw failure; }
            return Observe(await actual.CreateDbContextAsync(ct));
        }
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            db.Database.GetDbConnection().StateChange += (_, args) =>
            {
                if (ObserveReads && args.CurrentState == System.Data.ConnectionState.Open && Environment.CurrentManagedThreadId == _presentation)
                    Interlocked.Increment(ref OpensOnPresentationThread);
            };
            return db;
        }
    }

    /// <summary>Creates one isolated original source, or reopens its exact retained manifest without reseeding.</summary>
    internal static async Task<Original> PrepareAsync(string folder, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-form.db"); var manifest = Path.Combine(folder, "original.json");
        var exists = File.Exists(file);
        if (new DirectoryInfo(folder).LinkTarget is not null || exists && new FileInfo(file).LinkTarget is not null)
            throw new IOException("Original nonredirected editor fixture required.");
        if (exists && !File.Exists(manifest)) throw new IOException("Retain the original incomplete seed; do not seed another file.");
        var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        if (exists)
        {
            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(manifest, ct)); var root = saved.RootElement;
            var old = new Original(fixture, root.GetProperty("Transfer").GetGuid(), root.GetProperty("Expense").GetGuid(),
                root.GetProperty("Archived").GetGuid(), root.GetProperty("Rows").GetString()!, folder, [], []);
            var progress = Path.Combine(folder, "progress.json");
            if (File.Exists(progress))
            {
                using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(progress, ct));
                foreach (var fact in prior.RootElement.GetProperty("Cases").EnumerateArray()) old.Cases.Add(fact.Clone());
                foreach (var stage in prior.RootElement.GetProperty("CompletedStages").EnumerateArray()) old.CompletedStages.Add(stage.GetString()!);
            }
            return old;
        }
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct); var store = fixture.GetRequiredService<ZananceStore>();
        var day = new DateOnly(2026, 10, 1);
        var from = new Account { Name = "D427 original EUR", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 100000 };
        var to = new Account { Name = "D427 original USD", CurrencyCode = "USD", OpeningDate = day };
        var archive = new Account { Name = "D427 historical GBP", CurrencyCode = "GBP", OpeningDate = day };
        foreach (var account in new[] { from, to, archive }) await store.SaveAccountAsync(account, ct);
        var food = new Category { Name = "D427 original food", Kind = CategoryKind.Expense };
        var fees = new Category { SystemKey = DefaultCategories.Fees, Kind = CategoryKind.Expense };
        await store.SaveCategoryAsync(food, ct); await store.SaveCategoryAsync(fees, ct);
        var transfer = new LedgerEntry { AccountId = from.Id, ToAccountId = to.Id, Kind = EntryKind.Transfer,
            Date = day.AddDays(8), Amount = 1234, ToAmount = 1100, Title = "D427 original transfer", Tags = ["OriginalTransfer"] };
        var fee = EntryActions.SyncTransferFee(transfer, null, 100, fees.Id)!;
        var destination = EntryActions.SyncDestinationFee(transfer, null, 200, fees.Id)!;
        var expense = new LedgerEntry { AccountId = from.Id, Kind = EntryKind.Expense, Date = transfer.Date, Amount = 2345,
            CategoryId = food.Id, Title = "D427 original purchase", Note = "Original purchase note", Tags = ["OriginalPurchase"],
            ReimbursableAmount = 1000, ReimbursedBy = "Original person" };
        var refund = new LedgerEntry { AccountId = from.Id, Kind = EntryKind.Refund, Date = transfer.Date,
            CategoryId = food.Id, Amount = 345, RefundOfId = expense.Id, Title = "Original purchase refund" };
        var archived = new LedgerEntry { AccountId = archive.Id, Kind = EntryKind.Expense, Date = transfer.Date,
            CategoryId = food.Id, Amount = 333, Title = "D427 historical purchase" };
        if (!(await store.SaveEntriesAsync([transfer, fee, destination, expense, refund, archived], [], ct)).Succeeded)
            throw new IOException("Original editor source seed refused.");
        archive.IsArchived = true; await store.SaveAccountAsync(archive, ct);
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var original = new Original(fixture, transfer.Id, expense.Id, archived.Id, rows, folder, [], []);
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { original.Transfer, original.Expense, original.Archived, original.Rows }), ct);
        return original;
    }

    /// <summary>Writes only derived observations at their original native process checkpoint.</summary>
    internal static async Task RecordAsync(Original original, string stage, object observation, CancellationToken ct)
    {
        if (!original.CompletedStages.Add(stage)) return;
        original.Cases.Add(new { Stage = stage, Observation = observation, ProcessId = Environment.ProcessId });
        var path = Path.Combine(original.Folder, "progress.json"); var temporary = path + ".pending";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new { original.Cases, original.CompletedStages }), ct);
        File.Move(temporary, path, overwrite: true);
    }
    /// <summary>Compares every mapped row after read-only editor work, including the absence of initialized preferences.</summary>
    internal static async Task RequireUnchangedAsync(Original original, CancellationToken ct)
    {
        if (original.Rows != await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct))
            throw new IOException("Original read-only editor changed stored rows.");
    }
    private static EntryEditorViewModel Model(IServiceProvider services, ZananceStore store, ReceiptWorkStore receipts) =>
        new(store, services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(),
            services.GetRequiredService<TimeProvider>(), new UndoService(store, services.GetRequiredService<TimeProvider>()),
            new AssetEntryConfirmation(services.GetRequiredService<Translator>(), new HoldingOverviewNativeReview.Navigation()),
            new HoldingOverviewNativeReview.Navigation(), receipts);
    private static Task Query(EntryEditorViewModel model, Dictionary<string, object> query) =>
        (Task)(typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new IOException("Original editor query unavailable.")).Invoke(model, [query])!;
    private static async Task<Dictionary<string, object>> RouteAsync(ZananceStore store, Guid id, string key, CancellationToken ct) =>
        new() { [key] = id, ["editorOrigin"] = new EntryEditorOrigin(await store.ReadEntryDetailSnapshotAsync(id, ct)) };

    /// <summary>Checks loading/failure/native Retry, original raw refresh and retired query/confirmation ownership.</summary>
    internal static async Task ContextAsync(IServiceProvider services, Original original, string context, bool first,
        Func<EntryEditorPage, Dictionary<string, object>, Task> show, Func<EntryEditorPage, Task> close, Func<EntryEditorPage, string, Task> invoke,
        Func<EntryEditorPage, string, Task<object>> observe, Func<EntryEditorPage, Func<Task>, Task> retireConfirmation,
        CancellationToken ct)
    {
        if (original.CompletedStages.Contains(context + "-missing")) return;
        var resumedNewQuery = original.CompletedStages.Contains(context + "-new-query");
        var factory = new Factory(original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var access = original.Fixture.GetRequiredService<ICommercialWriteAccessSource>(); var time = services.GetRequiredService<TimeProvider>();
        var store = new ZananceStore(factory, time, access); var model = Model(services, store, new ReceiptWorkStore(factory, access));
        var page = new EntryEditorPage(model);
        async Task Record(string state)
        {
            var stage = context + "-" + state;
            if (!original.CompletedStages.Contains(stage)) await RecordAsync(original, stage, await observe(page, stage), ct);
        }
        try
        {
            if (!original.CompletedStages.Contains(context + "-error"))
            {
                factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); factory.FailOnce = new IOException("Original form read refusal.");
                await show(page, await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Transfer, "id", ct));
                await AwaitAsync(() => factory.Calls > 0, ct); await Record("loading"); factory.Gate.SetResult(); await AwaitReadAsync(model, ct);
                if (!model.Loading.HasFailed || model.CanSave || model.HasPublishedForm) throw new IOException("Original failed form escaped its cover: " + JsonSerializer.Serialize(new { model.Loading.IsReady, model.Loading.HasFailed, model.Loading.IsLoading, model.CanSave, model.HasPublishedForm, model.LoadError, model.FormEpoch }));
                await Record("error"); factory.Gate = null; await invoke(page, "retry");
                await AwaitReadAsync(model, ct);
            }
            else
            {
                await show(page, await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Transfer, "id", ct));
                await AwaitReadAsync(model, ct);
            }
            RequireTransfer(model, original.Transfer); await Record("ready");
            if (first)
            {
                var oldSave = model.SaveCommand; var oldToggle = model.ToggleDetailsCommand;
                if (!original.CompletedStages.Contains(context + "-raw-refresh"))
                {
                    model.Note = "D427 retained raw note"; model.RetirePendingSave(); await model.ResumeFormAsync();
                    RequireTransfer(model, original.Transfer);
                    if (model.Note != "D427 retained raw note" || !model.IsDirty) throw new IOException("Original read-only refresh replaced raw input.");
                    await oldSave.ExecuteAsync(null); await Record("raw-refresh");
                }
                if (!original.CompletedStages.Contains(context + "-new-query"))
                {
                    await Query(model, await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Expense, "id", ct));
                    var details = model.ShowDetails; await oldToggle.ExecuteAsync(null);
                    if (model.FormReview?.Source?.Id != original.Expense || model.AmountText != MoneyText.ForInput(2345, "EUR",
                        services.GetRequiredService<ILocalizationService>().CurrentCulture) || model.Note != "Original purchase note"
                        || model.FeeText.Length != 0 || model.DestinationFeeText.Length != 0 || details != model.ShowDetails)
                        throw new IOException("New original query retained old draft or command authority.");
                    await Record("new-query");
                }
                if (!original.CompletedStages.Contains(context + "-retired-cancel"))
                {
                    var receipt = Path.Combine(original.Folder, context + "-retired-cancel-action.json");
                    // The original terminal Android report follows the verified archived-source/old-command guards,
                    // then fails only while observing the caption after dialog dismissal. Preserve that action.
                    if (!File.Exists(receipt) && original.CompletedStages.Contains(context + "-new-query"))
                    {
                        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(original.Folder)!, "failure-*.json"))
                        {
                            using var report = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
                            var evidence = report.RootElement;
                            if (evidence.TryGetProperty("ReviewCheck", out var check)
                                && check.GetString() == "Original native amount differs: AmountEntry"
                                && evidence.GetProperty("Stack").GetString()!.Contains("ContextAsync", StringComparison.Ordinal)
                                && original.Cases.Count > 0
                                && JsonSerializer.SerializeToElement(original.Cases[^1]).GetProperty("Stage").GetString() == context + "-new-query")
                            {
                                await RequireUnchangedAsync(original, ct);
                                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
                                { OriginalComparison = evidence.GetProperty("Comparison").GetString(),
                                    OriginalActionGuardReachedBeforeCaption = true, DerivedFromTerminalSourceStack = true,
                                    CaptionOnlyContinuation = true, NoCancelReplayed = true }), ct); break;
                            }
                        }
                    }
                    if (File.Exists(receipt))
                        await Query(model, await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Archived, "id", ct));
                    else
                    {
                        if (resumedNewQuery) throw new IOException("Retain the original native action evidence; do not repeat an ambiguous discard.");
                        await Query(model, await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Expense, "id", ct));
                        model.Note = "D427 original discard candidate";
                        await retireConfirmation(page, async () => await Query(model,
                            await RouteAsync(original.Fixture.GetRequiredService<ZananceStore>(), original.Archived, "id", ct)));
                    }
                    if (model.FormReview?.Source?.Id != original.Archived || model.Account?.CurrencyCode != "GBP" || model.HasNoAccounts
                        || model.Note.Length != 0 || model.RefundInfo is not null || model.HasReimbursementDue || model.ReimbursableEnabled)
                        throw new IOException("Retired discard or archived query adopted the old raw source.");
                    if (!File.Exists(receipt)) await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
                    { ProcessId = Environment.ProcessId, OriginalNativeCancelGuardPassed = true, NoCancelReplayed = true }), ct);
                    await Record("retired-cancel");
                }
            }
            factory.FailOnce = new EntryEditorSourceChangedException(); model.RetirePendingSave(); await model.ResumeFormAsync();
            if (!model.Loading.HasFailed || model.CanSave || model.CanReloadForm || !model.HasPublishedForm)
                throw new IOException("Changed original form retained editable authority.");
            await Record("source-changed");
            await Query(model, new() { ["id"] = Guid.NewGuid() });
            if (!model.Loading.HasFailed || model.CanSave || model.HasPublishedForm || !model.CanReloadForm)
                throw new IOException("Missing original form became a new transaction.");
            await Record("missing");
            if (factory.OpensOnPresentationThread != 0) throw new IOException("Original form read opened SQL on the presentation thread.");
        }
        finally { await close(page); }
        await RequireUnchangedAsync(original, ct);
    }

    /// <summary>Invokes one original edit and one derived duplicate through real native Save targets.</summary>
    internal static async Task FinancialAsync(IServiceProvider services, Original original,
        Func<EntryEditorPage, Dictionary<string, object>, Task> show, Func<EntryEditorPage, string, Task> invoke,
        Func<EntryEditorPage, string, Task<object>> observe, CancellationToken ct)
    {
        var store = original.Fixture.GetRequiredService<ZananceStore>();
        foreach (var duplicate in new[] { false, true })
        {
            var stage = duplicate ? "original-duplicate-save" : "original-edit-save";
            if (original.CompletedStages.Contains(stage)) continue;
            var checkpoint = Path.Combine(original.Folder, stage + "-accepted.json");
            if (File.Exists(checkpoint))
            {
                using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(checkpoint, ct));
                var rows = await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct);
                if (rows != accepted.RootElement.GetProperty("Rows").GetString()) throw new IOException("Retained accepted editor rows changed.");
                await RecordAsync(original, stage, new { AcceptedReadOnlyContinuation = true, NoSaveReplayed = true }, ct); continue;
            }
            var before = await store.GetEntriesAsync();
            var source = before.Single(e => e.Id == original.Transfer); var originalGroup = source.GroupId;
            var related = before.Where(e => e.GroupId == originalGroup).ToArray();
            var model = Model(services, store, original.Fixture.GetRequiredService<ReceiptWorkStore>());
            var page = new EntryEditorPage(model);
            await show(page, await RouteAsync(store, original.Transfer, duplicate ? "duplicate" : "id", ct));
            await AwaitReadAsync(model, ct);
            if (!model.CanSave || model.FormReview?.Source?.Id != original.Transfer) throw new IOException("Original native Save source unavailable.");
            if (!duplicate) await invoke(page, "amount");
            var draftId = model.FormDraftId; var savedCommand = model.SaveCommand;
            await observe(page, stage);
            var notices = 0;
            void Changed(object? sender, EventArgs args)
            {
                notices++;
                if (!model.FormAccepted) throw new IOException("Original financial acceptance was not consumed before notification.");
                // Persist acceptance before the deliberate observer failure; never replay Save to repair a later observer.
                File.WriteAllText(checkpoint, JsonSerializer.Serialize(new { DraftId = draftId, ProcessId = Environment.ProcessId,
                    Rows = Task.Run(() => ImportFinancialNativeReview.StoredAsync(original.Fixture, ct), ct).GetAwaiter().GetResult() }));
                throw new IOException("Deliberate original editor postcommit observer refusal.");
            }
            store.Changed += Changed;
            try
            {
                await invoke(page, "save"); await AwaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page) && !model.IsBusy, ct);
            }
            finally { store.Changed -= Changed; }
            if (!model.FormAccepted || model.CanSave || notices != 1) throw new IOException("Original native Save acceptance differs.");
            await savedCommand.ExecuteAsync(null);
            var after = await store.GetEntriesAsync(); var saved = after.Single(e => e.Id == draftId);
            if (saved.Kind != EntryKind.Transfer || saved.Amount != 2100 || saved.ToAmount != 1100
                || saved.AccountId != source.AccountId || saved.ToAccountId != source.ToAccountId
                || EntryActions.FindTransferFee(saved, after)?.Amount != 100 || EntryActions.FindDestinationFee(saved, after)?.Amount != 200)
                throw new IOException("Original native transfer currencies or both fee values differ.");
            if (duplicate)
            {
                if (draftId == source.Id || saved.GroupId == originalGroup || after.Count != before.Count + 3
                    || before.Any(e => JsonSerializer.Serialize(e) != JsonSerializer.Serialize(after.Single(a => a.Id == e.Id))))
                    throw new IOException("Derived native Save replaced original financial identities.");
            }
            else
            {
                if (draftId != source.Id || saved.GroupId != originalGroup || after.Count != before.Count)
                    throw new IOException("Original native edit changed a financial identity.");
                foreach (var old in before)
                {
                    var expected = old.Copy(); var actual = after.Single(a => a.Id == old.Id).Copy();
                    if (old.Id == source.Id) expected.Amount = 2100;
                    // The existing writer timestamps every submitted group row; only that audit field may advance.
                    if (related.Any(e => e.Id == old.Id)) actual.UpdatedAt = expected.UpdatedAt;
                    if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(actual))
                        throw new IOException("Original native edit changed another financial field.");
                }
            }
            await RecordAsync(original, stage, new { DraftId = draftId, Saved = saved, KnownAcceptance = true,
                PostcommitObserverFailures = notices, BothFeeAmountsAndIdentitiesChecked = true, NoAcceptedCommandReplayed = true }, ct);
        }
    }

    /// <summary>Checks that an explicitly new routed form clears every previous optional raw field, without Save.</summary>
    internal static async Task FreshDefaultsAsync(IServiceProvider services, Original original,
        Func<EntryEditorPage, Dictionary<string, object>, Task> show, Func<EntryEditorPage, Task> close,
        Func<EntryEditorPage, string, Task<object>> observe, CancellationToken ct)
    {
        const string stage = "fresh-query-defaults";
        if (original.CompletedStages.Contains(stage)) return;
        var before = await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct);
        var store = original.Fixture.GetRequiredService<ZananceStore>();
        var model = Model(services, store, original.Fixture.GetRequiredService<ReceiptWorkStore>());
        var page = new EntryEditorPage(model);
        await show(page, await RouteAsync(store, original.Transfer, "id", ct)); await AwaitReadAsync(model, ct);
        try
        {
            var oldSave = model.SaveCommand;
            model.IconKey = "Tag"; model.Note = "Original optional raw fields"; model.TagsText = "OriginalOptionalTag";
            model.ForeignEnabled = true; model.ForeignCurrency = "USD"; model.ForeignAmountText = "9.87";
            await Query(model, new() { ["kind"] = "Income" }); await oldSave.ExecuteAsync(null);
            if (!model.IsFormEditable || model.FormReview?.Source is not null || model.IconKey is not null
                || model.Note.Length != 0 || model.TagsText.Length != 0 || model.ForeignEnabled || model.ForeignAmountText.Length != 0
                || model.AmountText.Length != 0 || model.FeeText.Length != 0 || model.DestinationFeeText.Length != 0
                || model.ToAmountText.Length != 0 || model.IsDirty)
                throw new IOException("An explicitly new form retained optional raw source fields.");
            await RecordAsync(original, stage, await observe(page, stage), ct);
        }
        finally { await close(page); }
        if (before != await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct))
            throw new IOException("Original fresh-query review changed stored rows.");
    }

    /// <summary>Checks complete original transfer input, both currencies/fees and a separate mutable draft.</summary>
    internal static void RequireTransfer(EntryEditorViewModel model, Guid id)
    {
        if (!model.Loading.IsReady || !model.IsFormEditable || model.FormReview?.Source?.Id != id || !model.CanSave
            || !model.IsTransfer || model.Account?.CurrencyCode != "EUR" || model.ToAccount?.CurrencyCode != "USD"
            || !model.ShowDestinationFee || !model.ShowToAmount || model.FeeText.Length == 0 || model.DestinationFeeText.Length == 0)
            throw new IOException("Complete original transfer form unavailable: " + JsonSerializer.Serialize(new { model.Loading.IsReady, model.Loading.HasFailed, model.Loading.IsLoading, model.IsFormEditable, model.CanSave, Source = model.FormReview?.Source?.Id, model.IsTransfer, From = model.Account?.CurrencyCode, To = model.ToAccount?.CurrencyCode, model.ShowDestinationFee, model.ShowToAmount, model.FeeText, model.DestinationFeeText, model.LoadError }));
    }
    /// <summary>Lets Shell own the actual query and modal lifecycle for this exact isolated form.</summary>
    internal static async Task ShowRoutedAsync(EntryEditorPage page, Dictionary<string, object> query)
    {
        var route = "original-entry-form-" + Guid.NewGuid().ToString("N");
        Routing.RegisterRoute(route, new OriginalPageFactory(page));
        try { await Shell.Current.GoToAsync(route, query); }
        finally { Routing.UnRegisterRoute(route); }
    }
    /// <summary>Retains one actual page without changing its native query or navigation contract.</summary>
    private sealed class OriginalPageFactory(EntryEditorPage page) : RouteFactory
    {
        /// <inheritdoc />
        public override Element GetOrCreate() => page;
        /// <inheritdoc />
        public override Element GetOrCreate(IServiceProvider services) => page;
    }
    private static async Task AwaitReadAsync(EntryEditorViewModel model, CancellationToken ct)
    {
        var field = typeof(EntryEditorViewModel).GetField("_formLoadTask", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new IOException("Original pending form read unavailable.");
        await AwaitAsync(() => field.GetValue(model) is Task || model.Loading.HasFailed || model.HasPublishedForm, ct);
        if (field.GetValue(model) is Task pending) await pending.WaitAsync(ct);
    }
    private static async Task AwaitAsync(Func<bool> ready, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (!ready()) { if (DateTime.UtcNow > deadline) throw new IOException("Preserve original pending editor context."); await Task.Delay(50, ct); }
    }
}
#endif
