using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Importing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Readonly batch publication comes only from the actual original committed writer, never recaptured rights or a new file.</summary>
public sealed class CommittedImportReadbackTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string path) => Current;
    }
    private sealed class Owner
    {
        internal int Version = 1;
        internal void Check() { if (Version != 1) throw new OperationCanceledException("The original native generation retired."); }
    }
    private static void Rights(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free) => access.Current = new(f.Directory.Combine("zanance.db"),
        new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("46464646-1111-2222-3333-464646464646"))));
    private static async Task<(ZananceStore Store, DataFileWorkSnapshot Workspace, ImportReadReview Review, LedgerEntry Entry, Access Access, Owner Owner)> PrepareAsync(FlowFixture f)
    {
        await f.Store.GetSettingsAsync(Ct);
        var account = new Account { Name = "Original receipt funding", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var access = new Access(); var owner = new Owner();
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var workspace = await store.ReadDataFileWorkAsync(Ct);
        var review = await store.ReadImportReviewAsync(f.Directory.Combine("zanance.db"), Ct, reviewAccess: false, owner.Check);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234,
            Date = new(2026, 10, 9), Title = "Original committed receipt row", Note = "Original complete note\nsecond line", Tags = ["original", "receipt"] };
        review.Bind([entry]); Assert.Null(review.AcceptedWork); Rights(f, access);
        return (store, workspace, review, entry, access, owner);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Actual_committed_writer_preserves_readonly_receipt_before_optional_notification(bool notificationFailure)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        if (notificationFailure) s.Store.Changed += (_, _) => throw new IOException("Fictitious original post-commit notification failure.");
        ImportResult result;
        if (notificationFailure)
            result = (await Assert.ThrowsAsync<CommittedWriteNotificationException<ImportResult>>(() => s.Store.ImportReviewedAsync([s.Entry], [], s.Review, Ct))).Result;
        else result = await s.Store.ImportReviewedAsync([s.Entry], [], s.Review, Ct);
        Assert.Equal(1, result.Imported); var receipt = Assert.IsType<DataFileImportReceipt>(s.Review.AcceptedWork);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var current = await s.Store.ReadDataFileWorkAsync(Ct, previous: s.Workspace, acceptedImport: receipt);
        Assert.Equal(result.BatchId, Assert.Single(current.Batches).BatchId); current.EnsureCurrent();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("rights")] [InlineData("owner")] [InlineData("file")]
    public async Task Committed_receipt_cannot_borrow_new_rights_native_generation_or_foreign_workspace(string change)
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var s = await PrepareAsync(f);
        await s.Store.ImportReviewedAsync([s.Entry], [], s.Review, Ct);
        var receipt = Assert.IsType<DataFileImportReceipt>(s.Review.AcceptedWork); var previous = s.Workspace;
        if (change == "rights") Rights(f, s.Access, ProductPlan.Plus);
        else if (change == "owner") s.Owner.Version++; // A later appearance cannot rewind this original generation.
        else previous = await other.Store.ReadDataFileWorkAsync(Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        if (change == "owner") await Assert.ThrowsAsync<OperationCanceledException>(() => s.Store.ReadDataFileWorkAsync(Ct, previous: previous, acceptedImport: receipt));
        else await Assert.ThrowsAsync<InvalidOperationException>(() => s.Store.ReadDataFileWorkAsync(Ct, previous: previous, acceptedImport: receipt));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Actual_rolled_back_original_import_never_activates_readback_receipt()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER OwnedRejectImportJournal BEFORE INSERT ON ImportLinks BEGIN SELECT RAISE(ABORT, 'Owned original journal refusal'); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<DbUpdateException>(() => s.Store.ImportReviewedAsync([s.Entry], [], s.Review, Ct));
        Assert.Null(s.Review.AcceptedWork); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
