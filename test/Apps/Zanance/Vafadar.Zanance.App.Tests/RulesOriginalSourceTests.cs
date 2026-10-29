using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original rule rows must retain the categories and file against which they were read.</summary>
public sealed class RulesOriginalSourceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<Guid> PrepareAsync(FlowFixture f, string name, string match, Guid? id = null)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var category = new Category { Name = name, Kind = CategoryKind.Expense };
        db.Categories.Add(category);
        if (id is { } original) db.Entry(category).Property(c => c.Id).CurrentValue = original;
        db.CategoryRules.Add(new CategoryRule { Match = match, CategoryId = category.Id, Kind = CategoryKind.Expense });
        await db.SaveChangesAsync(Ct); return category.Id;
    }

    [Fact]
    public async Task Original_rule_rows_remain_with_their_original_category_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        var id = await PrepareAsync(first, "Original rule category", "D637_ORIGINAL");
        await PrepareAsync(second, "Foreign rule category", "D637_FOREIGN", id);
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => Interlocked.Increment(ref calls) == 1 ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second) };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        // The actual published two-call failure is retained separately; this original reader owns one lease.
        var source = await store.ReadCategoryRulesSnapshotAsync(cancellationToken: Ct);
        var categories = source.Categories; var rules = source.Rules;
        Assert.Equal(1, calls);
        Assert.Equal("Original rule category", Assert.Single(categories, c => c.Id == id).Name);
        Assert.Equal("D637_ORIGINAL", Assert.Single(rules).Match);
    }

    [Fact]
    public async Task An_empty_original_rule_read_does_not_create_settings_or_financial_data()
    {
        using var f = new FlowFixture();
        var source = await f.Store.ReadCategoryRulesSnapshotAsync(cancellationToken: Ct);
        Assert.Empty(source.Rules);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Changed_detached_or_persisted_original_rule_inputs_cannot_authorize_publication()
    {
        using var f = new FlowFixture(); await PrepareAsync(f, "Original rule category", "D637_ORIGINAL");
        var source = await f.Store.ReadCategoryRulesSnapshotAsync(cancellationToken: Ct);
        await f.Store.ValidateCategoryRulesSnapshotAsync(source, Ct);
        source.Rules[0].Match = "Detached replacement";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateCategoryRulesSnapshotAsync(source, Ct));
        source.Rules[0].Match = "D637_ORIGINAL";
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        await db.Categories.Where(c => c.Id == source.Rules[0].CategoryId).ExecuteUpdateAsync(set => set.SetProperty(c => c.Name, "New stored name"), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateCategoryRulesSnapshotAsync(source, Ct));
    }
}
