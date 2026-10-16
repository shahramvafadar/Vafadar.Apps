using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Bounded room-serialized operational metadata; it never reads decrypted financial content or private recipient grants.</summary>
internal sealed class HouseholdAuditStore(ServiceDbContext db)
{
    /// <summary>Appends only inside the original authenticated room transaction; identical business retries return before this point.</summary>
    internal async Task AppendAsync(HouseholdRequestScope scope, HouseholdAuditAction action, Guid device, Guid operation,
        HouseholdAuditEvidence? evidence, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null || !Enum.IsDefined(action) || device == Guid.Empty || operation == Guid.Empty ||
            scope.Space.AuditCursor is < 0 or long.MaxValue || scope.Space.AuditBytes < 0)
            throw new HouseholdProtocolRefusal(409, "HouseholdAuditUnavailable");
        var json = JsonSerializer.Serialize(evidence ?? new(), HouseholdMetadataJsonContext.Default.HouseholdAuditEvidence);
        var bytes = Encoding.UTF8.GetByteCount(json) + 512;
        var maximum = action == HouseholdAuditAction.EncryptedUpdate ? 256L * 1024 * 1024 : 260L * 1024 * 1024;
        if (bytes > 512 * 1024 || scope.Space.AuditBytes > maximum - bytes) throw new HouseholdProtocolRefusal(413, "HouseholdAuditStorageBound");
        db.Add(new HouseholdAuditRow { SpaceId = scope.Space.Id, Sequence = ++scope.Space.AuditCursor, Action = action, ActorId = scope.ActorId,
            DeviceId = device, OperationId = operation, CreatedAt = scope.Now, MembershipVersion = scope.Space.MembershipVersion, MembershipHead = scope.Space.MembershipHead,
            PublicationCursor = scope.Space.Cursor, PublicationHead = scope.Space.PublicationHead, EvidenceJson = json, Bytes = bytes });
        scope.Space.AuditBytes += bytes; await Task.CompletedTask; ct.ThrowIfCancellationRequested();
    }

    /// <summary>Returns only the requested finite original prefix to a current accepted approved member; later appends cannot extend the horizon.</summary>
    internal async Task<HouseholdAuditPage> ReadAsync(HouseholdRequestScope scope, Guid device, long after, long? through, CancellationToken ct, int maximum = 16)
    {
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.ReadAudit, ct);
        var horizon = through ?? scope.Space.AuditCursor;
        if (after < 0 || horizon < after || horizon > scope.Space.AuditCursor || maximum is < 1 or > 16) throw new HouseholdProtocolRefusal(400, "InvalidHouseholdAuditPosition");
        var rows = await db.Set<HouseholdAuditRow>().AsNoTracking().Where(row => row.SpaceId == scope.Space.Id && row.Sequence > after && row.Sequence <= horizon)
            .OrderBy(row => row.Sequence).Take(maximum).ToArrayAsync(ct);
        var result = new List<HouseholdAuditItem>(); var next = after; var bytes = 0;
        foreach (var row in rows)
        {
            if (row.Sequence != next + 1 || row.Bytes != Encoding.UTF8.GetByteCount(row.EvidenceJson) + 512) throw new HouseholdProtocolRefusal(409, "HouseholdAuditPrefixMismatch");
            if (result.Count != 0 && bytes + row.Bytes > 1024 * 1024) break;
            result.Add(new(row.Sequence, row.Action, row.ActorId, row.DeviceId, row.OperationId, row.CreatedAt.ToUnixTimeSeconds(), row.MembershipVersion,
                row.MembershipHead, row.PublicationCursor, row.PublicationHead, JsonSerializer.Deserialize(row.EvidenceJson, HouseholdMetadataJsonContext.Default.HouseholdAuditEvidence)
                    ?? throw new HouseholdProtocolRefusal(409, "HouseholdAuditEvidenceUnavailable")));
            bytes += row.Bytes; next = row.Sequence;
        }
        if (result.Count == 0 && next != horizon) throw new HouseholdProtocolRefusal(409, "HouseholdAuditPrefixMissing");
        return new(scope.Space.Id, horizon, result.ToArray(), next, next == horizon);
    }
}
