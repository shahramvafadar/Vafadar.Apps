using System.Collections.Specialized;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Caption feedback cannot borrow a competing local-read owner during synchronous publication.</summary>
    [Fact]
    public async Task Original_device_caption_feedback_cannot_start_a_competing_read()
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogDevices = [new(Guid.NewGuid(), "Fictitious reentry device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-reentry-access",
            RefreshToken = "fictitious-reentry-refresh",
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
            var captures = environment.Captures;
            Task? competitor = null;
            model.DeviceItems.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset && competitor is null)
                    competitor = model.ReloadCommand.ExecuteAsync(null);
            };
            fixture.Localization.SetLanguage(fixture.Localization.SupportedLanguages.First(row => row.CultureName == "de"));
            Assert.NotNull(competitor);
            await competitor;
            Assert.Equal(captures, environment.Captures);
            Assert.True(model.SignedIn);
            Assert.True(model.CanConnect);
            Assert.False(model.HasPendingOperation);
            Assert.Equal(0, host.Registrations);
            Assert.Equal(1, host.Tokens.Writes);
        }
        finally { model.Deactivate(); }
    }
}
