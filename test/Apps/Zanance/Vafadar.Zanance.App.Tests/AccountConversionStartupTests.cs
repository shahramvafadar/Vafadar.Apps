using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original conversion startup cannot acquire source authority after synchronous publication retires its owner.</summary>
public sealed class AccountConversionStartupTests
{
    [Fact]
    public async Task Conversion_startup_busy_notification_failure_releases_the_original_command()
    {
        using var f = new FlowFixture(); var s = Prepare(f); var notified = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AccountConversionSession.IsBusy) || !s.Session.IsBusy || notified) return;
            notified = true; throw new IOException("D956 original Busy publication failure");
        };
        var error = await Record.ExceptionAsync(() => s.Session.ConvertAsync(s.Id, () => true));
        Assert.True(notified); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Session.IsBusy); Assert.Null(error); Assert.NotNull(s.Session.Error);
        NoContinuation(f, s, 0);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("external")]
    [InlineData("day")]
    [InlineData("language")]
    public async Task Conversion_startup_busy_publication_keeps_the_original_owner_and_display(string change)
    {
        using var f = new FlowFixture(); var s = Prepare(f); var changed = false; var external = true;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AccountConversionSession.IsBusy) || !s.Session.IsBusy || changed) return;
            changed = true;
            switch (change)
            {
                case "owner": s.Session.SetOwner(s.Id, false); s.Session.SetOwner(s.Id, true); break;
                case "external": external = false; break;
                case "day": f.Time.Now = f.Time.Now.AddDays(1); break;
                case "language": f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de")); break;
            }
        };
        Assert.Null(await s.Session.ConvertAsync(s.Id, () => external));
        Assert.True(changed); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Session.IsBusy); NoContinuation(f, s, 0);
    }

    [Fact]
    public async Task Conversion_startup_error_clear_cannot_acquire_a_retired_source()
    {
        using var f = new FlowFixture(); var s = Prepare(f);
        Assert.Null(await s.Session.ConvertAsync(s.Id, () => true)); Assert.NotNull(s.Session.Error);
        Assert.Equal(1, s.Factory.Acquired); s.Factory.Acquired = 0; var changed = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AccountConversionSession.Error) || s.Session.Error is not null || changed) return;
            changed = true; s.Session.SetOwner(s.Id, false); s.Session.SetOwner(s.Id, true);
        };
        Assert.Null(await s.Session.ConvertAsync(s.Id, () => true));
        Assert.True(changed); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Session.IsBusy); NoContinuation(f, s, 0);
    }

    [Fact]
    public async Task Conversion_startup_current_owner_still_reaches_original_source_validation()
    {
        using var f = new FlowFixture(); var s = Prepare(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Null(await s.Session.ConvertAsync(s.Id, () => true));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(s.Session.IsBusy);
        Assert.NotNull(s.Session.Error); NoContinuation(f, s, 1);
    }

    private static Prepared Prepare(FlowFixture f)
    {
        var probe = new SourceProbe(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new HoldingStore(probe, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var session = new AccountConversionSession(store, f.Translator, f.Localization, f.Time, f.Platform);
        var id = Guid.NewGuid(); session.SetOwner(id, true); return new(id, probe, session);
    }

    private static void NoContinuation(FlowFixture f, Prepared s, int expectedSourceCalls)
    {
        Assert.Equal(expectedSourceCalls, s.Factory.Acquired); Assert.Null(s.Session.AcceptedOutcome);
        Assert.Null(s.Session.Result); Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
    }

    private sealed record Prepared(Guid Id, SourceProbe Factory, AccountConversionSession Session);

    /// <summary>Observes the actual source-factory entry and refuses before connection/read/consent/writer acquisition.</summary>
    private sealed class SourceProbe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal int Acquired;
        public ZananceDbContext CreateDbContext() => Refuse();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromException<ZananceDbContext>(Failure());
        private ZananceDbContext Refuse() => throw Failure();
        private IOException Failure()
        {
            // Keep the concrete original factory identity without forwarding a read or manufacturing a financial result.
            _ = original; Acquired++; return new IOException("D956 original source-factory boundary sentinel");
        }
    }
}
