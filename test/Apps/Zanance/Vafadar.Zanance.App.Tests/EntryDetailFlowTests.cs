using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-426: complete original detail ownership and actual metadata/deletion writers over isolated SQLite.</summary>
[Trait("AT", "AT-426")]
public sealed class EntryDetailFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<LedgerEntry> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original detail account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var category = new Category { Name = "Original detail food", Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(category, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense,
            Amount = 1234, Date = new(2026, 10, 9), Title = "Original detail expense", Payee = "Original grocery",
            Review = ReviewState.Unreviewed, Tags = ["OriginalTag"] };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded); return entry;
    }

    private static async Task<EntryDetailSession> LoadAsync(FlowFixture f, Guid id)
    {
        var session = new EntryDetailSession(f.Store, id); session.Publish(await session.ReadAsync()); return session;
    }

    [Fact]
    public async Task Coherent_original_read_retains_group_refunds_previews_and_absent_settings_without_writes()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var sibling = entry.Copy();
        sibling = new() { AccountId = entry.AccountId, CategoryId = entry.CategoryId, Kind = EntryKind.Expense,
            Date = entry.Date, Amount = 500, Title = "Other original split part" };
        entry.GroupId = sibling.GroupId = Guid.NewGuid(); Assert.True((await f.Store.SaveEntriesAsync([entry, sibling], [], Ct)).Succeeded);
        var refund = new LedgerEntry { AccountId = entry.AccountId, CategoryId = entry.CategoryId, Kind = EntryKind.Refund,
            Date = entry.Date, Amount = 200, RefundOfId = entry.Id };
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var file = new EntryAttachment { EntryId = entry.Id, FileName = "Fictitious image.png", ContentType = "image/png", Data = [1, 2, 3, 4] };
        await f.Store.AddAttachmentAsync(file, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.ReadEntryDetailSnapshotAsync(entry.Id, Ct); await f.Store.ValidateEntryDetailSnapshotAsync(source, Ct);
        Assert.Equal(entry.Id, source.Entry!.Id); Assert.Equal(2, source.Inputs.Related.Count);
        Assert.Equal(refund.Id, Assert.Single(source.Inputs.Paybacks).Id);
        Assert.Equal(file.Id, Assert.Single(source.Inputs.Images).AttachmentId);
        Assert.Equal(file.Data, source.Inputs.Images[0].Data); Assert.Null(source.Inputs.Resources.Settings);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Missing_original_is_complete_and_cannot_create_a_rule_review_or_ledger_row()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.ReadEntryDetailSnapshotAsync(Guid.NewGuid(), Ct);
        Assert.Null(source.Entry); Assert.Empty(source.Inputs.Related); Assert.Null(source.Inputs.AttachmentReview);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.MarkEntryDetailReviewedAsync(source, () => true, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Retired_preparation_cannot_publish_or_replace_a_newer_complete_read()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = new EntryDetailSession(f.Store, entry.Id);
        var old = await session.ReadAsync(); session.Retire();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => session.Publish(old), Ct));
        var current = await session.ReadAsync(); session.Publish(current);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => session.Publish(old), Ct));
        Assert.Same(current, session.Displayed); Assert.True(session.CanInteract);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Mutated_original_source_or_preview_is_refused_before_any_action(bool image)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f);
        await f.Store.AddAttachmentAsync(new() { EntryId = entry.Id, FileName = "Fictitious.png", ContentType = "image/png", Data = [1, 2, 3] }, Ct);
        var session = await LoadAsync(f, entry.Id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        if (image) session.Displayed!.Inputs.Images[0].Data[0] = 9; else session.Displayed!.Entry!.Title = "Mutated retained source";
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.MarkReviewedAsync());
        Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("source")][InlineData("account")][InlineData("refund")][InlineData("document")][InlineData("rule")]
    public async Task Changed_persistent_packet_refuses_original_confirmation_without_deleting_any_rows(string change)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = await LoadAsync(f, entry.Id);
        var answer = new TaskCompletionSource<bool>(); var entered = new TaskCompletionSource();
        var work = session.DeleteAsync(() => { entered.SetResult(); return answer.Task; }); await entered.Task.WaitAsync(Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            if (change == "source") await db.Entries.Where(e => e.Id == entry.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.Note, "Later source note"), Ct);
            if (change == "account") await db.Accounts.Where(a => a.Id == entry.AccountId).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, "Later account name"), Ct);
        }
        if (change == "refund") Assert.True((await f.Store.SaveEntryAsync(new() { AccountId = entry.AccountId,
            CategoryId = entry.CategoryId, Kind = EntryKind.Refund, Date = entry.Date, Amount = 200, RefundOfId = entry.Id }, Ct)).Succeeded);
        if (change == "document") await f.Store.AddAttachmentAsync(new() { EntryId = entry.Id, FileName = "Later fictitious.pdf", ContentType = "application/pdf", Data = [7, 8] }, Ct);
        if (change == "rule") await f.Store.SaveCategoryRuleAsync(new() { Match = "Later pattern", CategoryId = entry.CategoryId!.Value, Kind = CategoryKind.Expense }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); answer.SetResult(true);
        await Assert.ThrowsAsync<EntryDetailSupersededException>(() => work);
        Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("review")][InlineData("rule")][InlineData("delete")]
    public async Task Known_original_commit_survives_notification_failure_and_cannot_be_replayed(string action)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = await LoadAsync(f, entry.Id);
        var source = session.Displayed!; var notifications = 0;
        f.Store.Changed += (_, _) => { notifications++; throw new InvalidOperationException("Deliberate original postcommit refusal."); };
        if (action == "delete")
        {
            await Assert.ThrowsAsync<CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>>>(() => session.DeleteAsync(() => Task.FromResult(true)));
            Assert.Equal(entry.Id, Assert.Single(session.Deleted!).Id); Assert.Null(await f.Store.GetEntryAsync(entry.Id, Ct));
        }
        else
        {
            await Assert.ThrowsAsync<CommittedWriteNotificationException>(() => action == "review" ? session.MarkReviewedAsync() : session.SaveRuleAsync());
            var saved = (await f.Store.GetEntryAsync(entry.Id, Ct))!; Assert.Equal(entry.Amount, saved.Amount);
            Assert.Equal(entry.Tags, saved.Tags);
            if (action == "review") Assert.Equal(ReviewState.Confirmed, saved.Review);
            else Assert.Equal(entry.Payee, Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)).Match);
        }
        Assert.True(source.Accepted); Assert.True(session.Accepted); Assert.False(session.CanInteract);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await session.MarkReviewedAsync(); await session.SaveRuleAsync(); await session.DeleteAsync(() => throw new InvalidOperationException("No second confirmation."));
        Assert.Equal(1, notifications); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Old_confirmed_delete_cannot_adopt_a_new_query_owner()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var other = await SeedAsync(f);
        var old = await LoadAsync(f, entry.Id); var answer = new TaskCompletionSource<bool>(); var entered = new TaskCompletionSource();
        var work = old.DeleteAsync(() => { entered.SetResult(); return answer.Task; }); await entered.Task.WaitAsync(Ct);
        old.Retire(); var current = await LoadAsync(f, other.Id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        answer.SetResult(true); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
        Assert.False(old.Accepted); Assert.True(current.CanInteract); Assert.Equal(other.Id, current.Displayed!.EntryId);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_save_time_retirement_rolls_back_only_the_pending_review_flag()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = await LoadAsync(f, entry.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Time.BeforeRead = session.Retire;
        await Assert.ThrowsAnyAsync<Exception>(() => session.MarkReviewedAsync()); f.Time.BeforeRead = null;
        Assert.False(session.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Changed_source_never_dispatches_an_old_navigation_target()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var session = await LoadAsync(f, entry.Id);
        entry.Title = "Later stored title"; await f.Store.SaveEntryAsync(entry, Ct); var dispatched = 0;
        await Assert.ThrowsAsync<EntryDetailSupersededException>(() => session.NavigateAsync(_ => { dispatched++; return Task.CompletedTask; }));
        Assert.Equal(0, dispatched); Assert.False(session.Accepted);
    }

    [Theory][InlineData("review")][InlineData("rule")]
    public async Task A_fresh_read_of_an_already_satisfied_metadata_action_does_not_write_again(string action)
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f);
        var first = await LoadAsync(f, entry.Id);
        if (action == "review") await first.MarkReviewedAsync(); else await first.SaveRuleAsync();
        var next = await LoadAsync(f, entry.Id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => action == "review" ? next.MarkReviewedAsync() : next.SaveRuleAsync());
        Assert.False(next.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Reviewed_delete_retains_original_group_refund_link_and_document_for_exact_undo()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); entry.GroupId = Guid.NewGuid();
        var sibling = new LedgerEntry { AccountId = entry.AccountId, CategoryId = entry.CategoryId, Kind = EntryKind.Expense,
            Date = entry.Date, Amount = 500, GroupId = entry.GroupId, Title = "Original second part" };
        Assert.True((await f.Store.SaveEntriesAsync([entry, sibling], [], Ct)).Succeeded);
        var refund = new LedgerEntry { AccountId = entry.AccountId, Kind = EntryKind.Refund,
            Date = entry.Date, Amount = 100, RefundOfId = entry.Id };
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var file = new EntryAttachment { EntryId = entry.Id, FileName = "Original.pdf", ContentType = "application/pdf", Data = [1, 2, 3] };
        await f.Store.AddAttachmentAsync(file, Ct); var original = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var session = await LoadAsync(f, entry.Id); await session.DeleteAsync(() => Task.FromResult(true));
        Assert.True(session.Accepted); Assert.Equal(2, session.Deleted!.Count);
        Assert.Null((await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
        Assert.Equal(file.Data, (await f.Store.GetAttachmentAsync(file.Id, Ct))!.Data);
        await f.Store.RestoreEntriesAsync(session.Deleted, Ct);
        var restored = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(original.OrderBy(e => e.Id).Select(e => (e.Id, e.Amount, e.GroupId, e.RefundOfId, e.Title, e.Review)),
            restored.OrderBy(e => e.Id).Select(e => (e.Id, e.Amount, e.GroupId, e.RefundOfId, e.Title, e.Review)));
        Assert.Equal(file.Data, (await f.Store.GetAttachmentAsync(file.Id, Ct))!.Data);
    }

    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Current.CreateDbContextAsync(ct);
    }

    [Theory][InlineData("read")][InlineData("review")][InlineData("delete")][InlineData("rule")][InlineData("navigate")]
    public async Task Same_imported_identity_in_another_file_cannot_adopt_original_detail_work(string action)
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var entry = await SeedAsync(first);
        var firstFactory = first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var secondFactory = second.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await secondFactory.CreateDbContextAsync(Ct))
        {
            db.Accounts.AddRange(await first.Store.GetAccountsAsync(cancellationToken: Ct));
            db.Categories.AddRange(await first.Store.GetCategoriesAsync(Ct)); db.Entries.Add(entry.Copy()); await db.SaveChangesAsync(Ct);
        }
        var factory = new Factory(firstFactory); var store = new ZananceStore(factory, first.Time, new Access());
        var session = new EntryDetailSession(store, entry.Id); session.Publish(await session.ReadAsync()); factory.Current = secondFactory;
        var before = await GoalChooserFlowTests.SnapshotAsync(second);
        Task Attempt() => action switch
        {
            "read" => session.ReadAsync(), "review" => session.MarkReviewedAsync(), "rule" => session.SaveRuleAsync(),
            "delete" => session.DeleteAsync(() => throw new InvalidOperationException("A foreign-file confirmation must never open.")),
            _ => session.NavigateAsync(_ => throw new InvalidOperationException("A foreign-file target must never dispatch.")),
        };
        await Assert.ThrowsAsync<InvalidOperationException>(Attempt);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(second));
        Assert.Equal(ReviewState.Unreviewed, (await first.Store.GetEntryAsync(entry.Id, Ct))!.Review);
    }

    [Fact]
    public async Task Activating_policy_retires_an_original_inactive_publication_before_native_action()
    {
        using var f = new FlowFixture(); var entry = await SeedAsync(f); var access = new Access();
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var session = new EntryDetailSession(store, entry.Id); session.Publish(await session.ReadAsync());
        access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.MarkReviewedAsync());
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
