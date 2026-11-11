using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Initialization's original command binds its returned claim and local writer to the reviewed catalog.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task Original_initialization_claim_cannot_replace_its_reviewed_catalog_or_response(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        var original = new SyncProfile(Guid.NewGuid(), "Fictitious reviewed initialization", SyncSchema.Hash,
            0, true, Guid.NewGuid());
        host.CatalogProfiles = [original];
        host.CatalogDevices = [new(host.CurrentDeviceId, "Fictitious current device", DateTimeOffset.UtcNow, false)];
        var claimed = original with { BootstrapDeviceId = host.CurrentDeviceId };
        host.InitializationResponse = scenario switch
        {
            3 => claimed with { Id = Guid.NewGuid() },
            4 => claimed with { Name = "Different unreviewed initialization" },
            5 => claimed with { Cursor = 1 },
            6 => claimed with { Initializing = false },
            7 => claimed with { BootstrapDeviceId = Guid.NewGuid() },
            _ => claimed,
        };
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-initialization-access", RefreshToken = "fictitious-initialization-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri; model.Fingerprint = host.Peer.Fingerprint;
        await model.DiscoverCommand.ExecuteAsync(null);
        Assert.True(model.CanConnect);
        var store = fixture.Services.GetRequiredService<PersonalSyncStore>();
        var before = await store.ReviewAsync(Ct);
        var confirmations = 0;
        fixture.Platform.PendingConfirmation = () => { confirmations++; return Task.FromResult(true); };
        host.BeforeInitializationResponse = () =>
        {
            if (scenario == 0) model.Profiles.Clear();
            else if (scenario == 1) model.Devices.Clear();
            else if (scenario == 2) model.DeviceItems.Clear();
        };
        try
        {
            await model.RecoverInitializationCommand.ExecuteAsync(original);
            Assert.Equal(1, confirmations);
            Assert.Equal(1, host.InitializationClaims);
            var after = await store.ReviewAsync(Ct);
            if (scenario < 8)
            {
                Assert.Equal(before, after);
                Assert.Equal(0, environment.ConnectionsChanged);
            }
            else
            {
                Assert.Null(model.Error);
                Assert.Equal(original.Id, after.RemoteProfile);
                Assert.Equal(before.GenerationPath, after.GenerationPath);
                Assert.Equal(1, environment.ConnectionsChanged);
            }
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
