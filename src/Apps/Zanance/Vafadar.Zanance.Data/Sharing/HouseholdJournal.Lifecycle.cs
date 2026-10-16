using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    private const string DeletionSlot = "original-room-deletion";

    /// <summary>Reads only this protected profile's original finite public declaration and authenticated outcome, preserving downloaded financial data and keys.</summary>
    public async Task<HouseholdDeletionIntent?> ReadDeletionAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var result = await ReadDeletionCoreAsync(context.Db, binding, ct); context.Check(); return result;
    }

    /// <summary>Retains literal consent before HTTP; ordinary financial writers stop in that same protected transaction, even after a process restart.</summary>
    internal async Task RetainDeletionAsync(HouseholdJournalAuthority authority, HouseholdDeletionIntent intent, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        _ = await VerifyDeletionAsync(context.Db, binding, intent, ct);
        var previous = await ReadDeletionCoreAsync(context.Db, binding, ct);
        if (previous is null && (intent.Receipt is not null || intent.Rejected) || previous is not null &&
            (previous.Request != intent.Request || previous.AnchorVersion != intent.AnchorVersion || previous.Rejected && !intent.Rejected ||
             previous.Receipt is { } receipt && (intent.Receipt is null || receipt with { StorageRetired = intent.Receipt.StorageRetired } != intent.Receipt))) throw Refusal();
        var frame = JsonSerializer.SerializeToUtf8Bytes(intent, HouseholdMetadataJsonContext.Default.HouseholdDeletionIntent);
        try
        {
            if (frame.Length > 8192) throw Refusal();
            var row = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(row => row.Slot == DeletionSlot, ct);
            if (row is null) context.Db.Add(new HouseholdLocalSecret { Slot = DeletionSlot, Frame = frame });
            else { CryptographicOperations.ZeroMemory(row.Frame); row.Frame = frame; }
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Explicitly releases only the exact service-reviewed unaccepted expired declaration; accepted permanent deletion can never be cleared here.</summary>
    internal async Task FinishRejectedDeletionAsync(HouseholdJournalAuthority authority, Guid operation, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var intent = await ReadDeletionCoreAsync(context.Db, binding, ct) ?? throw Refusal();
        var declaration = await VerifyDeletionAsync(context.Db, binding, intent, ct);
        if (!intent.Rejected || intent.Receipt is not null || declaration.OperationId != operation) throw Refusal();
        await context.Db.Set<HouseholdLocalSecret>().Where(row => row.Slot == DeletionSlot).ExecuteDeleteAsync(ct); await context.CommitAsync(ct);
    }

    /// <summary>Only one fixed existing protected metadata slot; neither a new schema nor portable financial data.</summary>
    private static async Task<HouseholdDeletionIntent?> ReadDeletionCoreAsync(ZananceDbContext db, HouseholdLocalBinding binding, CancellationToken ct)
    {
        var row = await db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == DeletionSlot, ct); if (row is null) return null;
        try
        {
            var intent = StrictJsonPayload.Deserialize(row.Frame, HouseholdMetadataJsonContext.Default.HouseholdDeletionIntent, 8192);
            _ = await VerifyDeletionAsync(db, binding, intent, ct); return intent;
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    /// <summary>Uses the original independently verified historical owner root, exact captured scope/device and literal request hash.</summary>
    private static async Task<HouseholdDeleteStatement> VerifyDeletionAsync(ZananceDbContext db, HouseholdLocalBinding binding, HouseholdDeletionIntent intent, CancellationToken ct)
    {
        if (intent.Request is null || intent.Request.DeviceId != binding.DeviceId || intent.AnchorVersion < 1 || intent.Receipt is not null && intent.Rejected) throw Refusal();
        using var device = await ReadIdentityAsync(db, "device", ct); var history = await ReadMembershipAsync(db, binding, device, ct);
        if (!history.ByVersion.TryGetValue(intent.AnchorVersion, out var anchor)) throw Refusal();
        var value = StandardJoseSignatures.VerifyJson(intent.Request.SignedDeclaration, anchor.Root, HouseholdLifecycleProtocol.DeleteType,
            HouseholdLifecycleProtocol.DeclarationBytes, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
        if (value.Format != 1 || value.OperationId == Guid.Empty || value.SpaceId != binding.SpaceId || value.DeviceId != binding.DeviceId || value.OwnerId != binding.IdentityId ||
            value.MembershipVersion < anchor.Version || value.MembershipVersion == anchor.Version && value.MembershipHead != anchor.Hash ||
            !HouseholdProtectionContext.IsFingerprint(value.MembershipHead) || anchor.Snapshot().OwnerId != binding.IdentityId ||
            value.PublicationCursor < 0 || !HouseholdProtectionContext.IsFingerprint(value.PublicationHead) || value.ValidUntilUnixSeconds <= 0) throw Refusal();
        if (intent.Receipt is { } receipt && (receipt.SpaceId != value.SpaceId || receipt.OperationId != value.OperationId || receipt.Reason != HouseholdDeletionReason.Owner ||
            receipt.RequestHash != HouseholdMetadataProofHash(intent.Request.SignedDeclaration) || receipt.DeclaredAtUnixSeconds <= 0 ||
            receipt.DeclaredAtUnixSeconds >= value.ValidUntilUnixSeconds)) throw Refusal();
        return value;
    }

    /// <summary>Matches the service's literal compact signature commitment without decoding or re-signing it.</summary>
    internal static string HouseholdMetadataProofHash(string body) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(body)));
}
