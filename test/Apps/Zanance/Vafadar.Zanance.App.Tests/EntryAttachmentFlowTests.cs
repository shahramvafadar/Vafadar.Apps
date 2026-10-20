using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-424: original native picker/confirmation owners against real isolated SQLite attachments.</summary>
[Trait("AT", "AT-424")]
public sealed class EntryAttachmentFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static EntryAttachment File(Guid entryId = default) => new()
    { EntryId = entryId, FileName = "Fictitious-original.pdf", ContentType = "application/pdf", Data = [1, 2, 3, 4] };

    private static async Task<LedgerEntry> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original attachment account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense,
            Amount = 1234, Title = "Original attachment purchase", Note = "Original unchanged financial source" };
        Assert.True((await f.Store.SaveEntryAsync(entry, cancellationToken: Ct)).Succeeded);
        return (await f.Store.GetEntryAsync(entry.Id, Ct))!;
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Late_picker_cannot_follow_a_new_query_or_departed_detail(bool newQuery)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var other = await SeedAsync(f);
        var session = new EntryAttachmentSession(f.Store); await session.LoadAsync(entry);
        var pending = new TaskCompletionSource<EntryAttachment?>(); var work = session.AddAsync(() => pending.Task);
        if (newQuery) await session.LoadAsync(other); else session.Retire();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(File());
        await Assert.ThrowsAsync<InvalidOperationException>(() => work);
        Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        if (newQuery) Assert.Equal(other.Id, session.Review!.Inputs.Entry.Id); else Assert.Null(session.Review);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Picker_refuses_changed_or_deleted_original_entry_without_changing_any_other_rows(bool deletion)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryAttachmentSession(f.Store);
        await session.LoadAsync(entry); var pending = new TaskCompletionSource<EntryAttachment?>();
        var work = session.AddAsync(() => pending.Task);
        if (deletion) await f.Store.DeleteEntryAsync(entry.Id, Ct);
        else { entry.Note = "Later original edit"; await f.Store.SaveEntryAsync(entry, cancellationToken: Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(File());
        await Assert.ThrowsAsync<InvalidOperationException>(() => work);
        Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Cancelled_picker_and_second_pending_invocation_create_no_rows()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryAttachmentSession(f.Store);
        await session.LoadAsync(entry); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = new TaskCompletionSource<EntryAttachment?>(); var work = session.AddAsync(() => pending.Task);
        await session.AddAsync(() => throw new InvalidOperationException("A second picker must not open."));
        pending.SetResult(null); await work; Assert.False(session.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Confirmed_delete_refuses_changed_document_bytes_or_retired_owner(bool retire)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var file = File(entry.Id);
        await f.Store.AddAttachmentAsync(file, Ct); var session = new EntryAttachmentSession(f.Store); await session.LoadAsync(entry);
        var pending = new TaskCompletionSource<bool>(); var entered = new TaskCompletionSource();
        var work = session.DeleteAsync(file.Id, _ => { entered.SetResult(); return pending.Task; });
        await entered.Task.WaitAsync(Ct);
        if (retire) session.Retire();
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await db.Attachments.Where(a => a.Id == file.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Data, new byte[] { 8, 7, 6, 5 }), Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => work);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(session.Accepted);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Known_add_or_delete_commit_survives_notification_failure_without_replay(bool deletion)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var file = File(entry.Id);
        if (deletion) await f.Store.AddAttachmentAsync(file, Ct);
        var session = new EntryAttachmentSession(f.Store); await session.LoadAsync(entry);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Deliberate postcommit observer failure.");
        await Assert.ThrowsAsync<CommittedWriteNotificationException>(() => deletion
            ? session.DeleteAsync(file.Id, _ => Task.FromResult(true)) : session.AddAsync(() => Task.FromResult<EntryAttachment?>(file)));
        Assert.True(session.Accepted); Assert.Null(session.Review);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.AddAsync(() => throw new InvalidOperationException("No second native picker.")));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(deletion ? 0 : 1, (await f.Store.GetAttachmentsAsync(entry.Id, Ct)).Count);
        Assert.Equal(entry.Amount, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
    }

    [Fact]
    public async Task Original_add_read_and_delete_preserve_complete_document_and_financial_source()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryAttachmentSession(f.Store);
        await session.LoadAsync(entry, await f.Store.GetQuickResourceReviewAsync(entry.Id, Ct));
        var file = File(); await session.AddAsync(() => Task.FromResult<EntryAttachment?>(file));
        Assert.True(session.Accepted); await session.LoadAsync(entry); var read = await session.ReadAsync(file.Id);
        Assert.Equal(entry.Id, read.EntryId); Assert.Equal(file.FileName, read.FileName); Assert.Equal(file.ContentType, read.ContentType); Assert.Equal(file.Data, read.Data);
        await session.DeleteAsync(file.Id, name => Task.FromResult(name == file.FileName)); Assert.True(session.Accepted);
        Assert.Empty(await f.Store.GetAttachmentsAsync(entry.Id, Ct)); Assert.Equal(entry.Note, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Note);
    }

    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Current.CreateDbContextAsync(ct);
    }

    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }

    [Fact]
    public async Task Same_imported_entry_identity_in_another_profile_cannot_receive_the_pending_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await SeedAsync(first);
        var firstFactory = first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var secondFactory = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await secondFactory.CreateDbContextAsync(Ct))
        {
            db.Accounts.AddRange(await first.Store.GetAccountsAsync(cancellationToken: Ct));
            db.Entries.Add(entry.Copy()); await db.SaveChangesAsync(Ct);
        }
        var factory = new SwitchingFactory(firstFactory); var store = new ZananceStore(factory, first.Time, new Access());
        var session = new EntryAttachmentSession(store); await session.LoadAsync(entry);
        var pending = new TaskCompletionSource<EntryAttachment?>(); var work = session.AddAsync(() => pending.Task);
        factory.Current = secondFactory; var before = await GoalChooserFlowTests.SnapshotAsync(second);
        pending.SetResult(File()); await Assert.ThrowsAsync<InvalidOperationException>(() => work);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(second));
        Assert.Empty(await first.Store.GetAttachmentsAsync(entry.Id, Ct)); Assert.False(session.Accepted);
        var displayed = await first.Store.GetQuickResourceReviewAsync(entry.Id, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.LoadAsync(entry, displayed));
        Assert.Null(session.Review); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(second));
    }

    [Fact]
    public async Task Activation_during_native_pick_retires_even_an_original_inactive_review()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var access = new Access();
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var session = new EntryAttachmentSession(store); await session.LoadAsync(entry);
        var pending = new TaskCompletionSource<EntryAttachment?>(); var work = session.AddAsync(() => pending.Task);
        access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); pending.SetResult(File());
        await Assert.ThrowsAsync<InvalidOperationException>(() => work);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Retirement_from_the_actual_save_timestamp_rolls_back_the_document_insert()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryAttachmentSession(f.Store);
        await session.LoadAsync(entry); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = session.Retire;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.AddAsync(() => Task.FromResult<EntryAttachment?>(File())));
        f.Time.BeforeRead = null;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(session.Accepted);
    }
}
