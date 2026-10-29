using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original form continuations, raw fields and accepted metadata with real isolated SQLite.</summary>
public sealed class CategoryEditorContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(Guid First, Guid Second)> PrepareAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var first = new Category { Name = "D638 first", Color = "#2E7D32", Icon = "Tag", SortOrder = 10 };
        var second = new Category { Name = "D638 second", Color = "#00838F", Icon = "Home", SortOrder = 20 };
        db.Categories.AddRange(first, second); await db.SaveChangesAsync(Ct); return (first.Id, second.Id);
    }
    private static async Task<CategoryEditorViewModel> OpenAsync(FlowFixture f, Guid id, ZananceStore? store = null)
    {
        var model = new CategoryEditorViewModel(store ?? f.Store, f.Translator, f.Localization, f.Platform);
        model.SetActive(true); await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = id });
        Assert.True(model.CanInteract); return model;
    }

    [Fact]
    public async Task Actual_form_save_retired_at_audit_rolls_back_and_keeps_exact_raw_name()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.Name = "  D638 raw name  "; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = () => model.SetActive(false);
        try { await model.SaveCommand.ExecuteAsync(null); } finally { f.Time.BeforeRead = null; }
        Assert.False(model.Accepted); Assert.Equal("  D638 raw name  ", model.Name); Assert.True(model.IsDirty);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_archive_consent_retired_by_departure_does_not_write_or_leave_another_page()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.Name = "  D638 independent raw draft  "; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () => { model.SetActive(false); return Task.FromResult(true); };
        await model.ToggleArchiveCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes); Assert.False(model.Accepted);
        Assert.Equal("  D638 independent raw draft  ", model.Name); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_same_writer_move_receipt_survives_foreign_read_failure_without_raw_draft_loss_or_move_replay()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var ids = await PrepareAsync(f);
        await PrepareAsync(other); var calls = 0; var useOriginal = false;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => useOriginal || Interlocked.Increment(ref calls) <= 3 ? AccountDetailOriginalBoundaryTests.Factory(f) : AccountDetailOriginalBoundaryTests.Factory(other) };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = await OpenAsync(f, ids.Second, store); model.Name = "  D638 unsaved move name  ";
        await model.MoveUpCommand.ExecuteAsync(null);
        Assert.True(model.Accepted); Assert.True(model.Loading.HasFailed); Assert.True(model.IsDirty);
        Assert.Equal("  D638 unsaved move name  ", model.Name);
        Assert.Equal(10, (await f.Store.GetCategoriesAsync(Ct)).Single(item => item.Id == ids.Second).SortOrder);
        var committed = await GoalChooserFlowTests.SnapshotAsync(f); useOriginal = true;
        await model.ReloadCommand.ExecuteAsync(null); Assert.True(model.CanInteract); Assert.True(model.IsDirty);
        Assert.Equal("  D638 unsaved move name  ", model.Name); Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes); model.SetActive(false);
    }

    [Fact]
    public async Task Accepted_save_notification_failure_retains_same_writer_receipt_and_Retry_never_saves_again()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.Name = "  D638 accepted name  ";
        f.Store.Changed += (_, _) => throw new IOException("D638 post-commit notification failure");
        await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Accepted); Assert.True(model.Loading.HasFailed); Assert.False(model.IsDirty);
        Assert.Equal("  D638 accepted name  ", model.Name);
        var committed = await GoalChooserFlowTests.SnapshotAsync(f);
        await model.ReloadCommand.ExecuteAsync(null); Assert.Single(f.Platform.Routes);
        Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("D638 accepted name", (await f.Store.GetCategoriesAsync(Ct)).Single(item => item.Id == ids.First).Name);
        model.SetActive(false);
    }

    [Fact]
    public async Task Typing_a_revert_after_commit_keeps_a_dirty_new_draft_against_the_submitted_saved_baseline()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        var originalName = model.Name; model.Name = "  D638 submitted change  ";
        f.Store.Changed += (_, _) => model.Name = originalName;
        await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Accepted); Assert.True(model.CanInteract); Assert.True(model.IsDirty);
        Assert.Equal(originalName, model.Name); Assert.Empty(f.Platform.Routes);
        Assert.Equal("D638 submitted change", (await f.Store.GetCategoriesAsync(Ct)).Single(item => item.Id == ids.First).Name);
        model.SetActive(false);
    }

    [Fact]
    public async Task A_changed_original_definition_cannot_be_silently_rebased_under_an_old_raw_draft()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.Name = "  D638 keep this draft  ";
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Categories.Where(item => item.Id == ids.First).ExecuteUpdateAsync(set => set.SetProperty(item => item.Name, "External current name"), Ct);
        var current = await GoalChooserFlowTests.SnapshotAsync(f);
        await model.SaveCommand.ExecuteAsync(null); Assert.False(model.Accepted); Assert.True(model.Loading.HasFailed);
        await model.ReloadCommand.ExecuteAsync(null); Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract);
        Assert.Equal("  D638 keep this draft  ", model.Name); Assert.True(model.IsDirty); Assert.Empty(f.Platform.Routes);
        Assert.Equal(current, await GoalChooserFlowTests.SnapshotAsync(f)); model.SetActive(false);
    }

    [Fact]
    public async Task Old_native_swatch_and_parent_instances_cannot_replace_the_current_display_or_raw_draft()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        var oldIcon = model.Icons.Single(item => item.Key == "Home"); var oldParent = model.Parent;
        model.Name = "  D638 keep raw display draft  "; await ((IThemeAware)model).RefreshThemeAsync();
        Assert.NotSame(oldParent, model.Parent); Assert.Equal(oldParent!.Id, model.Parent!.Id);
        model.SelectIconCommand.Execute(oldIcon);
        Assert.True(model.Icons.Single(item => item.Key == "Tag").IsSelected);
        Assert.Equal("  D638 keep raw display draft  ", model.Name); Assert.True(model.IsDirty); model.SetActive(false);
    }

    [Fact]
    public async Task Newly_recorded_ledger_reference_during_merge_consent_refuses_the_old_graph_without_touching_money()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.MergeTarget = model.MergeTargets.Single(item => item.Id == ids.Second); string? afterExternalWrite = null;
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            var account = new Account { Name = "D638 external fictitious account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            db.Accounts.Add(account);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, CategoryId = ids.First, Kind = EntryKind.Expense,
                Amount = 500, Date = new(2026, 10, 2), Title = "D638 independent later row", Tags = ["Preserve"] });
            await db.SaveChangesAsync(Ct); afterExternalWrite = await GoalChooserFlowTests.SnapshotAsync(f); return true;
        };
        await model.MergeCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.False(model.Accepted); Assert.True(model.Loading.HasFailed); Assert.Empty(f.Platform.Routes);
        Assert.NotNull(afterExternalWrite); Assert.Equal(afterExternalWrite, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(false);
    }

    [Fact]
    public async Task Retained_archived_parent_is_not_silently_replaced_by_none_during_an_original_name_save()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            await db.Categories.Where(item => item.Id == ids.First).ExecuteUpdateAsync(set => set.SetProperty(item => item.IsArchived, true), Ct);
            await db.Categories.Where(item => item.Id == ids.Second).ExecuteUpdateAsync(set => set.SetProperty(item => item.ParentId, ids.First), Ct);
        }
        var model = await OpenAsync(f, ids.Second); Assert.Equal(ids.First, model.Parent!.Id);
        model.Name = "D638 child name only"; await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Accepted); Assert.Equal(ids.First, (await f.Store.GetCategoriesAsync(Ct)).Single(item => item.Id == ids.Second).ParentId);
        model.SetActive(false);
    }

    [Fact]
    public async Task Discard_consent_retired_by_actual_departure_cannot_close_another_page()
    {
        using var f = new FlowFixture(); var ids = await PrepareAsync(f); var model = await OpenAsync(f, ids.First);
        model.Name = "D638 unsaved cancel"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () => { model.SetActive(false); return Task.FromResult(true); };
        await model.CancelCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
