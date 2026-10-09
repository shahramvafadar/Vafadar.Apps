namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual external link parsing and application lock ordering; opening a draft never posts a ledger entry.</summary>
public sealed class AppLinkTests
{
    [Fact, Trait("AT", "AT-80")]
    public async Task Startup_link_waits_for_both_unlock_and_the_first_page_before_navigating()
    {
        using var f = new FlowFixture(); await AppLockTests.EnableDeviceAsync(f); var setup = AppLockTests.Create(f);
        var router = new AppLinkRouter(setup.Lock, f.Platform); var prepared = false;
        await setup.Lock.StartAsync();
        await router.StartAsync(() => { prepared = true; Assert.Empty(f.Platform.Routes); return Task.FromResult<string?>("entry|Expense"); });
        Assert.False(prepared); Assert.Empty(f.Platform.Routes);
        await setup.Lock.UnlockedAsync(); Assert.True(prepared);
        var opened = Assert.Single(f.Platform.Routes); Assert.Equal(AppRoutes.EntryEditorRoute, opened.Route);
        Assert.Equal("Expense", opened.Parameters!["kind"]);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Startup_without_a_link_prepares_the_page_once_and_never_navigates()
    {
        using var f = new FlowFixture(); var setup = AppLockTests.Create(f); var router = new AppLinkRouter(setup.Lock, f.Platform);
        var prepared = 0; await setup.Lock.StartAsync();
        await router.StartAsync(() => { prepared++; return Task.FromResult<string?>(null); });
        Assert.Equal(1, prepared); Assert.Empty(f.Platform.Routes);
    }

    [Theory, Trait("AT", "AT-80")]
    [InlineData("entry|Expense", "entry", "Expense")]
    [InlineData("entry|Income", "entry", "Income")]
    [InlineData("entry|Transfer", "entry", "Transfer")]
    [InlineData("plans", "//plans", null)]
    [InlineData("goals", "//insights/goals", null)]
    [InlineData("budget", "//insights/budget", null)]
    [InlineData("review", "review", null)]
    public async Task Supported_links_open_their_drafts_only_after_secure_startup_and_never_write_money(string link, string route, string? kind)
    {
        using var f = new FlowFixture(); var setup = AppLockTests.Create(f); var router = new AppLinkRouter(setup.Lock, f.Platform);
        await router.OpenAsync(link); Assert.Empty(f.Platform.Routes); await setup.Lock.StartAsync();
        var opened = Assert.Single(f.Platform.Routes); Assert.Equal(route, opened.Route);
        if (kind is not null) { Assert.Equal(kind, opened.Parameters!["kind"]); } else { Assert.Null(opened.Parameters); }
        await router.OpenAsync(link); Assert.Equal(2, f.Platform.Routes.Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory, Trait("AT", "AT-80")]
    [InlineData("")]
    [InlineData("entry|0")]
    [InlineData("entry|Refund")]
    [InlineData("entry|expense")]
    [InlineData("entry|Expense|unexpected")]
    [InlineData("occurrence|invalid|2026-10-09")]
    [InlineData("occurrence|7b8f33d0-4fea-4d6a-9b87-bfaf0eb2c3b3|2026-02-30")]
    [InlineData("plan|00000000-0000-0000-0000-000000000000")]
    [InlineData("goal|invalid")]
    [InlineData("review|unexpected")]
    [InlineData("//settings")]
    public async Task Malformed_or_unsupported_links_are_not_queued_or_navigated(string link)
    {
        using var f = new FlowFixture(); var setup = AppLockTests.Create(f); var router = new AppLinkRouter(setup.Lock, f.Platform);
        await router.OpenAsync(link); await setup.Lock.StartAsync(); Assert.Empty(f.Platform.Routes);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Occurrence_plan_and_goal_keep_typed_identity_and_original_date_while_waiting_for_unlock()
    {
        using var f = new FlowFixture(); await AppLockTests.EnableDeviceAsync(f); var setup = AppLockTests.Create(f);
        var router = new AppLinkRouter(setup.Lock, f.Platform); var id = Guid.NewGuid();
        await setup.Lock.StartAsync(); await router.OpenAsync($"occurrence|{id}|2026-10-09");
        await router.OpenAsync($"plan|{id}"); await router.OpenAsync($"goal|{id}"); Assert.Empty(f.Platform.Routes);
        await setup.Lock.UnlockedAsync(); Assert.Equal(3, f.Platform.Routes.Count);
        Assert.Equal(AppRoutes.OccurrenceRoute, f.Platform.Routes[0].Route);
        Assert.Equal(id, f.Platform.Routes[0].Parameters!["plan"]); Assert.Equal(new DateOnly(2026, 10, 9), f.Platform.Routes[0].Parameters!["date"]);
        Assert.Equal(AppRoutes.PlanDetailRoute, f.Platform.Routes[1].Route); Assert.Equal(id, f.Platform.Routes[1].Parameters!["id"]);
        Assert.Equal(AppRoutes.GoalDetailRoute, f.Platform.Routes[2].Route); Assert.Equal(id, f.Platform.Routes[2].Parameters!["id"]);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
