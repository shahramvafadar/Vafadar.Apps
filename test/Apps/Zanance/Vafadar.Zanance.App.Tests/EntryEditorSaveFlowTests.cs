using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-425: actual original editor acceptance and receipt handoff through real isolated SQLite writers.</summary>
[Trait("AT", "AT-425")]
public sealed class EntryEditorSaveFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<LedgerEntry> DraftAsync(FlowFixture f)
    {
        var account = new Account { Name = "Fictitious editor cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        return new() { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 9), Amount = 1234, Title = "Original receipt purchase" };
    }
    private static EntryAttachment Document() => new() { FileName = "Fictitious-original.pdf", ContentType = "application/pdf", Data = [1, 2, 3, 4] };

    [Fact]
    public async Task Original_document_and_finance_keep_detached_inputs_and_cannot_be_submitted_twice()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var document = Document();
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        f.Store.Changed += (_, _) => { entry.Title = "Later typing"; document.Data[0] = 9; };
        Assert.True((await session.SaveAsync([entry], [], entry.Id, document, () => true)).Succeeded);
        Assert.True(session.Accepted); Assert.False(session.AttachmentFailed);
        Assert.Equal("Original receipt purchase", (await f.Store.GetEntryAsync(entry.Id, Ct))!.Title);
        var file = Assert.Single(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, (await f.Store.GetAttachmentAsync(file.Id, Ct))!.Data);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, document, () => true));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Financial_or_document_notification_failure_retains_both_known_commits(bool onlyDocument)
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var calls = 0;
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        f.Store.Changed += (_, _) => { calls++; if (!onlyDocument || calls == 2) throw new InvalidOperationException("Deliberate original postcommit failure."); };
        Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => true)).Succeeded);
        Assert.True(session.Accepted); Assert.True(session.NotificationFailed); Assert.False(session.AttachmentFailed);
        Assert.Equal(1234, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
        Assert.Single(await f.Store.GetAttachmentsAsync(entry.Id, Ct)); Assert.Equal(2, calls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, null, () => true));
        Assert.Equal(2, calls);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Departed_owner_or_changed_saved_source_keeps_money_but_refuses_document(bool changedSource)
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var current = true;
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        f.Store.Changed += (_, _) =>
        {
            if (!changedSource) { current = false; return; }
            using var db = factory.CreateDbContext();
            db.Entries.Where(e => e.Id == entry.Id).ExecuteUpdate(s => s.SetProperty(e => e.Note, "Later original correction"));
        };
        Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => current)).Succeeded);
        Assert.True(session.Accepted); Assert.True(session.AttachmentFailed);
        Assert.Empty(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
        Assert.Equal(1234, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, Document(), () => true));
    }

    [Fact]
    public async Task Invalid_document_is_separate_from_accepted_finance_and_never_rearms_save()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var file = Document(); file.Data = [];
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        Assert.True((await session.SaveAsync([entry], [], entry.Id, file, () => true)).Succeeded);
        Assert.True(session.Accepted); Assert.True(session.AttachmentFailed);
        Assert.Equal(1234, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
        Assert.Empty(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
    }

    [Fact]
    public async Task Retirement_at_the_actual_financial_save_rolls_back_before_original_retry()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var current = true;
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Time.BeforeRead = () => current = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, Document(), () => current));
        f.Time.BeforeRead = null; Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        current = true;
        Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => current)).Succeeded);
        Assert.True(session.Accepted); Assert.Single(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
    }

    [Fact]
    public async Task Invalid_finance_saves_no_document_and_retains_only_uncommitted_retry()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); entry.Amount = 0;
        var session = new EntryEditorSaveSession(f.Store, await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        Assert.False((await session.SaveAsync([entry], [], entry.Id, Document(), () => true)).Succeeded);
        Assert.False(session.Accepted); Assert.Null(await f.Store.GetEntryAsync(entry.Id, Ct));
        Assert.Empty(await f.Store.GetAttachmentsAsync(entry.Id, Ct)); entry.Amount = 1234;
        Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => true)).Succeeded);
        Assert.True(session.Accepted);
    }

    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }

    [Fact]
    public async Task Policy_activation_after_financial_commit_refuses_the_old_document_rights()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f); var access = new Access();
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var session = new EntryEditorSaveSession(store, await store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        store.Changed += (_, _) => access.Current = new(f.Directory.Combine("zanance.db"),
            new(Core.Commerce.ProductPlan.Free, new(Core.Commerce.EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => true)).Succeeded);
        Assert.True(session.Accepted); Assert.True(session.AttachmentFailed);
        Assert.Equal(1234, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
        Assert.Empty(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
    }

    private sealed class WaitingFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Wait;
        internal readonly TaskCompletionSource Entered = new();
        internal readonly TaskCompletionSource Release = new();
        public ZananceDbContext CreateDbContext() => original.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            if (Wait) { Entered.TrySetResult(); await Release.Task.WaitAsync(ct); }
            return await original.CreateDbContextAsync(ct);
        }
    }

    [Fact]
    public async Task A_second_pending_save_cannot_open_another_writer_or_document_handoff()
    {
        using var f = new FlowFixture(); var entry = await DraftAsync(f);
        var factory = new WaitingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, f.Time, new Access());
        var session = new EntryEditorSaveSession(store, await store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        factory.Wait = true; var work = session.SaveAsync([entry], [], entry.Id, Document(), () => true);
        await factory.Entered.Task.WaitAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, Document(), () => true));
        factory.Wait = false; factory.Release.SetResult(); Assert.True((await work).Succeeded);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Single(await f.Store.GetAttachmentsAsync(entry.Id, Ct));
    }

    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> first) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = first;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Current.CreateDbContextAsync(ct);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Profile_switch_before_save_or_after_financial_commit_never_adopts_same_id_destination(bool afterCommit)
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await DraftAsync(first);
        var firstFactory = first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var secondFactory = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await secondFactory.CreateDbContextAsync(Ct))
        {
            db.Accounts.AddRange(await first.Store.GetAccountsAsync(cancellationToken: Ct));
            db.Entries.Add(entry.Copy()); await db.SaveChangesAsync(Ct);
        }
        var factory = new SwitchingFactory(firstFactory);
        var store = new ZananceStore(factory, first.Time, new Access());
        var session = new EntryEditorSaveSession(store, await store.GetQuickResourceReviewAsync(cancellationToken: Ct));
        var before = await GoalChooserFlowTests.SnapshotAsync(second);
        if (afterCommit) store.Changed += (_, _) => factory.Current = secondFactory;
        else factory.Current = secondFactory;
        if (afterCommit)
        {
            Assert.True((await session.SaveAsync([entry], [], entry.Id, Document(), () => true)).Succeeded);
            Assert.True(session.Accepted); Assert.True(session.AttachmentFailed);
            Assert.Equal(1234, (await first.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync([entry], [], entry.Id, Document(), () => true));
            Assert.False(session.Accepted); Assert.Null(await first.Store.GetEntryAsync(entry.Id, Ct));
        }
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(second));
        Assert.Empty(await first.Store.GetAttachmentsAsync(entry.Id, Ct));
    }
}
