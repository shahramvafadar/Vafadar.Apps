using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Creation retains its invocation, current catalog and an independently edited draft after acceptance.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Original_creation_keeps_entry_catalog_and_newer_draft_ownership(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogDevices = [new(Guid.NewGuid(), "Fictitious current device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-creation-access", RefreshToken = "fictitious-creation-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        var nonces = 0;
        var completed = 0;
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        {
            OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify),
            CreateOperation = _ =>
            {
                nonces++;
                if (scenario < 5) throw new InvalidOperationException("Refused before a fictitious remote side effect.");
                return Guid.NewGuid();
            },
            CompleteOperation = (_, _) => completed++
        };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri; model.Fingerprint = host.Peer.Fingerprint;
        await model.DiscoverCommand.ExecuteAsync(null);
        Assert.True(model.CanConnect);
        model.NewProfileName = "Original fictitious creation";
        var changed = false;
        void ChangeName(string? property, bool after)
        {
            if (changed || property != nameof(model.IsBusy) || scenario > 1 || after != (scenario == 1)) return;
            changed = true; model.NewProfileName = "Newer unsubmitted fictitious creation";
        }
        model.PropertyChanging += (_, e) => ChangeName(e.PropertyName, false);
        model.PropertyChanged += (_, e) => ChangeName(e.PropertyName, true);
        if (scenario == 5) environment.AfterConnectionChanged = () => model.NewProfileName = "Newer unsubmitted fictitious creation";
        var confirmations = 0;
        fixture.Platform.PendingConfirmation = () =>
        {
            confirmations++;
            if (scenario == 2) model.Profiles.Clear();
            else if (scenario == 3) model.Devices.Clear();
            else if (scenario == 4) model.DeviceItems.Clear();
            return Task.FromResult(true);
        };
        try
        {
            await model.CreateProfileCommand.ExecuteAsync(null);
            Assert.Equal(scenario < 2 ? 0 : 1, confirmations);
            Assert.Equal(scenario < 5 ? 0 : 1, nonces);
            Assert.Equal(scenario < 5 ? 0 : 1, host.ProfileCreations);
            Assert.Equal(scenario < 5 ? 0 : 1, completed);
            Assert.Equal(scenario < 5 ? 0 : 1, environment.ConnectionsChanged);
            Assert.Equal(scenario is 0 or 1 or 5 ? "Newer unsubmitted fictitious creation" : scenario == 6 ? "" : "Original fictitious creation", model.NewProfileName);
            var review = await fixture.Services.GetRequiredService<PersonalSyncStore>().ReviewAsync(Ct);
            Assert.Equal(scenario >= 5, review.RemoteProfile is not null);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
