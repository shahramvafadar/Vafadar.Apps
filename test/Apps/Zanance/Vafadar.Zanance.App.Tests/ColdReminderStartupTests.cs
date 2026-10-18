using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks original reminder preservation when a headless process cannot prepare protected storage.</summary>
public sealed class ColdReminderStartupTests
{
    [Theory, Trait("AT", "AT-333")]
    [InlineData("refused")]
    [InlineData("unpublished")]
    [InlineData("missing")]
    public async Task Cold_snooze_preserves_pending_notifications_when_protected_startup_is_unavailable(string state)
    {
        await using var fixture = new ProfileReminderFlowTests.Fixture();
        await fixture.SeedAsync();
        fixture.Data.Enable(ProductPlan.Plus);
        await fixture.Service.RefreshAsync();
        var original = fixture.Native.Pending.ToArray();
        var notification = Assert.Single(original, item => item.CanSnooze);
        var startup = new UnavailableStartup(state == "refused");
        var originalRows = await fixture.RowsAsync();
        using var cold = CreateColdProvider(fixture, state == "missing" ? null : startup);
        var service = cold.GetRequiredService<ReminderService>();
        Assert.Equal(0, startup.Attempts);

        await service.SnoozeAsync(new(notification, SnoozeChoice.OneHour));

        Assert.Equal(original, fixture.Native.Pending);
        Assert.Equal(state == "missing" ? 0 : 1, startup.Attempts);
        Assert.Null(fixture.Data.Preferences.Get("reminders.snoozed"));
        Assert.Equal(originalRows, await fixture.RowsAsync());
    }

    [Fact, Trait("AT", "AT-333")]
    public async Task Disposing_a_snooze_during_cold_preparation_does_not_open_financial_storage_or_clear_notifications()
    {
        await using var fixture = new ProfileReminderFlowTests.Fixture();
        await fixture.SeedAsync(); fixture.Data.Enable(ProductPlan.Plus); await fixture.Service.RefreshAsync();
        var original = fixture.Native.Pending.ToArray();
        var notification = Assert.Single(original, item => item.CanSnooze);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>? location = null;
        var financialOpens = 0;
        var startup = new ContinuingStartup(async () =>
        {
            entered.SetResult(); await release.Task;
            // Native publication is an explicit effect port. No cipher connection is fabricated or opened here.
            location!.MoveToProtected(location.Path, () =>
            {
                Interlocked.Increment(ref financialOpens);
                throw new IOException("A disposed snooze must not open this native connection port.");
            }, "native-effect-port");
        });
        using var cold = CreateColdProvider(fixture, startup);
        location = cold.GetRequiredService<Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>>();
        var service = cold.GetRequiredService<ReminderService>();
        var pending = service.SnoozeAsync(new(notification, SnoozeChoice.OneHour));
        try
        {
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            service.Dispose();
        }
        finally { release.TrySetResult(); }
        await pending;
        Assert.Equal(original, fixture.Native.Pending);
        Assert.Equal(0, financialOpens);
        Assert.Null(fixture.Data.Preferences.Get("reminders.snoozed"));
    }

    /// <summary>Creates a cold protected endpoint over only the original isolated SQLite fixture.</summary>
    private static ServiceProvider CreateColdProvider(ProfileReminderFlowTests.Fixture fixture, IAppDatabaseStartup? startup)
    {
        var path = fixture.Data.Services.GetRequiredService<Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>>().Path;
        var registration = new ServiceCollection()
            .AddSingleton<TimeProvider>(fixture.Clock)
            .AddZananceData(path, protectionRequired: true)
            .AddSingleton<ISettingsStore>(fixture.Data.Preferences)
            .AddSingleton(fixture.Data.Flow.Translator)
            .AddSingleton<ILocalizationService>(fixture.Data.Flow.Localization)
            .AddSingleton<IDateFormatter>(new DateFormatter(fixture.Data.Flow.Localization))
            .AddSingleton<IReminderScheduler>(fixture.Native)
            .AddSingleton<ReminderService>();
        if (startup is not null) registration.AddSingleton(startup);
        return registration.BuildServiceProvider();
    }

    /// <summary>Controls only the native preparation boundary for the disposal race.</summary>
    private sealed class ContinuingStartup(Func<Task> prepare) : IAppDatabaseStartup
    {
        /// <inheritdoc />
        public Task PrepareAsync(CancellationToken cancellationToken = default) => prepare();
    }

    /// <summary>Represents a native startup refusal; financial reads and notification policy remain real.</summary>
    private sealed class UnavailableStartup(bool refuse) : IAppDatabaseStartup
    {
        /// <summary>Counts preparation requests without opening a native key or cipher connection.</summary>
        internal int Attempts;

        /// <inheritdoc />
        public Task PrepareAsync(CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (refuse) throw new IOException("Protected startup is unavailable in this isolated scenario.");
            return Task.CompletedTask;
        }
    }
}
