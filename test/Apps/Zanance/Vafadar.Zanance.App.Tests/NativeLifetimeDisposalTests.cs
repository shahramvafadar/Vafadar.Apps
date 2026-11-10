using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Terminal lifetime disposal releases resources and never recreates foreground authority.</summary>
public sealed class NativeLifetimeDisposalTests
{
    /// <summary>Throwing, late, reentrant and repeated callbacks preserve the original terminal boundary.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Disposed_lifetime_cannot_restore_access_or_retain_native_resources(int scenario)
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var location = fixture.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var profiles = new ProfileService(location, fixture.Store, fixture.Preferences,
            new NativeLifetimeCancellationTests.UnusedRuntime(), fixture.Translator);
        var lifetime = new PersonalSyncLifetime(setup.Lock, profiles, location);
        lifetime.Resume();
        var original = lifetime.CaptureAnalysis(false);
        _ = original.Cancellation.WaitHandle;
        var marker = new IOException("Fictitious terminal cancellation callback.");
        using var registration = original.Cancellation.Register(() =>
        {
            if (scenario == 0) throw marker;
            if (scenario == 2) lifetime.Resume();
        });

        var failure = Record.Exception(lifetime.Dispose);
        if (scenario == 0)
            Assert.Same(marker, Assert.Single(Assert.IsType<AggregateException>(failure).InnerExceptions));
        else Assert.Null(failure);
        if (scenario == 1)
        {
            Assert.Null(Record.Exception(lifetime.Resume));
            Assert.Null(Record.Exception(lifetime.Suspend));
            Assert.Null(Record.Exception(lifetime.RetireNavigation));
        }
        if (scenario == 3) Assert.Null(Record.Exception(lifetime.Dispose));
        Assert.True(original.Cancellation.IsCancellationRequested);
        Assert.ThrowsAny<OperationCanceledException>(original.Validate);
        Assert.Throws<ObjectDisposedException>(() => _ = original.Cancellation.WaitHandle);
        Assert.ThrowsAny<OperationCanceledException>(() => lifetime.CaptureAnalysis(false));
        Assert.Equal(0, setup.Auth.Calls);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
