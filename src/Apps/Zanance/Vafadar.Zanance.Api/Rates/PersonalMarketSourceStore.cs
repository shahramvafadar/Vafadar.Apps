using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Original owner/device-scoped immutable source companion storage; no table/value/ledger mutation exists here.</summary>
internal sealed class PersonalMarketSourceStore(ServiceDbContext db, IDataProtectionProvider protection, ServiceStorage storage)
{
    internal const int MaximumDocumentBytes = 65536;
    internal const int MaximumUploadBytes = 512 * 1024;
    internal const int PageSize = 8;
    private IDataProtector Protector(string owner, Guid profile, Guid document) =>
        protection.CreateProtector("personal-market-source-v1", owner, profile.ToString("N"), document.ToString("N"));

    /// <summary>Replays only an identical original document under the same real owner lock used by device revocation.</summary>
    internal async Task<MarketSourceAcknowledgment> PutAsync(SyncRequestScope scope, SyncSpaceRow profile, MarketSourceUpload input, CancellationToken ct)
    {
        RequireAuthority(scope, profile);
        if (input.Version != 1 || input.DeviceId != scope.AuthorizedDeviceId || input.Documents is null || input.Documents.Length is < 1 or > PageSize ||
            input.Documents.Any(document => document is null) || input.Documents.Select(document => document.Id).Distinct().Count() != input.Documents.Length)
            throw Refusal(422, "MarketSourceUploadInvalid");
        // Validate the entire bounded packet before staging any row, including source/target conversion binding.
        var encoded = new List<(MarketReferenceDocument Document, string Json, string Digest, int Bytes)>();
        foreach (var document in input.Documents)
        {
            try { MarketReferenceDocuments.Validate(document, scope.Now); }
            catch (Exception error) when (error is ArgumentException or InvalidDataException or JsonException or OverflowException)
            { throw Refusal(422, "MarketSourceDocumentInvalid"); }
            var json = JsonSerializer.Serialize(document, MarketSourceJson.Default.MarketReferenceDocument);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                if (bytes.Length > MaximumDocumentBytes) throw Refusal(413, "MarketSourceDocumentBound");
                encoded.Add((document, json, Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.Length));
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        var ids = input.Documents.Select(document => document.Id).ToArray();
        var existing = await db.Set<PersonalMarketSourceRow>().Where(row => row.ProfileId == profile.Id && ids.Contains(row.DocumentId)).ToListAsync(ct);
        var newBytes = 0L;
        foreach (var item in encoded)
        {
            var before = existing.SingleOrDefault(row => row.DocumentId == item.Document.Id);
            if (before is not null)
            {
                if (before.Digest != item.Digest || Protector(scope.Owner, profile.Id, before.DocumentId).Unprotect(before.ProtectedDocument) != item.Json)
                    throw Refusal(409, "MarketSourceOriginalDiffers");
                continue;
            }
            newBytes += item.Bytes;
        }
        var ownerProfiles = db.Set<SyncSpaceRow>().Where(row => row.UserId == scope.Owner).Select(row => row.Id);
        var storedBytes = await db.Set<PersonalMarketSourceRow>().Where(row => ownerProfiles.Contains(row.ProfileId)).SumAsync(row => (long?)row.Bytes, ct) ?? 0;
        if (newBytes > storage.SyncLimits.StagedBytesPerOwner - storedBytes) throw Refusal(413, "MarketSourceStorageBound");
        foreach (var item in encoded.Where(item => existing.All(row => row.DocumentId != item.Document.Id)))
            db.Set<PersonalMarketSourceRow>().Add(new() { ProfileId = profile.Id, DocumentId = item.Document.Id, Digest = item.Digest,
                Bytes = item.Bytes, ProtectedDocument = Protector(scope.Owner, profile.Id, item.Document.Id).Protect(item.Json) });
        return new(1, ids);
    }
    /// <summary>Reads only the actual original profile and captured finite horizon; never substitutes its financial cursor.</summary>
    internal async Task<MarketSourcePage> ReadAsync(SyncRequestScope scope, SyncSpaceRow profile, long after, long? through, CancellationToken ct)
    {
        RequireAuthority(scope, profile);
        if (after < 0 || through is < 0 || through is { } supplied && supplied < after) throw Refusal(422, "MarketSourceCursorInvalid");
        var rows = db.Set<PersonalMarketSourceRow>().AsNoTracking().Where(row => row.ProfileId == profile.Id);
        var horizon = await rows.MaxAsync(row => (long?)row.Sequence, ct) ?? 0;
        if (after > horizon) throw Refusal(409, "MarketSourceHorizonDiffers");
        if (through is { } expected && expected > horizon) throw Refusal(409, "MarketSourceHorizonDiffers");
        horizon = through ?? horizon;
        var page = await rows.Where(row => row.Sequence > after && row.Sequence <= horizon).OrderBy(row => row.Sequence).Take(PageSize).ToArrayAsync(ct);
        var events = new List<MarketSourceEvent>();
        foreach (var row in page)
        {
            var json = Protector(scope.Owner, profile.Id, row.DocumentId).Unprotect(row.ProtectedDocument);
            if (Encoding.UTF8.GetByteCount(json) != row.Bytes || row.Bytes > MaximumDocumentBytes) throw Refusal(503, "MarketSourceStoredDocumentInvalid");
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != row.Digest) throw Refusal(503, "MarketSourceStoredDocumentInvalid");
                var document = JsonSerializer.Deserialize(bytes, MarketSourceJson.Default.MarketReferenceDocument) ?? throw Refusal(503, "MarketSourceStoredDocumentInvalid");
                MarketReferenceDocuments.Validate(document, scope.Now);
                if (document.Id != row.DocumentId) throw Refusal(503, "MarketSourceStoredDocumentInvalid");
                events.Add(new(row.Sequence, document));
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        var last = page.LastOrDefault()?.Sequence ?? after;
        return new(1, horizon, await rows.AnyAsync(row => row.Sequence > last && row.Sequence <= horizon, ct), [.. events]);
    }
    private static void RequireAuthority(SyncRequestScope scope, SyncSpaceRow profile)
    {
        if (scope.AuthorizedDeviceId is null || profile.UserId != scope.Owner || profile.Initializing)
            throw Refusal(409, "MarketSourceProfileUnavailable");
    }
    private static SyncProtocolRefusal Refusal(int status, string code) => new(status, code);
}
