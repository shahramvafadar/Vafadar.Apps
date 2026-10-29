using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original rule writer retirement at SaveChanges and native SQLite deletion.</summary>
public sealed class RulesOriginalWriterBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<CategoryRulesReadSnapshot> PrepareAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var category = new Category { Name = "Original rule writer category", Kind = CategoryKind.Expense };
        db.Categories.Add(category);
        db.CategoryRules.Add(new CategoryRule { Match = "D637_KEEP", CategoryId = category.Id, Kind = category.Kind });
        await db.SaveChangesAsync(Ct);
        return await f.Store.ReadCategoryRulesSnapshotAsync(cancellationToken: Ct);
    }

    [Fact]
    public async Task Retirement_during_original_rule_SaveChanges_preserves_the_complete_source()
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var active = true;
        f.Time.BeforeRead = () => active = false;
        try
        {
            // Supply original page authority to the same replacement writer; actual SaveChanges must roll back.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.SaveReviewedCategoryRuleAsync(source,
                new CategoryRule { Match = " D637_NEW ", CategoryId = source.Rules[0].CategoryId, Kind = CategoryKind.Expense },
                () => active, Ct));
        }
        finally { f.Time.BeforeRead = null; }
        Assert.False(active); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Retirement_during_original_rule_SQL_deletion_preserves_the_complete_source()
    {
        using var f = new FlowFixture(); var source = await PrepareAsync(f); var active = true;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER d637_retire_rule_owner AFTER DELETE ON CategoryRules BEGIN SELECT d637_retire_rule_owner(); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var factory = new NativeRetirementFactory(AccountDetailOriginalBoundaryTests.Factory(f), () => active = false);
        var writer = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.DeleteReviewedCategoryRuleAsync(source,
            source.Rules[0].Id, () => active, Ct));
        Assert.False(active); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    private sealed class NativeRetirementFactory(IDbContextFactory<ZananceDbContext> original, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => Bind(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Bind(await original.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Bind(ZananceDbContext db)
        {
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("d637_retire_rule_owner", () => { retire(); return 0; });
            return db;
        }
    }
}
