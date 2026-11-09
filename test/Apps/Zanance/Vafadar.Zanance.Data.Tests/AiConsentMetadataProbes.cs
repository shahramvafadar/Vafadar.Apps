using System.Text.Json;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;
namespace Vafadar.Zanance.Data.Tests;

/// <summary>Original private consent must refuse malformed metadata before credential, journal-write or source effects.</summary>
internal static class AiConsentMetadataProbes
{
    /// <summary>Observes one new malformed metadata condition at its original read/continue/close/submit entry.</summary>
    internal static async Task<ConsentMetadataObservation> ObserveAsync(int variant, int operation, CancellationToken ct)
    {
        if (variant is < 0 or > 12 || operation is < 0 or > 4) throw new ArgumentOutOfRangeException();
        var payload = new AiPayload(1, AiPurpose.PeriodExplanation, "en",
            new(new(2026, 11, 1), new(2026, 11, 2), 1, true, [new PeriodTotals("EUR", 0, 0, 0, 0)], 0), null);
        var offer = new AiOffer(Guid.NewGuid(), "fictitious-provider", "fictitious-model", payload.Purpose, 8, DateTimeOffset.UnixEpoch.AddYears(100));
        offer = variant switch
        {
            0 => offer with { Id = Guid.Empty },
            1 => offer with { Provider = null! },
            2 => offer with { Provider = "   " },
            3 => offer with { Provider = new string('p', 65) },
            4 => offer with { Model = null! },
            5 => offer with { Model = "   " },
            6 => offer with { Model = new string('m', 65) },
            7 => offer with { MaximumUnits = 0 },
            8 => offer with { MaximumUnits = 1_000_000_000_001 },
            9 => offer with { Purpose = AiPurpose.PeriodComparison },
            10 => offer with { Purpose = (AiPurpose)42 },
            // Expiry is a service decision for new work; historical literal state must remain readable.
            11 => offer with { ExpiresAt = DateTimeOffset.UnixEpoch },
            12 => offer with { Provider = new string('p', 64), Model = new string('m', 64), MaximumUnits = 1_000_000_000_000 },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
        var retained = new AiClientRecord(1, new(Guid.NewGuid(), offer, payload), null);
        var storage = new Journal(operation == 3 ? null : JsonSerializer.Serialize(retained, AiClientJson.Default.AiClientRecord));
        var credentials = new NoCredentials();
        using var transport = new PersonalSyncTransport(new LocalSyncPeer("https://localhost:55555", new string('f', 64)), credentials);
        var file = Path.GetFullPath(Path.Combine("artifacts", "consent-metadata-label-only", Guid.NewGuid().ToString("N") + ".sqlite"));
        var client = new AiClient(transport, storage, "fictitious-consent-owner", file, () => { });
        bool refused = false, accepted = false; string? error = null; int rechecks = 0;
        try
        {
            switch (operation)
            {
                case 0: accepted = JsonSerializer.Serialize(await client.ReadAsync(ct), AiClientJson.Default.AiClientRecord) == JsonSerializer.Serialize(retained, AiClientJson.Default.AiClientRecord); break;
                case 1: await client.ContinueAsync(ct); break;
                case 2: await client.CloseAsync(ct); break;
                case 4: await client.OfferAsync(offer.Purpose, ct); break;
                case 3: await client.SubmitAsync(offer, payload, () => { rechecks++; return Task.CompletedTask; }, ct); break;
            }
        }
        catch (InvalidDataException) { refused = true; }
        catch (Exception e) when (e is not OutOfMemoryException and not OperationCanceledException) { error = e.GetType().Name; }
        return new(variant, operation, refused, accepted, storage.Reads, storage.Writes, credentials.Calls, rechecks, error);
    }

    private sealed class Journal(string? raw) : IAiPrivateStorage
    {
        internal int Reads, Writes;
        public Task<string?> ReadAsync(string scope, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Reads++; return Task.FromResult(raw); }
        public Task WriteAsync(string scope, string value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Writes++; throw new PrivateWriteStop(); }
    }
    private sealed class PrivateWriteStop : IOException;
    private sealed class NoCredentials : IPersonalSyncTokenStore
    {
        internal int Calls;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) { Calls++; throw new CredentialStop(); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
    }
    private sealed class CredentialStop : IOException;
}

/// <summary>Bounded counters only; no durable value, credential, HTTP request or financial source is created.</summary>
internal sealed record ConsentMetadataObservation(int Variant, int Operation, bool Refused, bool Accepted, int Reads, int Writes, int Credentials, int Rechecks, string? OtherError);
