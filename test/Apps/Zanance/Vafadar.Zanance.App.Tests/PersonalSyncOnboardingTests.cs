using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original first-run completion over actual SQLite, with window/dialog effects isolated from financial writers.</summary>
public sealed class PersonalSyncOnboardingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<PersonalSyncStore> SeedAsync(FlowFixture f, bool account = true, bool acknowledged = true)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.ReportCurrencyCode = "KWD"; settings.DefaultCurrencyCode = "JPY";
        settings.Mode = Vafadar.Zanance.Core.Settings.ExperienceMode.Advanced; await f.Store.SaveSettingsAsync(settings, Ct);
        if (account) await f.Store.SaveAccountAsync(new Account { Name = "Fictitious existing received account", CurrencyCode = "KWD",
            OpeningDate = new(2026, 10, 15), OpeningBalance = 12345 }, Ct);
        var sync = f.Services.GetRequiredService<PersonalSyncStore>();
        await sync.ConnectAsync(await sync.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)), "fictitious-first-run-owner", Guid.NewGuid(),
            new(Guid.NewGuid(), "Fictitious received space", SyncSchema.Hash, 7, false), Ct);
        if (acknowledged)
        {
            // A received completion fact only; actual store writes create every financial/settings row.
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SyncBinding SET LastSuccessAt={f.Time.GetUtcNow()},Cursor=7", Ct);
        }
        return sync;
    }
    private static PersonalSyncViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment) => new(
        f.Services.GetRequiredService<PersonalSyncStore>(), f.Services.GetRequiredService<PersonalSyncCoordinator>(), environment,
        f.Platform, f.Translator, new DateFormatter(f.Localization), f.Localization);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task First_run_receive_only_mode_refuses_existing_accounts_or_independent_financial_goals(bool accounts)
    {
        using var f = new FlowFixture(); var sync = await SeedAsync(f, accounts, false);
        if (!accounts) await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(new()
            { Name = "Fictitious existing independent goal", CurrencyCode = "KWD", TargetAmount = 10000 }, Ct);
        var before = await sync.StatusAsync(Ct); var review = await sync.ReviewAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sync.ConnectAsync(review, new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-first-run-owner", review.DeviceId!.Value, new(Guid.NewGuid(), "Fictitious selected replacement", SyncSchema.Hash, 7, false), Ct, receiveFirstRun: true));
        Assert.Equal(review, await sync.ReviewAsync(Ct)); Assert.Equal(before.PendingChanges, (await sync.StatusAsync(Ct)).PendingChanges);
        Assert.Equal(accounts ? 1 : 0, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Count);
        if (!accounts) Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
    }

    [Fact]
    public async Task First_run_receive_only_reconnect_preserves_its_own_frozen_empty_round_and_does_not_queue_setup_defaults()
    {
        using var f = new FlowFixture(); var sync = await SeedAsync(f, false, false); var review = await sync.ReviewAsync(Ct);
        var remote = new SyncProfile(review.RemoteProfile!.Value, "Fictitious received space", SyncSchema.Hash, 7, false);
        var peer = new LocalSyncPeer("https://127.0.0.1:5048/", new('1', 64));
        await sync.ConnectAsync(review, peer, review.IdentityId!, review.DeviceId!.Value, remote, Ct, receiveFirstRun: true);
        var operation = Guid.NewGuid(); var hash = SyncProtocolDigest.Round([]);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,Cutoff,Phase,PartCount,SentParts,ContentHash,ReadCursor) VALUES(1,{operation},0,1,0,0,{hash},0)", Ct);
        await sync.ConnectAsync(await sync.ReviewAsync(Ct), peer, review.IdentityId!, review.DeviceId.Value, remote, Ct, receiveFirstRun: true);
        Assert.Equal(0, (await sync.StatusAsync(Ct)).PendingChanges);
        await using var context = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await context.Database.OpenConnectionAsync(Ct); await using var query = context.Database.GetDbConnection().CreateCommand();
        query.CommandText = "SELECT OperationId FROM SyncClientRound";
        Assert.Equal(operation, Guid.Parse((string)(await query.ExecuteScalarAsync(Ct))!));
    }

    [Fact]
    public async Task Explicit_first_run_completion_uses_existing_accounts_and_preserves_received_preferences()
    {
        using var f = new FlowFixture(); await SeedAsync(f); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var finished = 0; var vm = Create(f, environment); vm.BeginOnboarding(() => Task.CompletedTask, () => { finished++; return Task.CompletedTask; });
        Assert.False(vm.CanFinishOnboarding); await vm.ActivateAsync(); Assert.True(vm.CanFinishOnboarding);
        var before = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        f.Platform.Confirmations.Enqueue(true); await vm.FinishOnboardingCommand.ExecuteAsync(null);
        Assert.Null(vm.Error); Assert.Equal(1, finished); Assert.Equal(0, environment.Opens);
        var after = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(before.Id, after.Id); Assert.Equal(12345, after.OpeningBalance);
        var settings = await f.Store.GetSettingsAsync(Ct); Assert.True(settings.OnboardingCompleted);
        Assert.Equal("KWD", settings.ReportCurrencyCode); Assert.Equal("JPY", settings.DefaultCurrencyCode);
        Assert.Equal(Vafadar.Zanance.Core.Settings.ExperienceMode.Advanced, settings.Mode); vm.Deactivate();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Empty_or_unacknowledged_profile_cannot_bypass_first_run(bool account, bool acknowledged)
    {
        using var f = new FlowFixture(); var sync = await SeedAsync(f, account, acknowledged); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var finished = 0; var vm = Create(f, environment); vm.BeginOnboarding(() => Task.CompletedTask, () => { finished++; return Task.CompletedTask; });
        await vm.ActivateAsync(); Assert.False(vm.CanFinishOnboarding);
        Assert.False(await sync.CompleteOnboardingAsync(await sync.ReviewAsync(Ct), Ct));
        await vm.FinishOnboardingCommand.ExecuteAsync(null); Assert.Equal(0, finished); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.False((await f.Store.GetSettingsAsync(Ct)).OnboardingCompleted); vm.Deactivate();
    }

    [Fact]
    public async Task Cancelled_first_run_completion_and_back_leave_settings_and_financial_accounts_untouched()
    {
        using var f = new FlowFixture(); await SeedAsync(f); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var finished = 0; var closed = 0; var vm = Create(f, environment);
        vm.BeginOnboarding(() => { closed++; return Task.CompletedTask; }, () => { finished++; return Task.CompletedTask; });
        await vm.ActivateAsync(); f.Platform.Confirmations.Enqueue(false); await vm.FinishOnboardingCommand.ExecuteAsync(null);
        await vm.CloseOnboardingCommand.ExecuteAsync(null);
        Assert.Equal(0, finished); Assert.Equal(1, closed); Assert.False((await f.Store.GetSettingsAsync(Ct)).OnboardingCompleted);
        Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(0, environment.Opens); vm.Deactivate();
    }

    [Fact]
    public async Task Changed_connection_during_first_run_confirmation_cannot_complete_another_binding()
    {
        using var f = new FlowFixture(); var sync = await SeedAsync(f); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var finished = 0; var vm = Create(f, environment); vm.BeginOnboarding(() => Task.CompletedTask, () => { finished++; return Task.CompletedTask; });
        await vm.ActivateAsync(); Assert.True(vm.CanFinishOnboarding);
        f.Platform.PendingConfirmation = async () => { await sync.PauseAsync(await sync.ReviewAsync(Ct), Ct); return true; };
        await vm.FinishOnboardingCommand.ExecuteAsync(null);
        Assert.Equal(0, finished); Assert.False(vm.CanFinishOnboarding); Assert.NotNull(vm.Error);
        Assert.False((await f.Store.GetSettingsAsync(Ct)).OnboardingCompleted); vm.Deactivate();
    }
}
