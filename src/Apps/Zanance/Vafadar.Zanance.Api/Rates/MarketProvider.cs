using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Server-owned adapter only; no endpoint accepts credentials, an arbitrary source URL or a user's financial data.</summary>
internal interface IMarketProvider
{
    /// <summary>Returns locally registered reviewed policy/public symbols, without network; null keeps live service unavailable.</summary>
    MarketCatalog? Catalog { get; }
    /// <summary>Fetches once for an exact registered public symbol/date; no retry or inferred asset mapping.</summary>
    Task<MarketObservation> FetchAsync(MarketRequest request, CancellationToken ct);
}

/// <summary>Closed default until actual provider, rights, coverage, costs and operational acceptance are approved.</summary>
internal sealed class UnconfiguredMarketProvider : IMarketProvider
{
    public MarketCatalog? Catalog => null;
    public Task<MarketObservation> FetchAsync(MarketRequest request, CancellationToken ct) => throw new MarketRefusal(503, "MarketProviderUnavailable");
}

/// <summary>Content-free refusal, never carrying a source response, key, financial value or private path.</summary>
internal sealed class MarketRefusal(int status, string code) : Exception(code)
{
    /// <summary>Gets the bounded HTTP classification for the existing translated client boundary.</summary>
    internal int Status { get; } = status;
}
