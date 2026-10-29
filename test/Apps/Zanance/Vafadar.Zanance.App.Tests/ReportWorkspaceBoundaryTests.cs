using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-647: real original SQLite report source, retired page commands, owned export effects and child handoff.</summary>
public sealed class ReportWorkspaceBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task SeedAsync(FlowFixture f)
    { await f.Store.GetSettingsAsync(Ct); await f.Store.SaveAccountAsync(new Account { Name = "D647 original account", CurrencyCode = "EUR", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct); }
    private static ReportWorkspace Work(FlowFixture f) => new(f.Services.GetRequiredService<ReportWorkStore>());
    private static Task PublishAsync(ReportWorkspace work) => work.PublishAsync((_, _) => Task.CompletedTask, _ => { });
    private sealed class GateFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Func<CancellationToken, Task>? Before;
        internal int Calls;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { Calls++; if (Before is { } before) await before(cancellationToken); return await Current.CreateDbContextAsync(cancellationToken); }
    }
    private static ReportWorkspace Gated(FlowFixture f, GateFactory factory) => new(new ReportWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time));
    /// <summary>Actual source acquisition cancels at departure and its late completion cannot uncover a resumed publication.</summary>
    [Fact]
    public async Task Pending_original_read_retires_without_covering_the_resumed_report()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var work = Gated(f, factory);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.TrySetResult(); await held.Task.WaitAsync(ct); }; var old = PublishAsync(work); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        work.SetActive(false); Assert.Null(work.CaptureCommand(() => "R1/EUR")); factory.Before = null; work.SetActive(true); await PublishAsync(work); held.TrySetResult(); await old;
        Assert.True(work.CanInteract); Assert.Equal("D647 original account", Assert.Single(work.Review!.Inputs.Accounts).Name); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>An old original command cannot regain authority when another read publishes identical financial facts.</summary>
    [Fact]
    public async Task Old_original_command_does_not_regain_authority_after_republication()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var old = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR"));
        await PublishAsync(work); Assert.False(old.IsCurrent); Assert.True(work.CanInteract); Assert.NotNull(work.CaptureCommand(() => "R1/EUR"));
        var error = await Record.ExceptionAsync(old.ValidateAsync); Assert.True(error is OperationCanceledException or InvalidOperationException);
    }
    /// <summary>Real source facts and native scope are independent requirements before an old command can run.</summary>
    [Fact]
    public async Task Changed_original_scope_cannot_reuse_the_previous_report_command()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var scope = "R1/EUR"; var old = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => scope));
        scope = "R2/USD"; Assert.False(old.IsCurrent); await Assert.ThrowsAsync<InvalidOperationException>(old.ValidateAsync); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Actual protected source changes cover old authority and retain explicit current-page Retry.</summary>
    [Fact]
    public async Task Changed_actual_settings_refuse_old_source_until_explicit_retry()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var old = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR"));
        var settings = await f.Store.GetSettingsAsync(Ct); settings.MonthStartDay = 17; await f.Store.SaveSettingsAsync(settings, Ct);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(old.ValidateAsync); await work.RefuseAsync(error);
        Assert.True(work.Loading.HasFailed); Assert.False(work.CanInteract); Assert.Null(work.CaptureCommand(() => "R1/EUR")); await PublishAsync(work);
        Assert.True(work.CanInteract); Assert.Equal(17, work.Review!.Inputs.Settings!.MonthStartDay);
    }
    /// <summary>A native report cannot silently follow another valid actual reader file.</summary>
    [Fact]
    public async Task Original_report_refuses_a_replacement_actual_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f); await SeedAsync(other);
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var work = Gated(f, factory); await PublishAsync(work);
        factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); await PublishAsync(work);
        Assert.True(work.Loading.HasFailed); Assert.IsType<InvalidOperationException>(work.Failure); Assert.False(work.CanInteract); Assert.Null(work.Review);
    }
    /// <summary>Ordinary departure permits only the owned child's frozen source handoff, not an old parent command.</summary>
    [Fact]
    public async Task Owned_child_source_survives_expected_parent_departure_but_not_root_retirement()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var parent = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR")); var child = parent.CaptureChildSource();
        work.SetActive(false); Assert.False(parent.IsCurrent); await child.ValidateAsync(); work.SetActive(true); await PublishAsync(work); await child.ValidateAsync();
        work.RetireRoot(); Assert.False(child.IsCurrent); var error = await Record.ExceptionAsync(child.ValidateAsync); Assert.True(error is OperationCanceledException or InvalidOperationException);
    }
    /// <summary>Original caller/native query mutations cannot change the route or ids captured for an explanation.</summary>
    [Fact]
    public void Frozen_original_navigation_copies_caller_and_native_query_id_arrays()
    {
        var first = Guid.NewGuid(); var callerIds = new List<Guid> { first }; var caller = new Dictionary<string, object> { ["accounts"] = callerIds, ["currency"] = "EUR" };
        caller["confirmed"] = true; caller["from"] = new DateOnly(2026, 10, 1);
        var route = new ReportNavigation("//transactions", caller).WithQueryItem("search", "#D647"); callerIds[0] = Guid.NewGuid(); caller["currency"] = "USD";
        var native = route.CopyQuery(); Assert.True((bool)native["confirmed"]); Assert.Equal(new DateOnly(2026, 10, 1), native["from"]); Assert.Equal("#D647", native["search"]); Assert.Equal(first, Assert.Single((Guid[])native["accounts"])); Assert.Equal("EUR", native["currency"]);
        ((Guid[])native["accounts"])[0] = Guid.NewGuid(); native["currency"] = "JPY"; Assert.Equal(first, Assert.Single((Guid[])route.CopyQuery()["accounts"])); Assert.Equal("EUR", route.CopyQuery()["currency"]);
    }
    private sealed class OwnedFileRuntime(string path) : IReportExportRuntime
    {
        internal TaskCompletionSource<bool>? Consent;
        internal readonly TaskCompletionSource AuthenticationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Action? AfterWrite, AfterShare;
        internal int Writes, Shares, Removes;
        public Task<bool> ConfirmAsync() { AuthenticationEntered.TrySetResult(); return Consent?.Task ?? Task.FromResult(true); }
        public async Task<string> WriteAsync(byte[] document) { Writes++; await File.WriteAllBytesAsync(path, document, Ct); AfterWrite?.Invoke(); return path; }
        public Task ShareAsync(string owned) { Assert.Equal(path, owned); Shares++; AfterShare?.Invoke(); return Task.CompletedTask; }
        public Task RemoveAsync(string owned) { Assert.Equal(path, owned); Removes++; File.Delete(owned); return Task.CompletedTask; }
    }
    /// <summary>Root retirement during the original authentication port prevents rendering, cache and sharing.</summary>
    [Fact]
    public async Task Retired_original_report_cannot_export_after_pending_authentication()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var lease = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR"));
        var runtime = new OwnedFileRuntime(f.Directory.Combine("d647-unpublished.pdf")) { Consent = new(TaskCreationOptions.RunContinuationsAsynchronously) }; var renders = 0;
        var pending = new ReportExportSession(f.Services.GetRequiredService<ReportWorkStore>()).ExportAsync(lease.Review, false, lease.RequireCurrent, () => { renders++; return Task.FromResult(Encoding.ASCII.GetBytes("D647 effect bytes; not a rendered PDF")); }, runtime, lease.Token);
        await runtime.AuthenticationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        work.RetireRoot(); runtime.Consent.SetResult(true); var error = await Record.ExceptionAsync(() => pending);
        Assert.True(error is OperationCanceledException or InvalidOperationException); Assert.Equal(0, renders); Assert.Equal(0, runtime.Writes); Assert.Equal(0, runtime.Shares);
    }
    /// <summary>Frame retirement after a real owned cache write removes that unpublished file only.</summary>
    [Fact]
    public async Task Retired_original_frame_removes_its_unpublished_owned_export_file()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var lease = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR")); var path = f.Directory.Combine("d647-unpublished.pdf");
        var runtime = new OwnedFileRuntime(path) { AfterWrite = () => work.SetActive(false) };
        var error = await Record.ExceptionAsync(() => new ReportExportSession(f.Services.GetRequiredService<ReportWorkStore>()).ExportAsync(lease.Review, false, lease.RequireCurrent, () => Task.FromResult(Encoding.ASCII.GetBytes("D647 owned cache effect")), runtime, lease.Token));
        Assert.True(error is OperationCanceledException or InvalidOperationException); Assert.Equal(1, runtime.Writes); Assert.Equal(1, runtime.Removes); Assert.Equal(0, runtime.Shares); Assert.False(File.Exists(path));
    }
    /// <summary>A native recipient may keep reading a file after share starts, even if the original report retires afterward.</summary>
    [Fact]
    public async Task Retiring_after_original_share_starts_does_not_remove_the_offered_file()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var work = Work(f); await PublishAsync(work); var lease = Assert.IsType<ReportCommandLease>(work.CaptureCommand(() => "R1/EUR")); var path = f.Directory.Combine("d647-offered.pdf");
        var runtime = new OwnedFileRuntime(path) { AfterShare = () => work.SetActive(false) };
        await new ReportExportSession(f.Services.GetRequiredService<ReportWorkStore>()).ExportAsync(lease.Review, false, lease.RequireCurrent, () => Task.FromResult(Encoding.ASCII.GetBytes("D647 offered cache effect")), runtime, lease.Token);
        Assert.Equal(1, runtime.Shares); Assert.Equal(0, runtime.Removes); Assert.True(File.Exists(path)); Assert.False(work.CanInteract);
    }
    /// <summary>The actual report contribution writer rolls back when native ownership retires before or after SQL.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_contribution_writer_refuses_retired_native_ownership_without_source_changes(bool afterSql)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var goals = f.Services.GetRequiredService<GoalStore>();
        var goal = new Core.Goals.Goal { Name = "D647 original goal", CurrencyCode = "EUR", TargetAmount = 15000 }; await goals.SaveGoalAsync(goal, Ct);
        var work = Work(f); await PublishAsync(work); var checks = 0; var accepted = 0; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var plan = new Core.Goals.ContributionPlan { Method = Core.Goals.ContributionMethod.FixedAmount, Amount = 2500,
            Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = new(2026, 11, 1) } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => goals.SaveContributionPlanAsync(goal.Id, plan, Ct, reportReview: work.Review,
            isCurrent: () => ++checks < (afterSql ? 2 : 1), accepted: () => accepted++));
        Assert.Equal(afterSql ? 2 : 1, checks); Assert.Equal(0, accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(await goals.GetContributionPlansAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>A committed metadata receipt is visible to the first Changed observer even when notification throws.</summary>
    [Fact]
    public async Task Actual_contribution_receipt_precedes_failing_notification_and_preserves_original_goal()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var goals = f.Services.GetRequiredService<GoalStore>();
        var goal = new Core.Goals.Goal { Name = "D647 original goal", CurrencyCode = "EUR", TargetAmount = 15000 }; await goals.SaveGoalAsync(goal, Ct);
        var work = Work(f); await PublishAsync(work); var accepted = 0; var observerSawReceipt = false;
        goals.Changed += (_, _) => { observerSawReceipt = accepted == 1; throw new InvalidOperationException("D647 owned observer refusal"); };
        var plan = new Core.Goals.ContributionPlan { Method = Core.Goals.ContributionMethod.FixedAmount, Amount = 2500,
            Rule = new Core.Plans.RecurrenceRule { Frequency = Core.Plans.Frequency.Monthly, Start = new(2026, 11, 1) } };
        var error = await Assert.ThrowsAsync<CommittedWriteNotificationException<Guid>>(() => goals.SaveContributionPlanAsync(goal.Id, plan, Ct,
            reportReview: work.Review, isCurrent: () => true, accepted: () => accepted++));
        Assert.Equal(goal.Id, error.Result); Assert.True(observerSawReceipt); Assert.Equal(1, accepted);
        Assert.Equal(2500, Assert.Single(await goals.GetContributionPlansAsync(Ct)).Amount); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

}
