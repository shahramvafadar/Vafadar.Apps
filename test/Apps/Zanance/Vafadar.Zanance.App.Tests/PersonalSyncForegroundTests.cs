using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual foreground orchestration against SQLite; native lifetime/transport effects are explicit ports.</summary>
public sealed class PersonalSyncForegroundTests
{
    /// <summary>An unverified marker is not membership, but it must still prevent readable personal fallback.</summary>
    [Fact, Trait("AT", "AT-294")]
    public async Task A_household_kind_without_its_original_encrypted_channel_cannot_fall_back_to_personal_HTTP()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        await ConnectAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE SyncBinding SET TransportKind=1", Ct);
        await Create(f, environment).RunAsync(true, Ct);
        Assert.Equal(0, environment.Opens);
        Assert.Equal("service-unavailable", (await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Failure);
    }
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static PersonalSyncForegroundCoordinator Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment) => new(
        f.Services.GetRequiredService<PersonalSyncStore>(), f.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, f.Time);

    private static async Task<PersonalSyncReview> ConnectAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await store.ConnectAsync(await store.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious copy", SyncSchema.Hash, 0, false), Ct);
        return await store.ReviewAsync(Ct);
    }

    [Fact]
    public async Task Never_opted_in_and_offline_editions_do_not_capture_or_read_native_connection_state()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var worker = Create(f, environment); await worker.RunAsync(false, Ct);
        environment.Available = false; await worker.RunAsync(true, Ct);
        Assert.Equal(0, environment.Captures); Assert.Equal(0, environment.Reads); Assert.Equal(0, environment.Opens);
    }

    [Fact]
    public async Task A_scheduling_hint_cannot_opt_an_unconnected_profile_in_or_read_credentials()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        await Create(f, environment).RunAsync(true, Ct);
        Assert.Equal(0, environment.Reads); Assert.Equal(0, environment.Opens);
        Assert.False((await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Connected);
    }

    [Fact]
    public async Task Restored_generation_metadata_is_not_automatic_network_authority()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        await ConnectAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE SyncBinding SET GenerationPath='fictitious-old-generation';", Ct);
        await Create(f, environment).RunAsync(true, Ct);
        Assert.Equal(0, environment.Opens); Assert.Equal(0, environment.Reads);
        Assert.True((await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).PausedAfterRestore);
    }

    [Fact]
    public async Task A_saved_peer_for_another_profile_cannot_receive_this_profiles_automatic_request()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort { Fingerprint = new('2', 64) };
        await ConnectAsync(f); await Create(f, environment).RunAsync(true, Ct);
        Assert.Equal(0, environment.Opens);
        var status = await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct);
        Assert.True(status.Connected); Assert.Equal("connection-review-required", status.Failure);
    }

    [Fact]
    public async Task Outage_feedback_contains_no_exception_payload_and_failed_attempts_back_off()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenError = new HttpRequestException("Fictitious private payload must never be retained.") };
        await ConnectAsync(f); var worker = Create(f, environment); var changes = 0; worker.Changed += (_, _) => changes++;
        await worker.RunAsync(true, Ct); await worker.RunAsync(true, Ct);
        Assert.Equal(1, environment.Opens); Assert.Equal(1, changes);
        Assert.Equal("service-unavailable", (await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Failure);
        f.Time.Now = f.Time.Now.AddSeconds(30); await worker.RunAsync(true, Ct);
        Assert.Equal(2, environment.Opens);
        f.Time.Now = f.Time.Now.AddSeconds(30); await worker.RunAsync(true, Ct); Assert.Equal(2, environment.Opens);
        f.Time.Now = f.Time.Now.AddSeconds(30); await worker.RunAsync(true, Ct); Assert.Equal(3, environment.Opens);
    }

    [Fact]
    public async Task Refused_session_waits_for_explicit_action_instead_of_periodically_retrying_credentials()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenError = new PersonalSyncHttpException(HttpStatusCode.Unauthorized) };
        await ConnectAsync(f); var worker = Create(f, environment); await worker.RunAsync(true, Ct);
        f.Time.Now = f.Time.Now.AddDays(10); await worker.RunAsync(true, Ct); Assert.Equal(1, environment.Opens);
        Assert.Equal("sign-in-required", (await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Failure);
        worker.Reset(); await worker.RunAsync(true, Ct); Assert.Equal(1, environment.Opens);
        await ConnectAsync(f); worker.Reset(); await worker.RunAsync(true, Ct); Assert.Equal(2, environment.Opens);
    }

    [Fact]
    public async Task Access_retired_after_queueing_cannot_be_recaptured_as_authority_for_a_new_page()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        await ConnectAsync(f); var captured = environment.Capture(); environment.Lifetime.Cancel();
        await Create(f, environment).RunAsync(true, Ct, captured);
        Assert.Equal(1, environment.Captures); Assert.Equal(0, environment.Reads); Assert.Equal(0, environment.Opens);
        Assert.Null((await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Failure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_retired_binding_cannot_replace_the_new_connections_status(bool sameSpace)
    {
        using var f = new FlowFixture(); var old = await ConnectAsync(f);
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await store.ConnectAsync(old, new("https://127.0.0.1:5048/", new('1', 64)), "fictitious-owner", Guid.NewGuid(),
            new(sameSpace ? old.RemoteProfile!.Value : Guid.NewGuid(), "Fictitious replacement", SyncSchema.Hash, 0, false), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReportFailureAsync(old, PersonalSyncFailure.ServiceUnavailable, Ct));
        Assert.Null((await store.StatusAsync(Ct)).Failure);
    }

    [Fact]
    public async Task Visible_status_refreshes_from_fixed_feedback_and_is_cleared_on_explicit_pause()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var store = f.Services.GetRequiredService<PersonalSyncStore>(); var review = await ConnectAsync(f);
        var vm = new PersonalSyncViewModel(store, f.Services.GetRequiredService<PersonalSyncCoordinator>(), environment,
            f.Platform, f.Translator, new DateFormatter(f.Localization), f.Localization);
        await vm.ActivateAsync();
        await store.ReportFailureAsync(review, PersonalSyncFailure.ServiceUnavailable, Ct);
        environment.PublishStatus();
        await vm.PendingStatusRefresh.WaitAsync(Ct);
        Assert.False(vm.IsBusy); Assert.Equal(f.Translator["Sync_ServiceUnavailable"], vm.AutomaticError);
        f.Platform.Confirmations.Enqueue(true); await vm.PauseCommand.ExecuteAsync(null); Assert.Null(vm.AutomaticError);
        vm.Deactivate();
    }
}
