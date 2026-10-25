using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual operation lifetimes retain the original cover, file and data during period/source supersession.</summary>
[Trait("AT", "AT-50")]
public sealed class HomePublicationReadLoopTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    /// <summary>Both pre-projection and final-period changes release the old lease before a new factory read.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public async Task Changed_period_drains_old_read_before_maintenance_and_retry(bool afterProjection)
    {
        using var f = new FlowFixture();
        var gate = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Access;
        var source = new ObservedSource(f.Store, gate); var period = 0; var projections = 0;
        source.AfterBegin = count => { if (count == 1 && !afterProjection) period = 1; };
        var refresh = new HomeRefreshSession(); var cover = new SnapshotLoadState();
        await cover.RunAsync(() => HomePublicationReadLoop.RunAsync(source, Day, () => period,
            refresh.RequireOriginalFile, async operation =>
            {
                Assert.False(cover.IsReady); projections++;
                await operation.ValidateAsync(Ct);
                if (afterProjection && projections == 1) { period = 1; return false; }
                Assert.Equal(1, operation.Snapshot.PeriodIndex); return true;
            }), () => Ct.ThrowIfCancellationRequested());
        Assert.True(cover.IsReady); Assert.Equal(2, source.Begun);
        Assert.Equal(afterProjection ? 2 : 1, projections);
        using var owner = gate.AcquireExclusive(); gate.RequireDrainedOwner(owner);
    }

    /// <summary>Even a file guard failure before projection drains the captured context.</summary>
    [Fact]
    public async Task Original_file_refusal_disposes_without_projecting_or_uncovering()
    {
        using var f = new FlowFixture();
        var gate = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Access;
        var source = new ObservedSource(f.Store, gate); var refresh = new HomeRefreshSession();
        refresh.RequireOriginalFile("Another original file"); var projected = false;
        var cover = new SnapshotLoadState();
        await Assert.ThrowsAsync<InvalidOperationException>(() => cover.RunAsync(() =>
            HomePublicationReadLoop.RunAsync(source, Day, () => 0, refresh.RequireOriginalFile,
                _ => { projected = true; return Task.FromResult(true); }), () => Ct.ThrowIfCancellationRequested()));
        Assert.False(projected); Assert.False(cover.IsReady); Assert.True(cover.HasFailed);
        using var owner = gate.AcquireExclusive(); gate.RequireDrainedOwner(owner);
    }

    /// <summary>A real committed definition edit retries only the covered read, never the accepted writer.</summary>
    [Fact]
    public async Task Source_edit_releases_session_before_original_bounded_reread()
    {
        using var f = new FlowFixture();
        var account = new Account { Name = "Original account", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 1234 };
        await f.Store.SaveAccountAsync(account, Ct);
        var gate = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Access;
        var source = new ObservedSource(f.Store, gate); var writes = 0; var cover = new SnapshotLoadState();
        await cover.RunAsync(() => HomeRefreshSession.RunAsync(refresh =>
            HomePublicationReadLoop.RunAsync(source, Day, () => 0, refresh.RequireOriginalFile, async operation =>
            {
                Assert.False(cover.IsReady);
                if (source.Begun == 1)
                {
                    account.Name = "Committed once"; await f.Store.SaveAccountAsync(account, Ct); writes++;
                }
                await operation.ValidateAsync(Ct);
                Assert.Equal("Committed once", Assert.Single(operation.Snapshot.QuickResources.Accounts).Name);
                return true;
            }), () => Ct.ThrowIfCancellationRequested()), () => Ct.ThrowIfCancellationRequested());
        Assert.True(cover.IsReady); Assert.Equal(2, source.Begun); Assert.Equal(1, writes);
        Assert.Equal(1234, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).OpeningBalance);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        using var owner = gate.AcquireExclusive(); gate.RequireDrainedOwner(owner);
    }

    /// <summary>Decorates the real store operation, proving maintenance can acquire ownership before every retry.</summary>
    private sealed class ObservedSource(ZananceStore store, DatabaseAccessGate gate) : IHomeReadOperationSource
    {
        internal int Begun;
        internal Action<int>? AfterBegin;
        public async Task<IHomeReadOperation> BeginAsync(DateOnly today, int periodIndex, CancellationToken cancellationToken = default)
        {
            using (var owner = gate.AcquireExclusive()) gate.RequireDrainedOwner(owner);
            var operation = await new HomeReadOperationSource(store).BeginAsync(today, periodIndex, cancellationToken);
            Begun++; AfterBegin?.Invoke(Begun); return operation;
        }
    }
}
