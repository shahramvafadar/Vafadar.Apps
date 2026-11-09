using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original Home template invocation owns startup, asynchronous failure and the publication it may invalidate.</summary>
public sealed class HomeTemplateInvocationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>All three original command paths must release Busy even when its first notification fails.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("choose")]
    [InlineData("reload")]
    public async Task Home_template_invocation_startup_exception_keeps_cleanup_active(string action)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, action); var fired = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        { if (e.PropertyName == nameof(s.Model.IsBusy) && s.Model.IsBusy && !fired) { fired = true; throw new IOException("D957 original Busy publication"); } };
        var error = await Record.ExceptionAsync(() => InvokeAsync(s, action));
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Model.IsBusy); Assert.Null(error); Assert.Single(f.Platform.Failures);
        Assert.Equal(0, s.Probe.Calls); Assert.Equal(0, s.ReloadCalls); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>A replacement published during Busy belongs to a new invocation, not the old command's failure.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("choose")]
    public async Task Home_template_invocation_startup_replacement_keeps_the_new_publication_ready(string action)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, action); var fired = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        { if (e.PropertyName == nameof(s.Model.IsBusy) && s.Model.IsBusy && !fired) { fired = true; s.Model.Publish(s.Replacement); } };
        await InvokeAsync(s, action);
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(s.Model.CanInteract); Assert.Null(s.Model.Feedback); Assert.Empty(f.Platform.Failures);
        Assert.Equal(0, s.Probe.Calls); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>A new cover in Reload's startup notification must prevent even entering its original parent delegate.</summary>
    [Fact]
    public async Task Home_template_invocation_reload_startup_cover_refuses_parent_entry()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "reload"); var fired = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        { if (e.PropertyName == nameof(s.Model.IsBusy) && s.Model.IsBusy && !fired) { fired = true; s.Model.BeginLoad(); } };
        await InvokeAsync(s, "reload");
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, s.ReloadCalls); Assert.False(s.Model.CanInteract); Assert.False(s.Model.IsBusy); Assert.Empty(f.Platform.Failures);
    }

    /// <summary>A real source-factory refusal arriving after a new review cannot invalidate or report through that replacement.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("choose")]
    public async Task Home_template_invocation_pending_source_failure_keeps_the_new_publication(string action)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, action);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        s.Probe.BeforeFailure = async () => { entered.SetResult(); await release.Task.WaitAsync(Ct); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var pending = InvokeAsync(s, action);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); s.Model.Publish(s.Replacement); release.SetResult(); await pending;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, s.Probe.Calls);
        Assert.True(s.Model.CanInteract); Assert.Null(s.Model.Feedback); Assert.False(s.Model.IsBusy);
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>A failed parent Reload may not mark a later complete review as failed.</summary>
    [Fact]
    public async Task Home_template_invocation_pending_reload_failure_keeps_the_new_publication()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "reload");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); s.Reload = () => release.Task;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var pending = InvokeAsync(s, "reload");
        Assert.Equal(1, s.ReloadCalls); s.Model.Publish(s.Replacement); release.SetException(new IOException("D957 retired reload failure")); await pending;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(s.Model.CanInteract);
        Assert.Null(s.Model.Feedback); Assert.False(s.Model.IsBusy); Assert.Empty(f.Platform.Failures);
    }

    /// <summary>The failure caption itself can publish a replacement before any native modal is requested.</summary>
    [Fact]
    public async Task Home_template_invocation_failure_caption_replacement_refuses_stale_native_feedback()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "open"); var fired = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        { if (e.PropertyName == nameof(s.Model.Feedback) && s.Model.Feedback is not null && !fired) { fired = true; s.Model.Publish(s.Replacement); } };
        await InvokeAsync(s, "open");
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(s.Model.CanInteract); Assert.Null(s.Model.Feedback); Assert.Empty(f.Platform.Failures);
        Assert.Equal(1, s.Probe.Calls); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Current failures still reach the established reporter, including a parent Reload that starts its own cover.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("choose")]
    [InlineData("reload")]
    public async Task Home_template_invocation_current_failure_remains_explicitly_reloadable(string action)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, action);
        if (action == "reload") s.Reload = () => { s.Model.BeginLoad(); return Task.FromException(new IOException("D957 current parent read failure")); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await InvokeAsync(s, action);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(s.Model.CanInteract);
        Assert.True(s.Model.CanReload); Assert.False(s.Model.IsBusy); Assert.Single(f.Platform.Failures);
        Assert.Equal(f.Translator["Templates_SourceChanged"], s.Model.Feedback); Assert.Empty(f.Platform.Routes);
        Assert.Equal(action == "reload" ? 0 : 1, s.Probe.Calls); Assert.Equal(action == "reload" ? 1 : 0, s.ReloadCalls);
    }

    /// <summary>A disconnected parent's child cannot be rearmed by late cover, failure or complete source publication.</summary>
    [Fact]
    public async Task Home_template_invocation_permanent_retirement_cannot_rearm_from_late_publication()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "open");
        var before = await GoalChooserFlowTests.SnapshotAsync(f); s.Model.RetirePublication();
        s.Model.Publish(s.Replacement); s.Model.BeginLoad(); s.Model.FailLoad(); s.Model.RefreshDisplay();
        await s.Model.OpenAsync(s.Template); await s.Model.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(s.Model.CanInteract);
        Assert.False(s.Model.CanUse(s.Template)); Assert.False(s.Model.CanReload); Assert.False(s.Model.IsBusy);
        Assert.Equal(0, s.Probe.Calls); Assert.Equal(0, s.ReloadCalls); Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>New ownership guards retain original current-owner handoff and selection routes without applying an entry.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("choose")]
    public async Task Home_template_invocation_valid_current_handoff_remains_read_only(string action)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, action); s.Probe.Armed = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await InvokeAsync(s, action);
        var route = Assert.Single(f.Platform.Routes);
        if (action == "open")
        {
            Assert.Equal(AppRoutes.EntryEditorRoute, route.Route);
            var visit = Assert.IsType<TemplateVisit>(route.Parameters!["templateVisit"]);
            Assert.Equal(s.Template, visit.TemplateId);
        }
        else
        { Assert.Equal(AppRoutes.ActiveItemsRoute, route.Route); Assert.Equal(QuotaKind.QuickTemplates, route.Parameters!["kind"]); }
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(s.Model.CanInteract);
        Assert.False(s.Model.IsBusy); Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    private static Task InvokeAsync(Prepared s, string action) => action switch
    { "open" => s.Model.OpenAsync(s.Template), "choose" => s.Model.ChooseCommand.ExecuteAsync(null), "reload" => s.Model.ReloadCommand.ExecuteAsync(null), _ => throw new ArgumentOutOfRangeException(nameof(action)) };

    private static async Task<Prepared> PrepareAsync(FlowFixture f, string action)
    {
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var account = new Account { Name = "D957 template metadata only", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 };
        var templates = Enumerable.Range(0, action == "choose" ? 8 : 1).Select(i => new EntryTemplate { Name = "D957 definition " + i,
            AccountId = account.Id, Kind = EntryKind.Expense, Amount = 957, SortOrder = i }).ToArray();
        await using (var db = await factory.CreateDbContextAsync(Ct)) { db.Accounts.Add(account); db.Templates.AddRange(templates); await db.SaveChangesAsync(Ct); }
        var access = new AccessSource(action == "choose" ? new CommercialWriteAccess(f.Directory.Combine("zanance.db"),
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid()))) : CommercialWriteAccess.Inactive);
        var probe = new SourceProbe(factory); var store = new ZananceStore(probe, f.Time, access);
        var initial = await store.GetQuickResourceReviewAsync(cancellationToken: Ct); var replacement = await store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        var state = new Prepared { Probe = probe, Replacement = replacement, Template = templates[0].Id };
        state.Model = new(store, f.Translator, f.Platform, () => { state.ReloadCalls++; return state.Reload(); }); state.Model.Publish(initial);
        Assert.True(action == "choose" ? state.Model.CanChoose : state.Model.CanInteract); probe.Armed = true; return state;
    }
    private sealed class Prepared
    {
        internal TemplateNavigationViewModel Model = null!;
        internal required SourceProbe Probe; internal required QuickResourceReview Replacement; internal Guid Template;
        internal int ReloadCalls; internal Func<Task> Reload = () => Task.CompletedTask;
    }
    private sealed class AccessSource(CommercialWriteAccess access) : ICommercialWriteAccessSource
    { public CommercialWriteAccess Capture(string databasePath) => access; }
    private sealed class SourceProbe(IDbContextFactory<ZananceDbContext> factory) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Armed; internal int Calls; internal Func<Task>? BeforeFailure;
        public ZananceDbContext CreateDbContext() { if (Armed) { Calls++; throw new IOException("D957 original source boundary"); } return factory.CreateDbContext(); }
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        { if (!Armed) return await factory.CreateDbContextAsync(ct); Calls++; if (BeforeFailure is { } wait) await wait(); throw new IOException("D957 original source boundary"); }
    }
}
