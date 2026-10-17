using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Exact original native foreground/lock/profile access; it never selects data or supplies credit authority.</summary>
public sealed record AiUiAccess(Action Validate, CancellationToken Cancellation);
/// <summary>Original pinned identity/file context, kept separate from financial data and native controls.</summary>
public interface IAiRemote : IDisposable
{
    /// <summary>Actual reviewed HTTPS service destination; provider/model appear in its original offer.</summary>
    string Destination { get; }
    /// <summary>Reads provider metadata and real wallet balance without a financial upload.</summary>
    Task<AiAvailability> AvailabilityAsync(CancellationToken ct);
    /// <summary>Reads only the current original private intent/outcome.</summary>
    Task<AiClientRecord?> ReadAsync(CancellationToken ct);
    /// <summary>Requests metadata-only fixed-purpose terms.</summary>
    Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct);
    /// <summary>Retains literal native consent before the original immutable aggregate submission.</summary>
    Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct);
    /// <summary>Explicit original continuation; status first, identical submission only when the service has no original id.</summary>
    Task<AiRequestResult> ContinueAsync(CancellationToken ct);
    /// <summary>Closes an unaccepted original id durably; dispatched work is not assumed canceled or refunded.</summary>
    Task<AiRequestResult> CloseAsync(CancellationToken ct);
}
/// <summary>Native authorization, deliberate manual share and original pinned identity connection only.</summary>
public interface IAiRuntime
{
    /// <summary>Whether this explicitly network-enabled edition supports the provider boundary, not whether a provider exists.</summary>
    bool NetworkAvailable { get; }
    /// <summary>Captures the exact original local file and unlocked foreground lifetime.</summary>
    AiUiAccess Capture(string originalFile);
    /// <summary>Uses existing device/app-lock confirmation for disclosure; no new key or account.</summary>
    Task<bool> ConfirmDisclosureAsync();
    /// <summary>Offers the exact text to the platform chooser; no built-in model runs and no AI credit is consumed.</summary>
    Task ShareAsync(string exactText);
    /// <summary>Opens the actual existing pinned identity session without sign-in, registration or credential display.</summary>
    Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct);
}
