#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Exercises complete original transaction details on one independent fictitious file.</summary>
internal static class EntryDetailNativeReview
{
    /// <summary>Retains the original fixture, complete row baseline and native case receipts.</summary>
    internal sealed record Original(ServiceProvider Fixture, Guid First, Guid Second, string Rows, List<object> Cases);

    /// <summary>Seeds one bounded original packet once; later contexts only read it.</summary>
    internal static async Task<Original> PrepareAsync(string folder, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var path = Path.Combine(folder, "original-detail.db");
        if (File.Exists(path)) throw new IOException("Preserve the original detail fixture; never reseed it.");
        var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "D426 original cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var category = new Category { Name = "D426 original food", Kind = CategoryKind.Expense };
        await store.SaveAccountAsync(account, ct); await store.SaveCategoryAsync(category, ct);
        var first = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense,
            Date = new(2026, 10, 9), Amount = 1234, Title = "D426 original purchase", Payee = "D426 original grocer",
            Note = "Original retained note", Tags = ["OriginalTag"], Review = ReviewState.Unreviewed, GroupId = Guid.NewGuid() };
        var part = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense,
            Date = first.Date, Amount = 500, Title = "Original split part", GroupId = first.GroupId };
        var refund = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Refund,
            Date = first.Date, Amount = 100, RefundOfId = first.Id, Title = "Original refund" };
        var second = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Date = first.Date,
            Amount = 9000, Title = "D426 different income" };
        if (!(await store.SaveEntriesAsync([first, part, refund, second], [], ct)).Succeeded) throw new IOException("Original detail seed refused.");
        await store.AddAttachmentAsync(new() { EntryId = first.Id, FileName = "D426 original document.pdf",
            ContentType = "application/pdf", Data = [1, 2, 3, 4] }, ct);
        return new(fixture, first.Id, second.Id, await ImportFinancialNativeReview.StoredAsync(fixture, ct), []);
    }

    /// <summary>Continues only the unchanged original packet after a terminal pre-action report serialization failure.</summary>
    internal static async Task<Original> ContinueAsync(string folder, CancellationToken ct)
    {
        var path = Path.Combine(folder, "original-detail.db");
        if (!File.Exists(path) || new FileInfo(path).LinkTarget is not null || new DirectoryInfo(folder).LinkTarget is not null)
            throw new IOException("Exact original detail file required.");
        var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider(); var store = fixture.GetRequiredService<ZananceStore>();
        var entries = await store.GetEntriesAsync(cancellationToken: ct);
        var first = entries.Single(e => e.Title == "D426 original purchase"); var second = entries.Single(e => e.Title == "D426 different income");
        var packet = await store.ReadEntryDetailSnapshotAsync(first.Id, ct);
        var document = await store.GetAttachmentAsync(packet.Inputs.AttachmentReview!.Inputs.Attachments.Single().Id, ct);
        if (entries.Count != 4 || first.Amount != 1234 || first.Review != ReviewState.Unreviewed || second.Amount != 9000
            || packet.Inputs.Related.Count != 2 || packet.Inputs.Paybacks.Count != 1 || packet.Inputs.Rules.Count != 0
            || packet.Inputs.Resources.Settings is not null || document is null || !document.Data.SequenceEqual(new byte[] { 1, 2, 3, 4 }))
            throw new IOException("Original pre-action detail checkpoint differs.");
        return new(fixture, first.Id, second.Id, await ImportFinancialNativeReview.StoredAsync(fixture, ct), []);
    }

    /// <summary>Checks cover, a failed read and actual Retry, then every original ready context.</summary>
    internal static async Task ContextAsync(IServiceProvider services, Original original, string stage, bool first,
        Func<EntryDetailPage, Task> show, Func<EntryDetailPage, Task> close,
        Func<EntryDetailPage, string, Task> invoke, Func<EntryDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var factory = new HoldingOverviewNativeReview.Factory(original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        if (first) { factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); factory.FailOnce = true; }
        var model = Model(services, original, factory); var page = new EntryDetailPage(model);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = original.First });
        async Task Record(string state) => original.Cases.Add(await observe(page, stage + "-" + state));
        try
        {
            await show(page);
            if (first)
            {
                await AwaitAsync(() => factory.Calls > 0, ct); await Record("loading"); factory.Gate!.SetResult();
                await AwaitAsync(() => model.Loading.HasFailed, ct); await Record("error"); factory.Gate = null;
                await invoke(page, "retry");
            }
            await AwaitAsync(() => model.Loading.IsReady || model.Loading.HasFailed, ct); RequireReady(model, original.First);
            await Record("ready");
            if (first)
            {
                var oldReview = model.MarkReviewedCommand; var oldRule = model.SaveRuleCommand; var oldDelete = model.DeleteCommand;
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = original.Second }); await model.LoadAsync();
                RequireReady(model, original.Second);
                if (model.HasAttachments || model.HasRefunds || model.SplitText is not null || model.IsUnreviewed
                    || model.Attachments.Count != 0 || model.Refunds.Count != 0 || model.ReimbursementText is not null)
                    throw new IOException("New detail retained old packet fields.");
                await oldReview.ExecuteAsync(null); await oldRule.ExecuteAsync(null); await oldDelete.ExecuteAsync(null);
                await Record("new-query");
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = Guid.NewGuid() }); await model.LoadAsync();
                if (!model.Loading.IsReady || !model.NotFound || model.CanInteract || model.Lines.Count != 0 || model.Attachments.Count != 0)
                    throw new IOException("Missing detail retained data or authority.");
                await Record("missing");
                factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = original.First }); var calls = factory.Calls;
                var pending = model.LoadAsync(); await AwaitAsync(() => factory.Calls > calls, ct); model.Retire(); factory.Gate.SetResult();
                await pending;
                if (model.Loading.IsReady || model.CanInteract) throw new IOException("Retired detail escaped its cover.");
                original.Cases.Add(new { Stage = stage + "-retired", CompleteOwnerRetired = true, ProcessId = Environment.ProcessId });
            }
        }
        finally { await close(page); }
        await RequireUnchangedAsync(original, ct);
    }

    /// <summary>Invokes each changed original metadata action once, preserving the accepted result across notification failure.</summary>
    internal static async Task MetadataAsync(IServiceProvider services, Original original, Func<EntryDetailPage, Task> show,
        Func<EntryDetailPage, Task> close, Func<EntryDetailPage, string, Task> invoke,
        Func<EntryDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var store = original.Fixture.GetRequiredService<ZananceStore>(); var model = Model(services, original,
            original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var page = new EntryDetailPage(model);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = original.First });
        var notices = 0;
        void Refuse(object? sender, EventArgs args) { notices++; throw new IOException("Deliberate original metadata notification refusal."); }
        try
        {
            await show(page); await AwaitAsync(() => model.Loading.IsReady, ct); var oldReview = model.MarkReviewedCommand;
            store.Changed += Refuse;
            await invoke(page, "review"); await AwaitAsync(() => model.IsNotBusy && model.Loading.IsReady && !model.IsUnreviewed, ct);
            original.Cases.Add(await observe(page, "original-reviewed"));
            await oldReview.ExecuteAsync(null);
            if (notices != 1 || (await store.GetEntryAsync(original.First, ct))!.Review != ReviewState.Confirmed)
                throw new IOException("Original review acceptance was replayed or lost.");
            var oldRule = model.SaveRuleCommand; await invoke(page, "rule");
            await AwaitAsync(() => model.IsNotBusy && model.Loading.IsReady && model.RuleActionText is null, ct);
            original.Cases.Add(await observe(page, "original-rule")); await oldRule.ExecuteAsync(null);
            if (notices != 2 || (await store.GetCategoryRulesAsync(ct)).Count != 1)
                throw new IOException("Original rule acceptance was replayed or lost.");
        }
        finally { store.Changed -= Refuse; await close(page); }
        // Only the deliberate review flag and categorization rule may differ; ledger identity/amounts and documents remain exact.
        var after = await store.ReadEntryDetailSnapshotAsync(original.First, ct);
        if (after.Entry?.Amount != 1234 || after.Entry.Review != ReviewState.Confirmed || after.Inputs.Related.Count != 2
            || after.Inputs.Paybacks.Count != 1 || after.Inputs.AttachmentReview?.Inputs.Attachments.Count != 1
            || after.Inputs.Resources.Settings is not null) throw new IOException("Original metadata actions changed financial facts.");
        original.Cases.Add(new { MetadataCommands = 2, PostcommitNotificationFailures = notices, KnownCommandsReplayed = 0,
            FinancialCommands = 0, ProcessId = Environment.ProcessId });
    }

    /// <summary>Requires complete packet publication rather than a partly assigned header.</summary>
    internal static void RequireReady(EntryDetailViewModel model, Guid id)
    {
        if (!model.Loading.IsReady || model.NotFound || model.Displayed?.EntryId != id || !model.CanInteract || model.Heading is null
            || model.AmountText is null || model.Lines.Count == 0) throw new IOException("Complete original transaction detail unavailable.");
    }
    /// <summary>Compares every stored typed row after read-only native work.</summary>
    internal static async Task RequireUnchangedAsync(Original original, CancellationToken ct)
    {
        if (original.Rows != await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct))
            throw new IOException("Read-only original transaction detail changed stored rows.");
    }
    private static EntryDetailViewModel Model(IServiceProvider services, Original original, IDbContextFactory<ZananceDbContext> factory)
    {
        var store = new ZananceStore(factory, services.GetRequiredService<TimeProvider>(), original.Fixture.GetRequiredService<ICommercialWriteAccessSource>());
        // Use the same store instance for metadata notification qualification; gate-only contexts own a separate lease adapter.
        if (ReferenceEquals(factory, original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>()))
            store = original.Fixture.GetRequiredService<ZananceStore>();
        return new(store, services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(),
            services.GetRequiredService<IDateFormatter>(), new UndoService(store, services.GetRequiredService<TimeProvider>()),
            new HoldingOverviewNativeReview.Navigation(), services.GetRequiredService<Features.Receipts.ReceiptReadingSession>());
    }
    private static async Task AwaitAsync(Func<bool> ready, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (!ready()) { if (DateTime.UtcNow > deadline) throw new IOException("Preserve the original pending transaction detail context."); await Task.Delay(50, ct); }
    }
}
#endif
