using System.Collections.Specialized;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Incomplete remote publication cannot regain session authority through a local-only reload.</summary>
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
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task Original_catalog_authority_requires_complete_remote_publication_after_local_retry(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogProfiles = [new(Guid.NewGuid(), "First fictitious copy", SyncSchema.Hash, 0, false),
            new(Guid.NewGuid(), "Second fictitious copy", SyncSchema.Hash, 0, false)];
        host.CatalogDevices = [new(Guid.NewGuid(), "First fictitious device", DateTimeOffset.UtcNow, false),
            new(Guid.NewGuid(), "Second fictitious device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-catalog-access",
            RefreshToken = "fictitious-catalog-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri;
        model.Fingerprint = host.Peer.Fingerprint;
        var fired = false;
        void Observe(int collection, NotifyCollectionChangedEventArgs args)
        {
            if (fired || scenario == 12) return;
            var mode = scenario % 6;
            if (mode / 2 != collection || args.Action != (mode % 2 == 0 ? NotifyCollectionChangedAction.Reset : NotifyCollectionChangedAction.Add)) return;
            fired = true;
            if (scenario < 6) throw new InvalidOperationException("Fictitious catalog feedback failure.");
            if (mode % 2 != 0)
            {
                if (collection == 0) model.Profiles.Clear();
                else if (collection == 1) model.Devices.Clear();
                else model.DeviceItems.Clear();
            }
            else if (collection == 0) model.Profiles.Add(new(Guid.NewGuid(), "Unreviewed fictitious copy", SyncSchema.Hash, 0, false));
            else if (collection == 1) model.Devices.Add(new(Guid.NewGuid(), "Unreviewed fictitious device", DateTimeOffset.UtcNow, false));
            else model.DeviceItems.Add(new(host.CatalogDevices[0], "Unreviewed fictitious caption"));
        }
        model.Profiles.CollectionChanged += (_, e) => Observe(0, e);
        model.Devices.CollectionChanged += (_, e) => Observe(1, e);
        model.DeviceItems.CollectionChanged += (_, e) => Observe(2, e);
        try
        {
            await model.DiscoverCommand.ExecuteAsync(null);
            await model.ReloadCommand.ExecuteAsync(null);
            Assert.Equal(scenario != 12, fired);
            Assert.Equal(scenario == 12, model.SignedIn);
            Assert.Equal(scenario == 12, model.CanConnect);
            Assert.False(model.HasPendingOperation);
            if (scenario != 12)
            {
                var confirmations = 0;
                fixture.Platform.PendingConfirmation = () => { confirmations++; return Task.FromResult(false); };
                var requests = host.Requests;
                await model.ConnectCommand.ExecuteAsync(host.CatalogProfiles[0]);
                Assert.Equal(0, confirmations);
                Assert.Equal(requests, host.Requests);
            }
            else
            {
                Assert.Equal(host.CatalogProfiles, model.Profiles.ToArray());
                Assert.Equal(host.CatalogDevices, model.Devices.ToArray());
                Assert.Equal(host.CatalogDevices, model.DeviceItems.Select(item => item.Source).ToArray());
            }
            Assert.Equal(0, host.Registrations);
            Assert.Equal(1, host.Tokens.Writes);
            Assert.Equal(0, environment.RememberedPeers);
            Assert.Equal(0, environment.ConnectionsChanged);
        }
        finally { model.Deactivate(); }
    }
}
