using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original rule commands, native consent ports and raw drafts against real isolated SQLite.</summary>
public sealed class RulesOriginalFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static RulesViewModel Model(FlowFixture f, ZananceStore? store = null) => new(store ?? f.Store, f.Translator, f.Localization, f.Platform);
    private static async Task<Guid> PrepareAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var category = new Category { Name = "Original expense category", Kind = CategoryKind.Expense };
        db.Categories.Add(category);
        db.CategoryRules.Add(new CategoryRule { Match = "D637_KEEP", CategoryId = category.Id, Kind = category.Kind });
        await db.SaveChangesAsync(Ct); return category.Id;
    }

    [Fact]
    public async Task Departed_and_republished_original_rows_cannot_open_native_consent_or_write()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); var old = Assert.Single(model.Rules);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.SetActive(false); await model.DeleteCommand.ExecuteAsync(old);
        model.SetActive(true); await model.LoadAsync(); await model.DeleteCommand.ExecuteAsync(old);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(false);
    }

    [Fact]
    public async Task Native_delete_consent_retired_by_actual_departure_cannot_delete()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () => { model.SetActive(false); return Task.FromResult(true); };
        await model.DeleteCommand.ExecuteAsync(Assert.Single(model.Rules));
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.False(model.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Changed_original_source_during_native_consent_covers_without_deleting_or_clearing_draft()
    {
        using var f = new FlowFixture(); var id = await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); model.MatchText = "  raw unsaved draft  ";
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            await db.Categories.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.Name, "Changed category"), Ct);
            return true;
        };
        await model.DeleteCommand.ExecuteAsync(Assert.Single(model.Rules));
        Assert.True(model.Loading.HasFailed); Assert.False(model.Accepted); Assert.Equal("  raw unsaved draft  ", model.MatchText);
        Assert.Single(await f.Store.GetCategoryRulesAsync(Ct));
        await model.ReloadCommand.ExecuteAsync(null); Assert.True(model.CanInteract);
        Assert.Equal("Changed category", Assert.Single(model.Rules).CategoryName); Assert.Single(f.Platform.ConfirmationDialogs);
        model.SetActive(false);
    }

    [Fact]
    public async Task Read_failure_and_explicit_Retry_remain_bound_to_first_original_file_and_raw_draft()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await PrepareAsync(first); await PrepareAsync(second); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => Interlocked.Increment(ref calls) == 1 ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second) };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = Model(first, store); model.MatchText = "  preserve raw text  "; model.SetActive(true); await model.LoadAsync();
        Assert.True(model.Loading.HasFailed); await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract); Assert.Equal("  preserve raw text  ", model.MatchText);
        Assert.Empty(first.Platform.ConfirmationDialogs); model.SetActive(false);
    }

    [Fact]
    public async Task Reload_rebinds_same_selected_identity_and_keeps_missing_category_unselected_without_retargeting()
    {
        using var f = new FlowFixture(); var id = await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); var selected = model.SelectedCategory;
        model.MatchText = "  keep raw  "; await ((IThemeAware)model).RefreshThemeAsync();
        Assert.Equal(id, model.SelectedCategory!.Id); Assert.NotSame(selected, model.SelectedCategory);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Categories.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsArchived, true), Ct);
        await model.LoadAsync(); Assert.Null(model.SelectedCategory); Assert.Equal("  keep raw  ", model.MatchText);
        await model.AddCommand.ExecuteAsync(null); Assert.False(model.Accepted); Assert.Single(await f.Store.GetCategoryRulesAsync(Ct));
        model.SetActive(false);
    }

    [Fact]
    public async Task Save_retired_by_newer_raw_typing_rolls_back_without_trimming_or_clearing_new_draft()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); model.MatchText = "  D637_NEW  ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Time.BeforeRead = () => model.MatchText = "  newer typing  ";
        try { await model.AddCommand.ExecuteAsync(null); } finally { f.Time.BeforeRead = null; }
        Assert.False(model.Accepted); Assert.Equal("  newer typing  ", model.MatchText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); model.SetActive(false);
    }

    [Fact]
    public async Task Accepted_add_survives_failed_read_and_Retry_without_replaying_writer()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var failRead = false;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => failRead ? Task.FromException(new IOException("D637 accepted read failure")) : Task.CompletedTask };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = Model(f, store); model.SetActive(true); await model.LoadAsync(); model.MatchText = "  D637_NEW  ";
        store.Changed += (_, _) => failRead = true;
        await model.AddCommand.ExecuteAsync(null); Assert.True(model.Accepted); Assert.True(model.Loading.HasFailed);
        Assert.Equal(string.Empty, model.MatchText); var committed = await GoalChooserFlowTests.SnapshotAsync(f);
        failRead = false; await model.ReloadCommand.ExecuteAsync(null); Assert.True(model.CanInteract);
        Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(2, model.Rules.Count);
        model.SetActive(false);
    }

    [Fact]
    public async Task Accepted_delete_keeps_independent_Add_draft_and_Retry_never_repeats_consent()
    {
        using var f = new FlowFixture(); var id = await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); model.MatchText = "  independent draft  ";
        f.Platform.Confirmations.Enqueue(true); await model.DeleteCommand.ExecuteAsync(Assert.Single(model.Rules));
        Assert.True(model.Accepted); Assert.True(model.CanInteract); Assert.Empty(model.Rules);
        Assert.Equal("  independent draft  ", model.MatchText); Assert.Equal(id, model.SelectedCategory!.Id);
        var committed = await GoalChooserFlowTests.SnapshotAsync(f); await model.ReloadCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(false);
    }

    [Fact]
    public async Task Committed_add_notification_failure_keeps_receipt_clears_only_submitted_draft_and_Retry_reads()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); model.MatchText = "  D637_NOTIFY  ";
        f.Store.Changed += (_, _) => throw new IOException("D637 committed notification failure");
        await model.AddCommand.ExecuteAsync(null);
        Assert.True(model.Accepted); Assert.True(model.Loading.HasFailed); Assert.Equal(string.Empty, model.MatchText);
        var committed = await GoalChooserFlowTests.SnapshotAsync(f);
        await model.ReloadCommand.ExecuteAsync(null); Assert.True(model.CanInteract); Assert.Equal(2, model.Rules.Count);
        Assert.Equal(committed, await GoalChooserFlowTests.SnapshotAsync(f)); model.SetActive(false);
    }

    [Fact]
    public async Task Pending_original_read_retired_by_departure_cannot_publish_over_new_presence()
    {
        using var f = new FlowFixture(); await PrepareAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (Interlocked.Increment(ref calls) == 1) { entered.SetResult(); await release.Task.WaitAsync(ct); } } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = Model(f, store); model.SetActive(true); var pending = model.LoadAsync(); await entered.Task.WaitAsync(Ct);
        model.SetActive(false); model.SetActive(true); await model.LoadAsync(); Assert.True(model.CanInteract);
        var rows = model.Rules; release.TrySetResult(); await pending;
        Assert.Same(rows, model.Rules); Assert.True(model.CanInteract); Assert.Single(model.Rules); model.SetActive(false);
    }
}
