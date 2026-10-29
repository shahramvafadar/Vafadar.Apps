using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.Core.Categories;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original category form flow, original graphics and explicit native interaction ports.</summary>
public sealed class CategoryEditorOriginalFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static CategoryEditorViewModel Model(FlowFixture f)
    {
        var model = new CategoryEditorViewModel(f.Store, f.Translator, f.Localization, f.Platform);
        model.SetActive(true); return model;
    }

    [Fact]
    public async Task Missing_requested_original_category_cannot_fall_through_to_creating_a_new_definition()
    {
        using var f = new FlowFixture(); var model = Model(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        model.Name = " D638 missing original must not become new ";
        await model.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract); model.SetActive(false);
    }

    [Fact]
    public async Task Changed_original_merge_target_during_native_consent_cannot_merge_the_old_selection()
    {
        using var f = new FlowFixture(); Guid source; Guid target; Guid other;
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            var categories = new[] { "Source", "Target", "Other" }.Select(name => new Category { Name = "D638 " + name }).ToArray();
            db.Categories.AddRange(categories); await db.SaveChangesAsync(Ct);
            (source, target, other) = (categories[0].Id, categories[1].Id, categories[2].Id);
        }
        var model = Model(f); await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = source });
        model.MergeTarget = model.MergeTargets.Single(choice => choice.Id == target);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () =>
        { model.MergeTarget = model.MergeTargets.Single(choice => choice.Id == other); return Task.FromResult(true); };
        await model.MergeCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(false);
    }
}
