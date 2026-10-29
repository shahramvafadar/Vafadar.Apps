using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Localization.Formatting;
using Vafadar.Testing;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original checklist view model with native navigation ports and disposable real SQLite sources.</summary>
public sealed class PeriodReviewOriginalFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static PeriodReviewViewModel Model(FlowFixture f, ZananceStore? store = null)
    {
        var backup = new BackupService([], new StaticAppEnvironment("pro.vafadar.zanance", "Zanance",
            new(0, 1, 0), "Fictitious device", "Tests"), f.Preferences, f.Time, new());
        return new(store ?? f.Store, backup, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, f.Platform);
    }
    private static async Task PrepareAsync(FlowFixture f)
    {
        await f.Store.GetSettingsAsync(Ct);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        db.Accounts.Add(new Account { Name = "Original review account", CurrencyCode = "EUR",
            OpeningDate = new(2026, 1, 1), OpeningBalance = 10000 });
        await db.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Retired_original_row_cannot_navigate_or_save_a_toggle_after_departure_or_new_publication()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); Assert.True(model.CanInteract);
        var old = model.Steps[0]; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.SetActive(false); old.IsDone = true; await old.Open();
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(true); await model.LoadAsync(); Assert.True(model.CanInteract);
        old.IsDone = false; await old.Open(); await model.OpenStepCommand.ExecuteAsync(old);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        model.SetActive(false);
    }

    [Fact]
    public async Task Original_read_failure_and_explicit_Retry_never_adopt_a_newly_selected_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await PrepareAsync(first); await PrepareAsync(second); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => Interlocked.Increment(ref calls) == 1 ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second) };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var beforeFirst = await GoalChooserFlowTests.SnapshotAsync(first); var beforeSecond = await GoalChooserFlowTests.SnapshotAsync(second);
        var model = Model(first, store); model.SetActive(true); await model.LoadAsync();
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract); Assert.NotEmpty(model.Feedback!);
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanInteract);
        Assert.Equal(beforeFirst, await GoalChooserFlowTests.SnapshotAsync(first));
        Assert.Equal(beforeSecond, await GoalChooserFlowTests.SnapshotAsync(second));
        model.SetActive(false);
    }

    [Fact]
    public async Task Original_step_route_preserves_unreviewed_scope_without_financial_or_progress_writes()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.SetActive(true); await model.LoadAsync(); Assert.Equal(5, model.Steps.Count);
        await model.OpenStepCommand.ExecuteAsync(model.Steps[0]);
        var route = Assert.Single(f.Platform.Routes); Assert.Equal("//transactions", route.Route);
        Assert.Equal(true, route.Parameters!["unreviewed"]);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); model.SetActive(false);
    }

    [Fact]
    public async Task An_original_finish_accepted_before_notification_failure_is_not_submitted_again_by_Retry()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync(); var writes = 0;
        f.Store.Changed += (_, _) => { writes++; throw new IOException("Fictitious accepted review notification failure."); };
        await model.FinishCommand.ExecuteAsync(null);
        Assert.Equal(1, writes); Assert.True(model.Accepted); Assert.True(model.Loading.HasFailed);
        Assert.False(model.CanInteract); Assert.Empty(f.Platform.Routes);
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], model.Feedback);
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(1, writes); Assert.True(model.Loading.IsReady); Assert.True(model.NothingToReview);
        Assert.Empty(model.Steps); Assert.Empty(f.Platform.Routes);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Equal("2026-09", (await db.Settings.SingleAsync(Ct)).ReviewProgress);
        Assert.Equal(10000, (await db.Accounts.SingleAsync(Ct)).OpeningBalance); Assert.Empty(await db.Entries.ToListAsync(Ct));
        model.SetActive(false);
    }

    [Fact]
    public async Task An_actual_pending_original_read_cannot_publish_after_its_page_retires()
    {
        using var f = new FlowFixture(); await PrepareAsync(f);
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async token => { reached.TrySetResult(); await release.Task.WaitAsync(token); } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = Model(f, store); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.SetActive(true); var pending = model.LoadAsync(); await reached.Task.WaitAsync(Ct);
        Assert.True(model.Loading.IsLoading); Assert.False(model.CanInteract);
        model.SetActive(false); await pending;
        Assert.Empty(model.Steps); Assert.False(model.CanInteract); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        factory.Before = null; release.TrySetResult();
        model.SetActive(true); await model.LoadAsync(); Assert.True(model.CanInteract); Assert.Equal(5, model.Steps.Count);
        model.SetActive(false);
    }

    [Fact]
    public async Task The_original_finish_command_cannot_adopt_a_new_day_without_a_fresh_publication()
    {
        using var f = new FlowFixture(); await PrepareAsync(f); var model = Model(f);
        model.SetActive(true); await model.LoadAsync();
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.Now = f.Time.Now.AddDays(1);
        await model.FinishCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes); Assert.False(model.CanInteract);
        model.SetActive(false);
    }
}
