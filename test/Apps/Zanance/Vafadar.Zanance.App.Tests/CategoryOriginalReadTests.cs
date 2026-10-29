using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Features.Categories;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual lightweight category reader and the complete original navigation packet over real SQLite.</summary>
public sealed class CategoryOriginalReadTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Empty_category_read_does_not_create_settings_or_any_other_source_row()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        await f.Store.ValidateCategorySnapshotAsync(source, Ct);
        Assert.Empty(source.Categories); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_overview_navigation_cannot_adopt_another_actual_file_with_the_same_category_identity()
    {
        using var original = new FlowFixture(); using var foreign = new FlowFixture();
        var category = new Category { Name = "D638 original navigation" };
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(original).CreateDbContextAsync(Ct))
        { db.Categories.Add(category); await db.SaveChangesAsync(Ct); }
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(foreign).CreateDbContextAsync(Ct))
        { db.Categories.Add(new Category(category.Id) { Name = "D638 foreign navigation" }); await db.SaveChangesAsync(Ct); }
        var source = await original.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var store = new ZananceStore(AccountDetailOriginalBoundaryTests.Factory(foreign), original.Time,
            original.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var first = await GoalChooserFlowTests.SnapshotAsync(original); var second = await GoalChooserFlowTests.SnapshotAsync(foreign);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReadCategorySnapshotAsync(cancellationToken: Ct, expected: source));
        Assert.Equal(first, await GoalChooserFlowTests.SnapshotAsync(original)); Assert.Equal(second, await GoalChooserFlowTests.SnapshotAsync(foreign));
    }

    [Fact]
    public async Task Actual_editor_keeps_requested_edit_identity_when_original_overview_file_read_is_refused()
    {
        using var original = new FlowFixture(); using var foreign = new FlowFixture();
        var category = new Category { Name = "D638 original edit identity" };
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(original).CreateDbContextAsync(Ct))
        { db.Categories.Add(category); await db.SaveChangesAsync(Ct); }
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(foreign).CreateDbContextAsync(Ct))
        { db.Categories.Add(new Category(category.Id) { Name = "D638 foreign edit identity" }); await db.SaveChangesAsync(Ct); }
        var source = await original.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        var store = new ZananceStore(AccountDetailOriginalBoundaryTests.Factory(foreign), original.Time,
            original.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new CategoryEditorViewModel(store, original.Translator, original.Localization, original.Platform);
        model.SetActive(true);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = category.Id, ["categorySource"] = source });
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract); Assert.Equal(string.Empty, model.Name);
        Assert.Equal(original.Translator["Category_EditTitle"], model.Title);
        model.SetActive(false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Complete_detached_or_persisted_category_changes_cannot_authorize_original_publication(bool detached)
    {
        using var f = new FlowFixture(); Guid id;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { var category = new Category { Name = "D638 full original category" }; id = category.Id; db.Categories.Add(category); await db.SaveChangesAsync(Ct); }
        var source = await f.Store.ReadCategorySnapshotAsync(cancellationToken: Ct);
        if (detached) Assert.Single(source.Categories).IsEssential = true;
        else
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            await db.Categories.Where(item => item.Id == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.SpendingType, SpendingType.Fixed), Ct);
        }
        var current = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateCategorySnapshotAsync(source, Ct));
        Assert.Equal(current, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
