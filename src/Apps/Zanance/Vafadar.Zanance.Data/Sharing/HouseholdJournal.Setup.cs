using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Exact device-local invitation review identity; no capability, signing key or invitation body is exposed.</summary>
public sealed record HouseholdInvitationNotice(Guid OperationId, string FrameDigest);

public sealed partial class HouseholdJournal
{
    /// <summary>Refreshes only an original existing-member device proof, retaining its exact private key and setup operation across actual session changes.</summary>
    internal async Task RetainOwnDevicePossessionAsync(HouseholdJournalAuthority authority, HouseholdSetupIntent intent, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.OwnerCreation || binding.MembershipVersion != 0 || HouseholdSetupAnchors.Kind(binding.SignedInvitation) != HouseholdSetupAnchors.DeviceAccess ||
            intent.OperationId != authority.SetupOperation || intent.Capability is not null) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var proof = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(intent.SignedBody, device.Public, "zanance-household-device-enrollment+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
        var anchor = JsonSerializer.Deserialize(binding.SignedInvitation!, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw Refusal();
        var authorization = HouseholdTrustChain.DeviceAuthorization(anchor, authority.IdentityId);
        if (proof.Format != 1 || proof.SpaceId != authorization.SpaceId || proof.DeviceId != Guid.Empty || proof.OperationId != authority.SetupOperation ||
            proof.IdentityId != authority.IdentityId || proof.SessionId == Guid.Empty || proof.DeviceKeys != new HouseholdPublicKeys(device.Public.SigningKey, device.Public.WrappingKey)) throw Refusal();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(intent, HouseholdMetadataJsonContext.Default.HouseholdSetupIntent);
        try
        {
            if (bytes.Length > 8192) throw Refusal();
            var row = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(item => item.Slot == SetupSlot(false), ct);
            if (row is null) context.Db.Add(new HouseholdLocalSecret { Slot = SetupSlot(false), Frame = bytes });
            else
            {
                var previous = JsonSerializer.Deserialize(row.Frame, HouseholdMetadataJsonContext.Default.HouseholdSetupIntent) ?? throw Refusal();
                if (previous.OperationId != intent.OperationId || previous.Capability is not null) throw Refusal();
                row.Frame = bytes;
            }
            await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Reviews only the exact pending original invitation so explicit local finishing does not require a retained clipboard/file.</summary>
    public async Task<HouseholdInvitationNotice?> ReadInvitationNoticeAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == "pending-owner-invitation", ct);
        if (row is null) return null;
        try
        {
            if (row.Frame.Length is < 1 or > 8192) throw Refusal();
            var original = JsonSerializer.Deserialize(row.Frame, HouseholdMetadataJsonContext.Default.HouseholdSetupIntent) ?? throw Refusal();
            if (original.OperationId == Guid.Empty || string.IsNullOrEmpty(original.SignedBody)) throw Refusal();
            context.Check(); return new(original.OperationId, Convert.ToHexStringLower(SHA256.HashData(row.Frame)));
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }
    /// <summary>Reads only the fixed original private setup intent; no arbitrary slot can expose device/root/epoch frames.</summary>
    internal async Task<HouseholdSetupIntent?> ReadSetupIntentAsync(HouseholdJournalAuthority authority, bool ownerInvitation, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var slot = SetupSlot(ownerInvitation);
        var row = await context.Db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == slot, ct);
        if (row is null) return null;
        try
        {
            if (row.Frame.Length is < 1 or > 8192) throw Refusal();
            var result = JsonSerializer.Deserialize(row.Frame, HouseholdMetadataJsonContext.Default.HouseholdSetupIntent) ?? throw Refusal();
            if (result.OperationId == Guid.Empty || string.IsNullOrEmpty(result.SignedBody)) throw Refusal();
            context.Check(); return result;
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    /// <summary>Retains exact original claim/invitation operation, signature and capability before its first HTTP mutation.</summary>
    internal async Task RetainSetupIntentAsync(HouseholdJournalAuthority authority, bool ownerInvitation, HouseholdSetupIntent intent, CancellationToken ct)
    {
        if (intent.OperationId == Guid.Empty || string.IsNullOrEmpty(intent.SignedBody)) throw Refusal();
        var frame = JsonSerializer.SerializeToUtf8Bytes(intent, HouseholdMetadataJsonContext.Default.HouseholdSetupIntent);
        try
        {
            if (frame.Length is < 1 or > 8192) throw Refusal();
            await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
            await AddImmutableSecretAsync(context.Db, SetupSlot(ownerInvitation), frame, ct); await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Forgets only a user's explicit finished setup intent; private financial/key history remains untouched.</summary>
    internal async Task CompleteSetupIntentAsync(HouseholdJournalAuthority authority, bool ownerInvitation, Guid operation, CancellationToken ct,
        string? expectedFrameDigest = null)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var slot = SetupSlot(ownerInvitation);
        var row = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(row => row.Slot == slot, ct);
        if (row is null) return;
        try
        {
            if (row.Frame.Length is < 1 or > 8192 || expectedFrameDigest is not null && expectedFrameDigest != Convert.ToHexStringLower(SHA256.HashData(row.Frame)) || JsonSerializer.Deserialize(row.Frame,
                HouseholdMetadataJsonContext.Default.HouseholdSetupIntent)?.OperationId != operation) throw Refusal();
            context.Db.Remove(row); await context.CommitAsync(ct);
        }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }

    private static string SetupSlot(bool ownerInvitation) => ownerInvitation ? "pending-owner-invitation" : "pending-recipient-claim";
}
