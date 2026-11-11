using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>A pending original consent cannot authorize a catalog that changed before its answer.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Original_connect_refuses_catalog_retirement_while_native_consent_is_pending(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogProfiles = [new(Guid.NewGuid(), "Fictitious reviewed space", SyncSchema.Hash, 0, false)];
        host.CatalogDevices = [new(Guid.NewGuid(), "Fictitious reviewed device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-reviewed-access",
            RefreshToken = "fictitious-reviewed-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri;
        model.Fingerprint = host.Peer.Fingerprint;
        await model.DiscoverCommand.ExecuteAsync(null);
        Assert.True(model.CanConnect);
        var store = fixture.Services.GetRequiredService<PersonalSyncStore>();
        var before = await store.ReviewAsync(Ct);
        var confirmations = 0;
        fixture.Platform.PendingConfirmation = () =>
        {
            confirmations++;
            if (scenario == 0) model.Profiles.Clear();
            else if (scenario == 1) model.Devices.Clear();
            else model.DeviceItems.Clear();
            return Task.FromResult(true);
        };
        try
        {
            await model.ConnectCommand.ExecuteAsync(host.CatalogProfiles[0]);
            Assert.Equal(1, confirmations);
            Assert.False(model.SignedIn);
            Assert.Equal(before, await store.ReviewAsync(Ct));
            Assert.Equal(0, environment.ConnectionsChanged);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
