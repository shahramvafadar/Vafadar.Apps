using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Covered original Home refreshes over real source changes, preserving failures and file boundaries.</summary>
[Trait("AT", "AT-50")]
public sealed class HomeRefreshFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    [Fact]
    public async Task Same_file_concurrent_edit_is_reread_before_the_original_cover_is_removed()
    {
        using var f = new FlowFixture();
        var account = new Account { Name = "Original cash", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 1234 };
        await f.Store.SaveAccountAsync(account, Ct);
        var cover = new SnapshotLoadState(); var reads = 0; var published = 0; string? name = null;
        await cover.RunAsync(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            Assert.False(cover.IsReady); Assert.True(cover.IsLoading); Assert.False(cover.HasFailed);
            var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); reads++;
            refresh.RequireOriginalFile(packet.DatabasePath);
            if (reads == 1) { account.Name = "Changed while loading"; await f.Store.SaveAccountAsync(account, Ct); }
            await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
            name = Assert.Single(packet.QuickResources.Accounts).Name;
        }, () => Ct.ThrowIfCancellationRequested()), () => published++);
        Assert.True(cover.IsReady); Assert.False(cover.HasFailed); Assert.Equal(2, reads); Assert.Equal(1, published);
        Assert.Equal("Changed while loading", name);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(1234, saved.OpeningBalance);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Stable_home_is_read_once_and_never_creates_default_settings()
    {
        using var f = new FlowFixture(); var reads = 0;
        await HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct);
            refresh.RequireOriginalFile(packet.DatabasePath); await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => Ct.ThrowIfCancellationRequested());
        Assert.Equal(1, reads);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct));
    }

    [Fact]
    public async Task Continuous_source_edits_stop_after_three_reads_and_explicit_retry_can_finish()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day };
        await f.Store.SaveAccountAsync(account, Ct); var cover = new SnapshotLoadState(); var reads = 0; var published = 0;
        await Assert.ThrowsAsync<HomeSnapshotSupersededException>(() => cover.RunAsync(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            Assert.False(cover.IsReady); var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); reads++;
            refresh.RequireOriginalFile(packet.DatabasePath); account.Name = "Concurrent " + reads;
            await f.Store.SaveAccountAsync(account, Ct); await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => Ct.ThrowIfCancellationRequested()), () => published++));
        Assert.Equal(3, reads); Assert.Equal(0, published); Assert.True(cover.HasFailed); Assert.False(cover.IsReady);
        await cover.RunAsync(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct);
            refresh.RequireOriginalFile(packet.DatabasePath); await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
            Assert.Equal("Concurrent 3", Assert.Single(packet.QuickResources.Accounts).Name);
        }, () => Ct.ThrowIfCancellationRequested()), () => published++);
        Assert.True(cover.IsReady); Assert.Equal(4, reads); Assert.Equal(1, published);
    }

    [Fact]
    public async Task Changed_original_packet_is_not_retried_even_when_persisted_source_also_changed()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day };
        await f.Store.SaveAccountAsync(account, Ct); var reads = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); refresh.RequireOriginalFile(packet.DatabasePath);
            packet.QuickResources.Accounts[0].OpeningBalance = 999;
            account.Name = "Actual changed"; await f.Store.SaveAccountAsync(account, Ct);
            await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => Ct.ThrowIfCancellationRequested()));
        Assert.Equal(1, reads); Assert.Equal(0, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).OpeningBalance);
    }

    [Fact]
    public async Task Source_supersession_does_not_authorize_a_later_read_from_another_profile()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture(); var reads = 0;
        var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day };
        await first.Store.SaveAccountAsync(account, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            var store = reads++ == 0 ? first.Store : second.Store;
            var packet = await store.GetHomeSnapshotAsync(Day, 0, Ct); refresh.RequireOriginalFile(packet.DatabasePath);
            account.Name = "Changed"; await first.Store.SaveAccountAsync(account, Ct);
            await store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => Ct.ThrowIfCancellationRequested()));
        Assert.Equal(2, reads); Assert.Empty(await second.Store.GetAccountsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Retired_native_publication_stops_before_the_classified_retry()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day };
        await f.Store.SaveAccountAsync(account, Ct); var reads = 0; var retired = false;
        await Assert.ThrowsAsync<OperationCanceledException>(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); refresh.RequireOriginalFile(packet.DatabasePath);
            account.Name = "Changed"; await f.Store.SaveAccountAsync(account, Ct); retired = true;
            await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => { if (retired) throw new OperationCanceledException("Original native graph retired"); }));
        Assert.Equal(1, reads);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Unexpected_failure_or_cancellation_is_never_a_source_retry(bool cancelled)
    {
        var reads = 0; var original = cancelled ? (Exception)new OperationCanceledException("Original cancellation") : new IOException("Original file failure");
        var failure = await Record.ExceptionAsync(() => HomeRefreshSession.RunAsync(_ =>
        { reads++; return Task.FromException(original); }, () => { }));
        Assert.Same(original, failure); Assert.Equal(1, reads);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Altered_plan_or_goal_cannot_hide_behind_a_concurrent_account_edit(bool goal)
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day };
        await f.Store.SaveAccountAsync(account, Ct); var reads = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); refresh.RequireOriginalFile(packet.DatabasePath);
            if (reads == 1)
            {
                if (goal) packet.Goals.Goals.Add(new Core.Goals.Goal { Name = "Mutated packet", CurrencyCode = "EUR", TargetAmount = 999 });
                else packet.Plans.Schedules.Add(new Core.Plans.Schedule { Name = "Mutated packet", AccountId = account.Id, Amount = 999 });
                account.Name = "Actual changed"; await f.Store.SaveAccountAsync(account, Ct);
            }
            await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
        }, () => Ct.ThrowIfCancellationRequested()));
        Assert.Equal(1, reads);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("plan")]
    [InlineData("goal")]
    [InlineData("template")]
    [InlineData("filter")]
    [InlineData("category")]
    [InlineData("settings")]
    public async Task Original_persisted_source_additions_are_reread_without_replacing_the_file(string change)
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 1234 };
        await f.Store.SaveAccountAsync(account, Ct); var reads = 0;
        await HomeRefreshSession.RunAsync(async refresh =>
        {
            reads++; var packet = await f.Store.GetHomeSnapshotAsync(Day, 0, Ct); refresh.RequireOriginalFile(packet.DatabasePath);
            if (reads == 1)
            {
                await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                switch (change)
                {
                    case "account": db.Accounts.Add(new Account { Name = "Concurrent", CurrencyCode = "EUR", OpeningDate = Day }); break;
                    case "plan": db.Schedules.Add(new Core.Plans.Schedule { Name = "Concurrent", AccountId = account.Id, Amount = 100, Rule = new() { Start = Day } }); break;
                    case "goal": db.Goals.Add(new Core.Goals.Goal { Name = "Concurrent", CurrencyCode = "EUR", TargetAmount = 999 }); break;
                    case "template": db.Templates.Add(new Core.Ledger.EntryTemplate { Name = "Concurrent", AccountId = account.Id, Amount = 100 }); break;
                    case "filter": db.SavedFilters.Add(new Core.Ledger.SavedFilter { Name = "Concurrent" }); break;
                    case "category": db.Categories.Add(new Core.Categories.Category { Name = "Concurrent" }); break;
                    case "settings": db.Settings.Add(new Core.Settings.ZananceSettings { MonthStartDay = 5 }); break;
                }
                await db.SaveChangesAsync(Ct);
            }
            await f.Store.ValidateHomeSnapshotAsync(packet, Ct);
            if (reads == 2)
            {
                if (change == "account") Assert.Equal(2, packet.QuickResources.Accounts.Count);
                if (change == "plan") Assert.Single(packet.Plans.Schedules);
                if (change == "goal") Assert.Single(packet.Goals.Goals);
                if (change == "template") Assert.Single(packet.QuickResources.Templates);
                if (change == "filter") Assert.Single(packet.QuickResources.Filters);
                if (change == "category") Assert.Single(packet.QuickResources.Categories);
                if (change == "settings") Assert.Equal(5, packet.QuickResources.Settings!.MonthStartDay);
            }
        }, () => Ct.ThrowIfCancellationRequested());
        Assert.Equal(2, reads); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
