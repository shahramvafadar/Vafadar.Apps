using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Late busy feedback and retained collections cannot keep a previously published session usable.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Original_catalog_authority_retires_after_terminal_or_retained_row_mutation(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogProfiles = [new(Guid.NewGuid(), "Fictitious retained copy", SyncSchema.Hash, 0, false)];
        host.CatalogDevices = [new(Guid.NewGuid(), "Fictitious retained device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-retained-access",
            RefreshToken = "fictitious-retained-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri;
        model.Fingerprint = host.Peer.Fingerprint;
        var fired = false;
        model.PropertyChanged += (_, e) =>
        {
            if (fired || scenario == 3 || e.PropertyName != nameof(model.IsBusy) || model.IsBusy) return;
            fired = true;
            if (scenario == 0) model.Profiles.Clear();
            else if (scenario == 1) model.Devices.Clear();
            else model.DeviceItems.Clear();
        };
        try
        {
            await model.DiscoverCommand.ExecuteAsync(null);
            if (scenario == 3) { fired = true; model.Profiles.Clear(); }
            await model.ReloadCommand.ExecuteAsync(null);
            Assert.True(fired);
            Assert.False(model.SignedIn);
            Assert.False(model.CanConnect);
            Assert.False(model.HasPendingOperation);
            var requests = host.Requests;
            var confirmations = 0;
            fixture.Platform.PendingConfirmation = () => { confirmations++; return Task.FromResult(false); };
            await model.ConnectCommand.ExecuteAsync(host.CatalogProfiles[0]);
            Assert.Equal(0, confirmations);
            Assert.Equal(requests, host.Requests);
            Assert.Equal(0, host.Registrations);
            Assert.Equal(1, host.Tokens.Writes);
        }
        finally { model.Deactivate(); }
    }
}
