using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>A session read cannot authorize revocation after its reviewed catalog or original access retires.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Original_revoke_does_not_send_after_review_or_access_retires_in_session_read(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        var target = new SyncDevice(Guid.NewGuid(), "Fictitious selected device", DateTimeOffset.UtcNow, false);
        host.CatalogDevices = [target]; host.RevocableDevice = target.Id;
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-revocation-access", RefreshToken = "fictitious-revocation-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment); var root = true; model.BindRootOwner(() => root);
        await model.ActivateAsync(); model.Address = host.Peer.Address.AbsoluteUri; model.Fingerprint = host.Peer.Fingerprint;
        await model.DiscoverCommand.ExecuteAsync(null); Assert.True(model.CanConnect);
        var store = fixture.Services.GetRequiredService<PersonalSyncStore>(); var before = await store.ReviewAsync(Ct);
        var consents = 0; var interceptions = 0;
        fixture.Platform.PendingConfirmation = () => { consents++; return Task.FromResult(true); };
        host.Tokens.BeforeRead = () =>
        {
            if (interceptions++ != 0) return;
            if (scenario == 0) model.Devices.Clear();
            else if (scenario == 1) model.DeviceItems.Clear();
            else if (scenario == 2) model.Profiles.Clear();
            else if (scenario == 3) root = false;
            else if (scenario == 4) fixture.Localization.SetLanguage(fixture.Localization.SupportedLanguages.First(row => row.CultureName == "de"));
            else if (scenario == 5) model.Devices[0] = target with { Name = "Different unreviewed device label" };
        };
        try
        {
            await model.RevokeCommand.ExecuteAsync(target);
            Assert.Equal(1, consents); Assert.True(interceptions >= 1);
            Assert.Equal(scenario == 6 ? 1 : 0, host.DeviceRevocations);
            Assert.Equal(before, await store.ReviewAsync(Ct));
            Assert.Equal(0, environment.ConnectionsChanged);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
            if (scenario == 6) { Assert.Null(model.Error); Assert.True(model.Devices.Single().Revoked); }
        }
        finally { model.Deactivate(); }
    }
}
