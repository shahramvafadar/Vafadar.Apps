using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Failed native departure feedback cannot leave the original read waiting on a retired owner.</summary>
public sealed class PersonalSyncDeparturePendingTests
{
    /// <summary>The original held read cancels once, and retrying the unaccepted Close does not read again.</summary>
    [Fact]
    public async Task Departure_feedback_failure_cancels_the_original_held_read_before_recovery()
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var factory = new HoldingFactory();
        var store = new PersonalSyncStore(factory, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new PersonalSyncViewModel(store, fixture.Services.GetRequiredService<PersonalSyncCoordinator>(),
            environment, fixture.Platform, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        var closes = 0;
        model.BeginOnboarding(() => { closes++; return Task.CompletedTask; }, () => Task.CompletedTask);
        var activation = model.ActivateAsync();
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var failures = 0;
        PropertyChangedEventHandler fault = (_, e) =>
        {
            if (e.PropertyName != nameof(model.HasAttachmentPreview)) return;
            // Pending-read cleanup may also publish this property; keep the failure active for Close itself.
            Interlocked.Increment(ref failures);
            throw new IOException("Fictitious pending departure feedback failure.");
        };
        model.PropertyChanged += fault;
        try
        {
            var escaped = await Record.ExceptionAsync(() => model.CloseOnboardingCommand.ExecuteAsync(null));
            model.PropertyChanged -= fault;
            await activation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.Null(escaped);
            Assert.True(failures > 0);
            Assert.Equal(0, closes);
            Assert.Equal(1, factory.Attempts);
            Assert.Equal(1, factory.Cancelled);
            Assert.Equal(1, factory.Ended);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanRead);
            Assert.True(model.CanLeave);
            await model.CloseOnboardingCommand.ExecuteAsync(null);
            Assert.Equal(1, closes);
            Assert.Equal(1, factory.Attempts);
            Assert.Equal(1, environment.Captures);
            Assert.Equal(0, environment.Opens);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            model.PropertyChanged -= fault;
            model.Deactivate();
            await activation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
    }

    private sealed class HoldingFactory : IDbContextFactory<ZananceDbContext>
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Attempts { get; private set; }
        internal int Cancelled { get; private set; }
        internal int Ended { get; private set; }

        public ZananceDbContext CreateDbContext() => throw new InvalidOperationException("No synchronous context.");

        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            Attempts++;
            Entered.TrySetResult();
            try
            {
                return await new TaskCompletionSource<ZananceDbContext>(TaskCreationOptions.RunContinuationsAsynchronously)
                    .Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) { Cancelled++; throw; }
            finally { Ended++; }
        }
    }
}
