using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original lifecycle retirement releases the cancelled source even when one registered callback fails.</summary>
public sealed class NativeLifetimeCancellationTests
{
    /// <summary>The previous token is cancelled and disposed while the original operation failure remains observable.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Failing_cancellation_disposes_the_retired_source(int transition)
    {
        using var fixture = new FlowFixture();
        var setup = AppLockTests.Create(fixture);
        await setup.Lock.StartAsync();
        var location = fixture.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var profiles = new ProfileService(location, fixture.Store, fixture.Preferences, new UnusedRuntime(), fixture.Translator);
        using var lifetime = new PersonalSyncLifetime(setup.Lock, profiles, location);
        lifetime.Resume();
        var original = lifetime.CaptureAnalysis(false);
        _ = original.Cancellation.WaitHandle;
        var marker = new IOException("Fictitious original cancellation callback.");
        using var registration = original.Cancellation.Register(() => throw marker);

        Action change = transition switch { 0 => lifetime.Suspend, 1 => lifetime.Resume, _ => lifetime.RetireNavigation };
        var failure = Assert.Throws<AggregateException>(change);
        Assert.Same(marker, Assert.Single(failure.InnerExceptions));
        Assert.True(original.Cancellation.IsCancellationRequested);
        Assert.ThrowsAny<OperationCanceledException>(original.Validate);
        Assert.Throws<ObjectDisposedException>(() => _ = original.Cancellation.WaitHandle);
        if (transition == 0) Assert.ThrowsAny<OperationCanceledException>(() => lifetime.CaptureAnalysis(false));
        else lifetime.CaptureAnalysis(false).Validate();
        Assert.Equal(0, setup.Auth.Calls);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Refuses every profile effect; only the unchanged catalogue can be captured.</summary>
    internal sealed class UnusedRuntime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new InvalidOperationException("No profile transition belongs to this review.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new InvalidOperationException();
        public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException();
        public Task ReloadLockAsync() => throw new InvalidOperationException();
        public void DismissUndo() => throw new InvalidOperationException();
    }
}
