using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original discovery must retain its entry inputs before feedback and before a session read.</summary>
public sealed class PersonalSyncDiscoveryEntryTests
{
    /// <summary>Changed entry/open callbacks refuse new discovery; unchanged input reaches only the empty synthetic session store.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Original_discovery_revalidates_before_constructing_or_reading_a_session(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new DiscoveryEnvironment();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        await model.ActivateAsync();
        var fired = false;
        void Mutate(int stage)
        {
            if (fired || scenario != stage) return;
            fired = true;
            if (scenario is 0 or 3) model.Address = "https://127.0.0.1:5099/";
            else if (scenario == 1) model.Fingerprint = new string('2', 64);
            else model.DeviceName = "Newer fictitious discovery device";
        }
        model.PropertyChanging += (_, e) => { if (e.PropertyName == nameof(model.IsBusy)) Mutate(0); };
        model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(model.IsBusy)) Mutate(1); };
        environment.AfterCapture = () => Mutate(2);
        environment.AfterOpen = () => Mutate(3);
        try
        {
            await model.DiscoverCommand.ExecuteAsync(null);
            Assert.Equal(scenario != 4, fired);
            Assert.Equal(scenario >= 3 ? 1 : 0, environment.Inner.Opens);
            Assert.Equal(scenario == 4 ? 1 : 0, environment.Sessions.Reads);
            Assert.Equal(2, environment.Inner.Captures);
            Assert.False(model.SignedIn);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanInteract);
            Assert.Equal(0, environment.Inner.RememberedPeers);
            Assert.Equal(0, environment.Inner.ConnectionsChanged);
            if (scenario is 0 or 3) Assert.Equal("https://127.0.0.1:5099/", model.Address);
            if (scenario == 1) Assert.Equal(new string('2', 64), model.Fingerprint);
            if (scenario == 2) Assert.Equal("Newer fictitious discovery device", model.DeviceName);
        }
        finally { model.Deactivate(); }
    }

    /// <summary>A deliberately empty in-memory store proves whether original discovery reaches credential lookup.</summary>
    private sealed class EmptySessions : IPersonalSyncTokenStore
    {
        internal int Reads;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct)
        { Reads++; return Task.FromResult<PersonalSyncSession?>(null); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
    }

    /// <summary>Delegates original local effects while exposing only the two controlled request-entry callbacks.</summary>
    private sealed class DiscoveryEnvironment : IPersonalSyncEnvironment, IDisposable
    {
        internal readonly PersonalSyncFlowTests.EnvironmentPort Inner = new();
        internal readonly EmptySessions Sessions = new();
        internal Action? AfterCapture, AfterOpen;
        public bool Available => Inner.Available;
        public event EventHandler? StatusChanged { add => Inner.StatusChanged += value; remove => Inner.StatusChanged -= value; }
        public PersonalSyncConnection ReadConnection() => Inner.ReadConnection();
        public PersonalSyncAccess Capture() { var access = Inner.Capture(); AfterCapture?.Invoke(); return access; }
        public PersonalSyncTransport Open(LocalSyncPeer peer, Action verifyAccess)
        { Inner.Opens++; var client = new PersonalSyncTransport(peer, Sessions, verifyAccess: verifyAccess); AfterOpen?.Invoke(); return client; }
        public void RememberPeer(LocalSyncPeer peer) => Inner.RememberPeer(peer);
        public Task PublishAppliedAsync(CancellationToken ct) => Inner.PublishAppliedAsync(ct);
        public void ConnectionChanged() => Inner.ConnectionChanged();
        public Guid CreationOperation(string requestDigest) => Inner.CreationOperation(requestDigest);
        public void CompleteCreation(string requestDigest, Guid operation) => Inner.CompleteCreation(requestDigest, operation);
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int pageIndex, CancellationToken ct) =>
            Inner.RenderAttachmentPageAsync(content, pageIndex, ct);
        public void Dispose() => Inner.Dispose();
    }
}
