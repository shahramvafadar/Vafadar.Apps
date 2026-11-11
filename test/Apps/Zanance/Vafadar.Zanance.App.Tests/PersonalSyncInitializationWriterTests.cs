using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>A catalog retired inside the actual original SQLite writer cannot retain its connection commit.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Original_initialization_writer_refuses_catalog_retirement_before_commit(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        var original = new SyncProfile(Guid.NewGuid(), "Fictitious writer initialization", SyncSchema.Hash, 0, true, Guid.NewGuid());
        host.CatalogProfiles = [original];
        host.CatalogDevices = [new(host.CurrentDeviceId, "Fictitious current device", DateTimeOffset.UtcNow, false)];
        host.InitializationResponse = original with { BootstrapDeviceId = host.CurrentDeviceId };
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-writer-access", RefreshToken = "fictitious-writer-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var factory = new InitializationWriterFactory(fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new PersonalSyncStore(factory, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>(), fixture.Time);
        var model = new PersonalSyncViewModel(store, fixture.Services.GetRequiredService<PersonalSyncCoordinator>(),
            environment, fixture.Platform, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri; model.Fingerprint = host.Peer.Fingerprint;
        await model.DiscoverCommand.ExecuteAsync(null);
        Assert.True(model.CanConnect);
        var before = await store.ReviewAsync(Ct);
        fixture.Platform.PendingConfirmation = () => Task.FromResult(true);
        host.BeforeInitializationResponse = () => factory.BeforeSaving = () =>
        {
            if (scenario == 0) model.Profiles.Clear();
            else if (scenario == 1) model.Devices.Clear();
            else model.DeviceItems.Clear();
        };
        try
        {
            await model.RecoverInitializationCommand.ExecuteAsync(original);
            Assert.Equal(1, host.InitializationClaims);
            Assert.Equal(1, factory.Interleavings);
            Assert.Equal(before, await store.ReviewAsync(Ct));
            Assert.Equal(0, environment.ConnectionsChanged);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }

    /// <summary>Retains the original context factory and inserts only one actual SavingChanges observation.</summary>
    private sealed class InitializationWriterFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal Action? BeforeSaving;
        internal int Interleavings;
        public ZananceDbContext CreateDbContext() => Observe(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Observe(await original.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Observe(ZananceDbContext context)
        {
            context.SavingChanges += (_, _) =>
            {
                var action = BeforeSaving;
                if (action is null) return;
                BeforeSaving = null; Interleavings++; action();
            };
            return context;
        }
    }
}
