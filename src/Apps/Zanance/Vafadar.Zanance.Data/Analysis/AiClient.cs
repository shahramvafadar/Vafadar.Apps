using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Analysis;

/// <summary>Platform-private original request journal outside financial/portable storage; loss must never imply safe resend.</summary>
public interface IAiPrivateStorage
{
    /// <summary>Reads only the exact identity/peer/file slot, failing closed when its loss marker survives.</summary>
    Task<string?> ReadAsync(string scope, CancellationToken ct);
    /// <summary>Durably publishes and verifies the original bounded state before any financial HTTP submission.</summary>
    Task WriteAsync(string scope, string value, CancellationToken ct);
}
/// <summary>Original private immutable consent and last confirmed outcome; no bearer credential or new grant.</summary>
public sealed record AiClientRecord(int Version, AiSubmission Submission, AiRequestResult? Result);

/// <summary>One identity/peer/original-file client; timeouts retain its literal request and permit status only.</summary>
public sealed class AiClient(PersonalSyncTransport transport, IAiPrivateStorage storage, string expectedUser, string originalFile, Action verifyAccess)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Operations = new(StringComparer.Ordinal);
    private string Scope => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.analysis.client.v1\n" +
        transport.Peer.BindingKey(expectedUser) + "\n" + Path.GetFullPath(originalFile))));
    /// <summary>Reads actual local provider availability/balance without any financial request or automatic registration.</summary>
    public async Task<AiAvailability> AvailabilityAsync(CancellationToken ct = default)
    {
        verifyAccess(); var result = await transport.AnalysisAvailabilityAsync(expectedUser, ct); verifyAccess(); Balance(result.Balance); return result;
    }
    /// <summary>Obtains metadata-only exact terms for the user's selected fixed purpose.</summary>
    public async Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct = default)
    {
        verifyAccess(); var result = await transport.AnalysisOfferAsync(expectedUser, purpose, ct); verifyAccess();
        if (result.Id == Guid.Empty || result.Purpose != purpose || string.IsNullOrWhiteSpace(result.Provider) || result.Provider.Length > 64 ||
            string.IsNullOrWhiteSpace(result.Model) || result.Model.Length > 64 || result.MaximumUnits is <= 0 or > 1_000_000_000_000)
            throw new InvalidDataException("Original analysis offer refused.");
        return result;
    }
    /// <summary>Reads the original retained intent/result without HTTP, key generation or permission renewal.</summary>
    public async Task<AiClientRecord?> ReadAsync(CancellationToken ct = default)
    {
        verifyAccess(); var raw = await storage.ReadAsync(Scope, ct); verifyAccess();
        if (raw is null) return null;
        if (Encoding.UTF8.GetByteCount(raw) > 65536) throw new InvalidDataException("Original analysis state exceeds its bound.");
        var state = JsonSerializer.Deserialize(raw, AiClientJson.Default.AiClientRecord) ?? throw new InvalidDataException("Original analysis state unavailable.");
        if (state.Version != 1 || state.Submission is null || state.Submission.RequestId == Guid.Empty || state.Submission.Offer is null)
            throw new InvalidDataException("Original analysis state differs.");
        AiProtocol.Validate(state.Submission.Payload);
        if (state.Result is not null) Validate(state.Submission, state.Result);
        return state;
    }
    /// <summary>Publishes original native consent before one financial HTTP request; a prior pending record blocks a new id.</summary>
    public async Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheckSource, CancellationToken ct = default)
    {
        verifyAccess(); var semaphore = Operations.GetOrAdd(Scope, _ => new SemaphoreSlim(1, 1)); await semaphore.WaitAsync(ct);
        try
        {
            var prior = await ReadAsync(ct);
            if (prior is { Result: null } || prior?.Result?.State == AiRequestState.Pending)
                throw new InvalidOperationException("Continue the original analysis status before another request.");
            // Freeze exactly the original disclosed aggregates; no live database getter is retained over HTTP.
            var encoded = JsonSerializer.SerializeToUtf8Bytes(payload, AiJson.Default.AiPayload);
            if (encoded.Length > AiProtocol.MaximumPayloadBytes) throw new InvalidDataException("Analysis payload bound exceeded.");
            var frozen = JsonSerializer.Deserialize(encoded, AiJson.Default.AiPayload)!; AiProtocol.Validate(frozen);
            var submission = new AiSubmission(Guid.NewGuid(), offer, frozen);
            verifyAccess();
            await SaveAsync(new(1, submission, null), ct); // A process cut after this point never means safe to create a new id.
            await recheckSource(); verifyAccess();
            var result = await transport.AnalysisSubmitAsync(expectedUser, submission, ct); verifyAccess(); Validate(submission, result);
            await SaveAsync(new(1, submission, result), ct); return result;
        }
        finally { semaphore.Release(); }
    }
    /// <summary>Explicit continuation: status first; only an absent service id replays the same immutable submission, never a new id.</summary>
    public async Task<AiRequestResult> ContinueAsync(CancellationToken ct = default)
    {
        verifyAccess(); var semaphore = Operations.GetOrAdd(Scope, _ => new SemaphoreSlim(1, 1)); await semaphore.WaitAsync(ct);
        try
        {
            var state = await ReadAsync(ct) ?? throw new InvalidOperationException("Original analysis request required.");
            if (state.Result is { State: not AiRequestState.Pending } final) return final;
            AiRequestResult result;
            try { result = await transport.AnalysisStatusAsync(expectedUser, state.Submission.RequestId, ct); }
            catch (PersonalSyncHttpException missing) when (missing.Status == HttpStatusCode.NotFound)
            {
                // This explicitly confirmed continuation may publish the same original packet if it never arrived.
                // The service serializes the original id; it never invokes the provider twice for an existing request.
                verifyAccess(); result = await transport.AnalysisSubmitAsync(expectedUser, state.Submission, ct);
            }
            verifyAccess(); Validate(state.Submission, result);
            await SaveAsync(state with { Result = result }, ct); return result;
        }
        finally { semaphore.Release(); }
    }
    /// <summary>Explicitly closes the exact retained original id before allowing a new consent; there is no financial-payload upload.</summary>
    public async Task<AiRequestResult> CloseAsync(CancellationToken ct = default)
    {
        verifyAccess(); var semaphore = Operations.GetOrAdd(Scope, _ => new SemaphoreSlim(1, 1)); await semaphore.WaitAsync(ct);
        try
        {
            var state = await ReadAsync(ct) ?? throw new InvalidOperationException("Original analysis request required.");
            if (state.Result is { State: not AiRequestState.Pending } final) return final;
            var result = await transport.AnalysisCloseAsync(expectedUser, state.Submission.RequestId,
                new(state.Submission.Offer, AiProtocol.Digest(state.Submission.Payload)), ct);
            verifyAccess(); Validate(state.Submission, result); await SaveAsync(state with { Result = result }, ct); return result;
        }
        finally { semaphore.Release(); }
    }
    private Task SaveAsync(AiClientRecord record, CancellationToken ct)
    {
        verifyAccess(); var raw = JsonSerializer.Serialize(record, AiClientJson.Default.AiClientRecord);
        if (Encoding.UTF8.GetByteCount(raw) > 65536) throw new InvalidDataException("Analysis private journal bound exceeded.");
        return storage.WriteAsync(Scope, raw, ct);
    }
    private static void Validate(AiSubmission original, AiRequestResult result)
    {
        if (result.RequestId != original.RequestId || result.PayloadDigest != AiProtocol.Digest(original.Payload) || !Enum.IsDefined(result.State) ||
            result.ChargedUnits < 0 || result.ChargedUnits > original.Offer.MaximumUnits ||
            result.State != AiRequestState.Completed && (result.Text is not null || result.ChargedUnits != 0))
            throw new InvalidDataException("Original analysis outcome differs.");
        if (result.State == AiRequestState.Completed) AiProtocol.ValidateText(result.Text ?? "");
        Balance(result.Balance);
    }
    private static void Balance(AiBalance balance)
    {
        if (balance is null || balance.PurchasedAvailable < 0 || balance.GiftedAvailable < 0 || balance.PurchasedReserved < 0 || balance.GiftedReserved < 0 ||
            balance.PurchasedAvailable > 1_000_000_000_000 || balance.GiftedAvailable > 1_000_000_000_000 ||
            balance.PurchasedReserved > 1_000_000_000_000 || balance.GiftedReserved > 1_000_000_000_000)
            throw new InvalidDataException("Original analysis balance unavailable.");
    }
}

/// <summary>Explicit bounded private-state AOT metadata; no private journal is a portable preference or entitlement.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(AiClientRecord))]
public sealed partial class AiClientJson : JsonSerializerContext;
