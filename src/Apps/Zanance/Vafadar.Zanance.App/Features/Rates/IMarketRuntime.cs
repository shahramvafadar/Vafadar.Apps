using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Original unlocked foreground/file scope for public-reference work; never a new financial write permission.</summary>
public sealed record MarketUiAccess(string File, Action Validate, CancellationToken Cancellation);
/// <summary>One actual original identity connection; fetching never changes a rate, holding or ledger row.</summary>
public interface IMarketRemote : IDisposable
{
    /// <summary>Original approved service destination, without a secret or arbitrary user-supplied URL.</summary>
    string Destination { get; }
    /// <summary>Explicit metadata-only source availability request.</summary>
    Task<MarketAvailability> AvailabilityAsync(CancellationToken ct);
    /// <summary>One explicitly selected source/symbol/date lookup, retaining exact original policy.</summary>
    Task<MarketQuote> FetchAsync(MarketCatalog catalog, MarketRequest request, CancellationToken ct);
}
/// <summary>Native private cache and current identity/lock lifetime; the UI chooses all network and financial actions explicitly.</summary>
public interface IMarketRuntime
{
    /// <summary>Whether this build selected networking; it does not imply a configured or licensed provider.</summary>
    bool NetworkAvailable { get; }
    /// <summary>Explicitly opens existing account UI; never signs in or creates an identity automatically.</summary>
    Task OpenAccountAsync();
    /// <summary>Captures the exact current unlocked original file without opening its database.</summary>
    MarketUiAccess Capture();
    /// <summary>Reads only retained private cached references, without HTTP or sign-in.</summary>
    Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct);
    /// <summary>Opens only the already existing authenticated identity; never registers or signs in automatically.</summary>
    Task<IMarketRemote> OpenAsync(MarketUiAccess access, CancellationToken ct);
    /// <summary>Actual native confirmation retaining the complete original source/value mapping text.</summary>
    Task<bool> ConfirmAsync(string title, string originalTerms, string accept, string cancel);
}
