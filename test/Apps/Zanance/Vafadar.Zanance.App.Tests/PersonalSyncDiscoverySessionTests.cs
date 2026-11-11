namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Unchanged discovery uses its existing synthetic session without replaying sign-in or device registration.</summary>
    [Fact]
    public async Task Original_discovery_uses_existing_synthetic_session_without_sign_in_or_registration()
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-access-for-local-discovery",
            RefreshToken = "fictitious-refresh-for-local-discovery",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri;
        model.Fingerprint = host.Peer.Fingerprint;
        try
        {
            await model.DiscoverCommand.ExecuteAsync(null);
            Assert.Null(model.Error);
            Assert.True(model.SignedIn);
            Assert.Equal("Fictitious original device", model.DeviceName);
            Assert.Equal(0, host.Registrations);
            Assert.Equal(1, host.Tokens.Writes);
            Assert.Equal(4, host.Tokens.Reads);
            Assert.Equal(1, environment.Opens);
            Assert.Equal(0, environment.RememberedPeers);
            Assert.Equal(0, environment.ConnectionsChanged);
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
