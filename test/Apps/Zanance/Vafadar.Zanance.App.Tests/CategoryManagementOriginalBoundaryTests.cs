using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original category-management writer and actual-file continuation regressions on disposable SQLite.</summary>
public sealed class CategoryManagementOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(Guid First, Guid Second)> PrepareAsync(FlowFixture f, bool entry = false)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var first = new Category { Name = "D638 original first", Kind = CategoryKind.Expense, SortOrder = 10 };
        var second = new Category { Name = "D638 original second", Kind = CategoryKind.Expense, SortOrder = 20 };
        db.Categories.AddRange(first, second);
        if (entry)
        {
            var account = new Account { Name = "D638 fictitious account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            db.Accounts.Add(account);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, CategoryId = first.Id, Kind = EntryKind.Expense,
                Amount = 1234, Date = new(2026, 10, 2), Title = "D638 original expense", Payee = "Fictitious merchant", Tags = ["Original"] });
        }
        await db.SaveChangesAsync(Ct); return (first.Id, second.Id);
    }

    [Fact]
    public async Task Original_category_save_cannot_commit_after_actual_audit_retires_its_owner()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f);
        var source = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var category = (await f.Store.GetCategoriesAsync(Ct)).Single(x => x.Id == ids.First);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true;
        category.Name = "D638 changed after retirement";
        f.Time.BeforeRead = () => active = false;
        try { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.SaveReviewedCategoryAsync(source, category, category.Id, () => active, Ct)); }
        finally { f.Time.BeforeRead = null; }
        Assert.False(active); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_category_move_cannot_commit_after_actual_audit_retires_its_owner()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f);
        var source = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true;
        f.Time.BeforeRead = () => active = false;
        try { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.MoveReviewedCategoryAsync(source, ids.Second, -1, () => active, Ct)); }
        finally { f.Time.BeforeRead = null; }
        Assert.False(active); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_merge_cannot_commit_after_actual_SQL_entry_update_retires_its_owner()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f, entry: true); var active = true;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER d638_retire_merge_owner AFTER UPDATE OF CategoryId ON Entries BEGIN SELECT d638_retire_merge_owner(); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var factory = new NativeRetirementFactory(AccountDetailOriginalBoundaryTests.Factory(f), () => active = false);
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var source = await store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var review = await store.ReadCategoryMergeReviewAsync(source, ids.First, ids.Second, () => active, Ct);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.MergeReviewedCategoryAsync(review, () => active, Ct));
        Assert.False(active); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_move_continuation_cannot_adopt_another_files_sort_order()
    {
        using var original = new FlowFixture(); using var foreign = new FlowFixture();
        var ids = await PrepareAsync(original);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(foreign).CreateDbContextAsync(Ct))
        {
            db.Categories.Add(new Category(ids.Second) { Name = "D638 foreign second", SortOrder = 999, Kind = CategoryKind.Expense });
            await db.SaveChangesAsync(Ct);
        }
        var calls = 0;
        var source = await original.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(original))
        { Select = () => Interlocked.Increment(ref calls) == 1 ? AccountDetailOriginalBoundaryTests.Factory(original) : AccountDetailOriginalBoundaryTests.Factory(foreign) };
        var store = new ZananceStore(factory, original.Time, original.Services.GetRequiredService<ICommercialWriteAccessSource>());
        // The corrected original continuation receives its result while the same actual file's writer is held.
        var receipt = await store.MoveReviewedCategoryAsync(source, ids.Second, -1, () => true, Ct);
        var actual = (await original.Store.GetCategoriesAsync(Ct)).Single(x => x.Id == ids.Second);
        Assert.Equal(actual.SortOrder, receipt.SortOrder);
        Assert.Equal(actual.SortOrder, receipt.Source.Categories.Single(x => x.Id == ids.Second).SortOrder);
        Assert.Equal(1, calls);
    }

    private sealed class NativeRetirementFactory(IDbContextFactory<ZananceDbContext> original, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => Bind(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Bind(await original.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Bind(ZananceDbContext db)
        {
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("d638_retire_merge_owner", () => { retire(); return 0; }); return db;
        }
    }
}
