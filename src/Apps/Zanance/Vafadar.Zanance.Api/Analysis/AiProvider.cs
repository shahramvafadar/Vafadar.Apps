using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>A separately qualified destination/model and maximum units; quotation is local metadata, never a financial upload.</summary>
internal sealed record AiProviderOffer(string Provider, string Model, long MaximumUnits);
/// <summary>Only a definitive provider outcome may spend/refund an original reservation.</summary>
internal enum AiProviderState { Unknown, Completed, DefinitiveNoCharge }
/// <summary>Provider-confirmed outcome, never guessed from HTTP timeout or a missing request lookup.</summary>
internal sealed record AiProviderOutcome(AiProviderState State, long ChargedUnits = 0, string? Text = null, AiPilotUsage? Metering = null,
    AiPilotVendorStatus? VendorStatus = null, AiPilotCreateRefusal? CreateRefusal = null);
/// <summary>Terminal status reported by the original synthetic vendor operation, independent of customer credit.</summary>
public enum AiPilotVendorStatus { Completed = 1, Failed = 2, Incomplete = 3, Cancelled = 4 }
/// <summary>Only the observed HTTP response class for the one original synthetic create.</summary>
public enum AiPilotCreateRefusal { RequestRejected = 400, AuthenticationRejected = 401, AccessDenied = 403, ResourceUnavailable = 404, RateLimited = 429 }
/// <summary>Authenticated terminal usage and an uncached Standard reference estimate, independent of customer credit.</summary>
internal sealed record AiPilotUsage(int Input, int Output, int? CachedInput, int? Thinking, long EstimateMicros)
{
    /// <summary>Reference tariff used for the persisted estimate.</summary>
    internal const string Tariff = "standard-uncached-text-2026-11-19";
}
/// <summary>Replaceable owned adapter; Begin is called once per original id, and status must never submit again.</summary>
internal interface IAiProvider
{
    /// <summary>Local offer only; null means no separately approved provider/pricing, with no network effect.</summary>
    AiProviderOffer? Offer(AiPurpose purpose);
    /// <summary>Executes an explicitly consented immutable request; external adapters must prove idempotence and status recovery.</summary>
    Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct);
    /// <summary>Queries the same original id only. Not found alone is Unknown, never a refund or permission to resend.</summary>
    Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct);
}
/// <summary>No production model, price, SDK, network or fake analysis is silently selected.</summary>
internal sealed class UnconfiguredAiProvider : IAiProvider
{
    /// <inheritdoc />
    public AiProviderOffer? Offer(AiPurpose purpose) => null;
    /// <inheritdoc />
    public Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct)
        => throw new AiRefusal(503, "AiProviderNotConfigured");
    /// <inheritdoc />
    public Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct)
        => Task.FromResult(new AiProviderOutcome(AiProviderState.Unknown));
}
/// <summary>Content-free translated classification; no prompt, response, identity or credential appears in diagnostics.</summary>
internal sealed class AiRefusal(int status, string code) : Exception(code)
{
    /// <summary>HTTP classification only.</summary>
    internal int Status { get; } = status;
}
