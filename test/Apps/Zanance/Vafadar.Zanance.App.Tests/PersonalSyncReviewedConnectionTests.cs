using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>The same original command still commits a current explicit connection when its catalog is unchanged.</summary>
    [Fact]
    public async Task Original_unchanged_catalog_can_complete_one_reviewed_connection()
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
        fixture.Platform.PendingConfirmation = () => { confirmations++; return Task.FromResult(true); };
        try
        {
            await model.ConnectCommand.ExecuteAsync(host.CatalogProfiles[0]);
            var after = await store.ReviewAsync(Ct);
            Assert.Equal(1, confirmations);
            Assert.Null(model.Error);
            Assert.Equal(before.GenerationPath, after.GenerationPath);
            Assert.Equal(host.CatalogProfiles[0].Id, after.RemoteProfile);
            Assert.Equal(1, environment.ConnectionsChanged);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
