using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Rates;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Actual native private cache, lock/foreground/catalogue lifetime and fixed identity transport for reference prices only.</summary>
internal sealed class MauiMarketRuntime(PersonalSyncLifetime lifetime, IPersonalSyncTokenStore tokens,
    IMarketPrivateCache storage, TimeProvider time, IAppInteraction interaction) : IMarketRuntime
{
    private static LocalSyncPeer Peer => new("https://api.zanance.vafadar.pro/", "");
    /// <inheritdoc />
    public bool NetworkAvailable => PersonalSyncBuild.IsAvailable || Commerce.Billing.BillingBuild.IsAvailable;
    /// <inheritdoc />
    public Task OpenAccountAsync() => Shell.Current.GoToAsync(AppRoutes.BillingIdentityRoute);
    /// <inheritdoc />
    public MarketUiAccess Capture()
    {
        var access = lifetime.CaptureMarket(false);
        return new(access.Scope ?? throw new InvalidOperationException("Original market file unavailable."), access.Validate, access.Cancellation);
    }
    private MarketQuoteClient Client(MarketUiAccess access) => new(storage, Peer, access.File, time, access.Validate);
    /// <inheritdoc />
    public async Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct)
    {
        access.Validate(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, access.Cancellation);
        var result = await Client(access).ReadAsync(linked.Token); access.Validate(); return result;
    }
    /// <inheritdoc />
    public async Task<IMarketRemote> OpenAsync(MarketUiAccess original, CancellationToken ct)
    {
        original.Validate(); var access = lifetime.CaptureMarket(true);
        if (access.Scope != original.File) throw new OperationCanceledException("Original market file changed.");
        void Validate() { original.Validate(); access.Validate(); }
        var transport = new PersonalSyncTransport(Peer, tokens, time, Validate);
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, original.Cancellation, access.Cancellation);
            var user = await transport.BillingIdentityAsync(linked.Token) ?? throw new InvalidOperationException("MarketIdentityRequired"); Validate();
            return new Remote(transport, new(storage, Peer, original.File, time, Validate), user, access, original);
        }
        catch { transport.Dispose(); throw; }
    }
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string originalTerms, string accept, string cancel) =>
        interaction.ConfirmAsync(title, originalTerms, accept, cancel);
    private sealed class Remote(PersonalSyncTransport transport, MarketQuoteClient client, string identity,
        PersonalSyncAccess access, MarketUiAccess original) : IMarketRemote
    {
        public string Destination => transport.Peer.Address.AbsoluteUri;
        public Task<MarketAvailability> AvailabilityAsync(CancellationToken ct) => Work(token => client.AvailabilityAsync(transport, identity, token), ct);
        public Task<MarketQuote> FetchAsync(MarketCatalog catalog, MarketRequest request, CancellationToken ct) =>
            Work(token => client.FetchAsync(transport, identity, catalog, request, token), ct);
        private async Task<T> Work<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            original.Validate(); access.Validate();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, original.Cancellation, access.Cancellation);
            var result = await action(linked.Token); original.Validate(); access.Validate(); return result;
        }
        public void Dispose() => transport.Dispose();
    }
}
