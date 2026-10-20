using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-427: original editor reads, refresh and financial source ownership through actual isolated SQLite.</summary>
[Trait("AT", "AT-427")]
public sealed class EntryEditorLoadFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static async Task<LedgerEntry> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original editor cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 9),
            Amount = 1234, Title = "Original editor source", Note = "Original source note", Tags = ["Shared", "Original"] };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded); return entry;
    }
    private static async Task<EntryEditorReadSnapshot> ReadAsync(FlowFixture f, LedgerEntry? entry = null) =>
        await f.Store.ReadEntryEditorSnapshotAsync(new(Today, EditId: entry?.Id), ct: Ct);

    [Fact]
    public async Task Coherent_editor_read_retains_group_refunds_tag_projection_and_absent_settings_without_writes()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); entry.GroupId = Guid.NewGuid();
        var part = new LedgerEntry { AccountId = entry.AccountId, Kind = EntryKind.Expense, Date = entry.Date,
            GroupId = entry.GroupId, Amount = 200, Tags = ["Shared"] };
        Assert.True((await f.Store.SaveEntriesAsync([entry, part], [], Ct)).Succeeded);
        var refund = new LedgerEntry { AccountId = entry.AccountId, Kind = EntryKind.Refund, Date = entry.Date,
            RefundOfId = entry.Id, Amount = 100 };
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var source = await ReadAsync(f, entry);
        await f.Store.ValidateEntryEditorSnapshotAsync(source, Ct);
        Assert.True(source.IsAvailable); Assert.Equal(entry.Id, source.Source!.Id); Assert.Equal(2, source.Inputs.Related.Count);
        Assert.Equal(refund.Id, Assert.Single(source.Inputs.Paybacks).Id); Assert.Null(source.Inputs.Resources.Settings);
        Assert.Equal(new[] { "Shared", "Original" }, source.Inputs.Tags);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("edit")][InlineData("duplicate")][InlineData("refund")][InlineData("income")]
    public async Task Missing_explicit_original_never_becomes_an_available_blank_form(string kind)
    {
        using var f = new FlowFixture(); var id = Guid.NewGuid();
        var request = kind switch { "edit" => new EntryEditorReadRequest(Today, EditId: id),
            "duplicate" => new(Today, DuplicateId: id), "refund" => new(Today, RefundOfId: id),
            _ => new(Today, IncomeId: id, Kind: EntryKind.IncomeReversal) };
        var session = new EntryEditorLoadSession(f.Store, request); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await session.ReadAsync(); Assert.False(source.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => session.Publish(source)); Assert.Null(session.Review);
        var entry = new LedgerEntry { Kind = EntryKind.Expense, Date = Today, Amount = 100 };
        var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync([entry], [], entry.Id, null, () => true));
        Assert.False(save.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Empty_original_file_prepares_new_form_without_default_accounts_categories_or_preferences()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await ReadAsync(f); Assert.True(source.IsAvailable); Assert.Null(source.Source);
        Assert.Empty(source.Inputs.Resources.Accounts); Assert.Empty(source.Inputs.Resources.Categories);
        Assert.Null(source.Inputs.Resources.Settings); Assert.Empty(source.Inputs.Tags);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_retired_prepared_packet_cannot_publish_over_a_later_read()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryEditorLoadSession(f.Store, new(Today, EditId: entry.Id));
        var old = await session.ReadAsync(); session.Retire(); var current = await session.ReadAsync();
        Assert.Throws<InvalidOperationException>(() => session.Publish(old)); session.Publish(current);
        Assert.Same(current, session.Review); Assert.True(session.HasPublished);
    }

    [Fact]
    public async Task Explicit_refresh_revalidates_the_same_original_packet_without_replacing_published_input_facts()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryEditorLoadSession(f.Store, new(Today, EditId: entry.Id));
        var original = await session.ReadAsync(); session.Publish(original); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        session.Retire(); Assert.Null(session.Review); var refreshed = await session.ReadAsync(); Assert.Same(original, refreshed);
        session.Publish(refreshed); await session.ValidateAsync(); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("source")][InlineData("group")][InlineData("refund")][InlineData("account")][InlineData("group-refund")]
    public async Task Changed_original_financial_packet_refuses_save_without_overwriting_any_later_rows(string change)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); entry.GroupId = Guid.NewGuid();
        var part = new LedgerEntry { AccountId = entry.AccountId, Date = entry.Date, Kind = EntryKind.Expense, Amount = 200, GroupId = entry.GroupId };
        Assert.True((await f.Store.SaveEntriesAsync([entry, part], [], Ct)).Succeeded); var source = await ReadAsync(f, entry);
        var draft = source.Source!.Copy(); draft.Amount = 1500;
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            if (change == "source") await db.Entries.Where(e => e.Id == entry.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.Note, "Later source note"), Ct);
            if (change == "group") await db.Entries.Where(e => e.Id == part.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.Amount, 300), Ct);
            if (change == "account") await db.Accounts.Where(a => a.Id == entry.AccountId).ExecuteUpdateAsync(s => s.SetProperty(a => a.CurrencyCode, "USD"), Ct);
        }
        if (change is "refund" or "group-refund") Assert.True((await f.Store.SaveEntryAsync(new() { AccountId = entry.AccountId, Kind = EntryKind.Refund,
            Date = entry.Date, Amount = 100, RefundOfId = change == "group-refund" ? part.Id : entry.Id }, Ct)).Succeeded);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => save.SaveAsync([draft], [], draft.Id, null, () => true));
        Assert.False(save.Accepted); Assert.False(source.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Changed_original_source_refuses_refresh_instead_of_adopting_later_financial_fields()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryEditorLoadSession(f.Store, new(Today, EditId: entry.Id));
        var source = await session.ReadAsync(); session.Publish(source); entry.Note = "Later persisted note"; await f.Store.SaveEntryAsync(entry, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); session.Retire();
        await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => session.ReadAsync());
        Assert.Null(session.Review); Assert.Equal("Original source note", source.Source!.Note);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Known_original_write_consumes_its_packet_before_notification_and_cannot_be_rearmed(bool notificationFails)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var load = new EntryEditorLoadSession(f.Store, new(Today, EditId: entry.Id));
        var source = await load.ReadAsync(); load.Publish(source); var draft = source.Source!.Copy(); draft.Amount = 1500;
        if (notificationFails) f.Store.Changed += (_, _) => throw new IOException("Deliberate original notification refusal.");
        var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        Assert.True((await save.SaveAsync([draft], [], draft.Id, null, () => true)).Succeeded);
        Assert.True(source.Accepted); Assert.True(save.Accepted); Assert.Equal(notificationFails, save.NotificationFailed);
        Assert.Equal(1500, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source)
            .SaveAsync([draft], [], draft.Id, null, () => true));
        load.Retire(); await Assert.ThrowsAsync<InvalidOperationException>(() => load.ReadAsync());
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_save_time_retirement_rolls_back_the_pending_original_edit_and_preserves_source_authority()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var source = await ReadAsync(f, entry);
        var draft = source.Source!.Copy(); draft.Amount = 1500; var current = true; f.Time.BeforeRead = () => current = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync([draft], [], draft.Id, null, () => current));
        f.Time.BeforeRead = null; Assert.False(source.Accepted); Assert.False(save.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Mutated_original_in_memory_is_refused_before_the_writer()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var source = await ReadAsync(f, entry);
        var draft = source.Source!.Copy(); source.Source.Note = "Mutated original"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveAsync([draft], [], draft.Id, null, () => true));
        Assert.False(source.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task A_retained_form_waits_for_its_exact_pending_writer_before_refresh_or_known_acceptance(bool afterCommit)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f);
        var load = new EntryEditorLoadSession(f.Store, new(Today, EditId: entry.Id)); var source = await load.ReadAsync(); load.Publish(source);
        var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source); var draft = source.Source!.Copy(); draft.Amount = 1500;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var current = true;
        void Pause() { entered.TrySetResult(); release.Task.GetAwaiter().GetResult(); }
        if (afterCommit) f.Store.Changed += (_, _) => Pause(); else f.Time.BeforeRead = Pause;
        var pending = Task.Run(() => save.SaveAsync([draft], [], draft.Id, null, () => current), Ct);
        try
        {
            await entered.Task.WaitAsync(Ct); var idle = save.WaitForIdleAsync(); Assert.False(idle.IsCompleted);
            Assert.Same(idle, save.WaitForIdleAsync()); Assert.Equal(afterCommit, source.Accepted);
            Assert.Equal(!afterCommit, source.IsPending); load.Retire(); current = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() => load.ReadAsync());
        }
        finally { release.TrySetResult(); }
        if (afterCommit) Assert.True((await pending).Succeeded); else await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        f.Time.BeforeRead = null; await save.WaitForIdleAsync(); Assert.False(source.IsPending);
        Assert.Equal(afterCommit, source.Accepted); Assert.Equal(afterCommit, save.Accepted);
        Assert.Equal(afterCommit ? 1500 : 1234, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Original_transfer_edit_or_duplicate_preserves_both_fees_without_adopting_the_original_rows(bool duplicate)
    {
        using var f = new FlowFixture(); var from = new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var to = new Account { Name = "Original USD", CurrencyCode = "USD", OpeningDate = from.OpeningDate };
        await f.Store.SaveAccountAsync(from, Ct); await f.Store.SaveAccountAsync(to, Ct);
        var category = new Vafadar.Zanance.Core.Categories.Category { Name = "Original fees", Kind = Vafadar.Zanance.Core.Categories.CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(category, Ct);
        var entry = new LedgerEntry { AccountId = from.Id, ToAccountId = to.Id, Kind = EntryKind.Transfer,
            Date = from.OpeningDate, Amount = 10000, ToAmount = 9000, Title = "Original two-fee transfer" };
        var fee = EntryActions.SyncTransferFee(entry, null, 100, category.Id)!;
        var destination = EntryActions.SyncDestinationFee(entry, null, 200, category.Id)!;
        Assert.True((await f.Store.SaveEntriesAsync([entry, fee, destination], [], Ct)).Succeeded);
        var source = await f.Store.ReadEntryEditorSnapshotAsync(new(Today, EditId: duplicate ? null : entry.Id,
            DuplicateId: duplicate ? entry.Id : null), ct: Ct);
        Assert.Equal(3, source.Inputs.Related.Count); var originalRows = await GoalChooserFlowTests.SnapshotAsync(f);
        var draft = duplicate ? EntryActions.Duplicate(source.Source!, Today) : source.Source!.Copy(); draft.Amount = 12000; draft.ToAmount = 10000;
        var nextFee = EntryActions.SyncTransferFee(draft, duplicate ? null : source.Inputs.Related.Single(e => e.Id == fee.Id).Copy(), 100, category.Id)!;
        var nextDestination = EntryActions.SyncDestinationFee(draft, duplicate ? null : source.Inputs.Related.Single(e => e.Id == destination.Id).Copy(), 200, category.Id)!;
        var save = new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source);
        Assert.True((await save.SaveAsync([draft, nextFee, nextDestination], [], draft.Id, null, () => true)).Succeeded);
        Assert.True(source.Accepted); Assert.Equal(10000, source.Source!.Amount);
        var saved = (await f.Store.GetEntryAsync(draft.Id, Ct))!; Assert.Equal(12000, saved.Amount); Assert.Equal(10000, saved.ToAmount);
        Assert.Equal(100, (await f.Store.GetEntryAsync(nextFee.Id, Ct))!.Amount); Assert.Equal(200, (await f.Store.GetEntryAsync(nextDestination.Id, Ct))!.Amount);
        if (duplicate)
        {
            Assert.NotEqual(entry.Id, draft.Id); Assert.NotEqual(fee.Id, nextFee.Id); Assert.NotEqual(destination.Id, nextDestination.Id);
            Assert.Equal(6, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
            Assert.Equal(10000, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount); Assert.Equal(9000, (await f.Store.GetEntryAsync(entry.Id, Ct))!.ToAmount);
        }
        else Assert.Equal(3, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        Assert.NotEqual(originalRows, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_missing_selected_template_or_ambiguous_source_is_never_a_blank_replacement()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var missing = await f.Store.ReadEntryEditorSnapshotAsync(new(Today, TemplateId: Guid.NewGuid()), ct: Ct);
        Assert.False(missing.IsAvailable);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Store.ReadEntryEditorSnapshotAsync(new(Today, EditId: entry.Id, DuplicateId: entry.Id), ct: Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Store.ReadEntryEditorSnapshotAsync(new(Today, IncomeId: entry.Id), ct: Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_new_form_packet_cannot_replace_an_already_stored_primary_identity()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var source = await ReadAsync(f);
        var draft = entry.Copy(); draft.Amount = 9999; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EntryEditorSaveSession(f.Store, source.Inputs.Resources, source)
            .SaveAsync([draft], [], draft.Id, null, () => true));
        Assert.False(source.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task An_unpublished_original_read_refuses_a_foreign_file_before_the_native_connection_opens()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await SeedAsync(first);
        var factory = new Factory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, first.Time, new Access()); var session = new EntryEditorLoadSession(store, new(Today, EditId: entry.Id));
        await session.ReadAsync(); factory.Current = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); factory.ObserveOpening = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ReadAsync()); Assert.Equal(0, factory.NativeOpenings);
    }

    [Fact]
    public async Task A_foreign_template_handoff_is_refused_before_opening_the_other_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await SeedAsync(first);
        var template = EntryTemplate.From(entry, "Original handoff", true); await first.Store.SaveTemplateAsync(template, Ct);
        var factory = new Factory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, first.Time, new Access());
        var visit = await store.CreateTemplateVisitAsync(await store.GetQuickResourceReviewAsync(cancellationToken: Ct), template.Id, Ct);
        factory.Current = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); factory.ObserveOpening = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ResolveTemplateVisitAsync(visit, Ct)); Assert.Equal(0, factory.NativeOpenings);
    }

    [Theory][InlineData("file")][InlineData("identity")][InlineData("rights")][InlineData("source")]
    public async Task Original_detail_handoff_pins_the_editor_before_its_first_successful_source_lease(string change)
    {
        using var first = new FlowFixture(); using var foreign = new FlowFixture(); var entry = await SeedAsync(first);
        var factory = new Factory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var access = new Access();
        var store = new ZananceStore(factory, first.Time, access);
        var origin = new EntryEditorOrigin(await store.ReadEntryDetailSnapshotAsync(entry.Id, Ct));
        var request = new EntryEditorReadRequest(Today, EditId: change == "identity" ? Guid.NewGuid() : entry.Id);
        var session = new EntryEditorLoadSession(store, request, origin: origin);
        if (change == "file") factory.Current = foreign.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (change == "rights") access.Current = new(first.Directory.Combine("zanance.db"),
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        if (change == "source")
        {
            entry.Note = "Later original source note"; await first.Store.SaveEntryAsync(entry, Ct);
        }
        factory.ObserveOpening = true; var before = await GoalChooserFlowTests.SnapshotAsync(first);
        if (change == "source") await Assert.ThrowsAsync<EntryEditorSourceChangedException>(() => session.ReadAsync());
        else await Assert.ThrowsAsync<InvalidOperationException>(() => session.ReadAsync());
        if (change != "source") Assert.Equal(0, factory.NativeOpenings);
        Assert.Null(session.Review); Assert.False(session.HasPublished); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(first));
    }

    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal bool ObserveOpening;
        internal int NativeOpenings;
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            if (ObserveOpening) db.Database.GetDbConnection().StateChange += (_, args) =>
            { if (args.CurrentState == System.Data.ConnectionState.Open) Interlocked.Increment(ref NativeOpenings); };
            return db;
        }
        public ZananceDbContext CreateDbContext() => Observe(Current.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Observe(await Current.CreateDbContextAsync(ct));
    }
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string path) => Current;
    }

    [Theory][InlineData("read")][InlineData("refresh")][InlineData("save")]
    public async Task Same_imported_identity_in_a_different_file_cannot_receive_original_form_work(string action)
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await SeedAsync(first);
        var firstFactory = first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var secondFactory = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await secondFactory.CreateDbContextAsync(Ct))
        { db.Accounts.AddRange(await first.Store.GetAccountsAsync(cancellationToken: Ct)); db.Entries.Add(entry.Copy()); await db.SaveChangesAsync(Ct); }
        var factory = new Factory(firstFactory); var store = new ZananceStore(factory, first.Time, new Access());
        var load = new EntryEditorLoadSession(store, new(Today, EditId: entry.Id)); var source = await load.ReadAsync(); load.Publish(source);
        factory.Current = secondFactory; var draft = source.Source!.Copy(); draft.Amount = 2000;
        var before = await GoalChooserFlowTests.SnapshotAsync(second);
        Task Attempt() => action == "save" ? new EntryEditorSaveSession(store, source.Inputs.Resources, source).SaveAsync([draft], [], draft.Id, null, () => true)
            : action == "refresh" ? load.ValidateAsync() : load.ReadAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(Attempt);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(second)); Assert.False(source.Accepted);
    }
}
