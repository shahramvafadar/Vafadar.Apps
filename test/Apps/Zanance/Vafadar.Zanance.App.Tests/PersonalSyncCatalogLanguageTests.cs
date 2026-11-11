using System.Collections.Specialized;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Rebuilding device captions cannot retain catalog authority after failure or reentrant alteration.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Original_device_caption_refresh_requires_complete_current_publication(int scenario)
    {
        using var fixture = new FlowFixture();
        await using var host = await ProtocolHost.StartAsync();
        host.CatalogDevices = [new(Guid.NewGuid(), "First fictitious caption device", DateTimeOffset.UtcNow, false),
            new(Guid.NewGuid(), "Second fictitious caption device", DateTimeOffset.UtcNow, false)];
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-caption-access",
            RefreshToken = "fictitious-caption-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { OpenTransport = (peer, verify) => new(peer, host.Tokens, verifyAccess: verify) };
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Address = host.Peer.Address.AbsoluteUri;
        model.Fingerprint = host.Peer.Fingerprint;
        var fired = false;
        try
        {
            await model.DiscoverCommand.ExecuteAsync(null);
            Assert.True(model.SignedIn);
            model.DeviceItems.CollectionChanged += (_, e) =>
            {
                if (fired || scenario == 2 || e.Action != (scenario == 0 ? NotifyCollectionChangedAction.Reset : NotifyCollectionChangedAction.Add)) return;
                fired = true;
                if (scenario == 0) throw new InvalidOperationException("Fictitious caption feedback failure.");
                model.DeviceItems.Clear();
            };
            var escaped = Record.Exception(() => fixture.Localization.SetLanguage(
                fixture.Localization.SupportedLanguages.First(row => row.CultureName == "de")));
            Assert.Equal(scenario != 2, fired);
            if (scenario == 2) Assert.Null(escaped);
            await model.ReloadCommand.ExecuteAsync(null);
            Assert.Equal(scenario == 2, model.SignedIn);
            Assert.Equal(scenario == 2, model.CanConnect);
            Assert.False(model.HasPendingOperation);
            if (scenario == 2) Assert.Equal(host.CatalogDevices, model.DeviceItems.Select(item => item.Source).ToArray());
            Assert.Equal(0, host.Registrations);
            Assert.Equal(1, host.Tokens.Writes);
        }
        finally { model.Deactivate(); }
    }
}
