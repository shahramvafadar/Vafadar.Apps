using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Validates only incoming financial adoption at the exact original target's offered frozen horizon; it grants no seal or outgoing publication.</summary>
    internal async Task RequireOwnershipReceptionAsync(HouseholdJournalAuthority authority, Guid operation, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        await RequireOwnershipReceptionAsync(context.Db, operation, ct); context.Check();
    }

    /// <summary>Rechecks the fixed original target/operation/root/checkpoint and current clock inside the actual receiving writer transaction.</summary>
    internal static async Task RequireOwnershipReceptionAsync(ZananceDbContext db, Guid operation, CancellationToken ct, long? financialThrough = null)
    {
        if (operation == Guid.Empty || db.Database.CurrentTransaction is null) throw Refusal();
        if (await db.Set<HouseholdLocalSecret>().AnyAsync(row => row.Slot == DeletionSlot, ct)) throw Refusal();
        var binding = await db.Set<HouseholdLocalBinding>().AsNoTracking().SingleOrDefaultAsync(ct) ?? throw Refusal();
        var row = await db.Set<HouseholdLocalOwnership>().AsNoTracking().SingleOrDefaultAsync(ct) ?? throw Refusal();
        if (row.OperationId != operation || row.Stage is < HouseholdOwnershipStage.Offered or > HouseholdOwnershipStage.Accepted ||
            row.AnchorVersion != binding.MembershipVersion || !binding.RotationRequired || Digest(row.BodyJson) != row.Digest ||
            binding.GenerationPath != Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db))) throw Refusal();
        var retained = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.BodyJson), HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation,
            HouseholdOwnershipProtocol.MaximumBytes);
        if (retained.OperationId != row.OperationId || retained.Stage != row.Stage || retained.AnchorVersion != row.AnchorVersion || retained.SignedOffer is null) throw Refusal();
        using var device = await ReadIdentityAsync(db, "device", ct); var membership = await ReadMembershipAsync(db, binding, device, ct);
        var current = membership.Current;
        var offer = StandardJoseSignatures.VerifyJson(retained.SignedOffer, current.Root, HouseholdOwnershipProtocol.OfferType, 8192,
            HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
        if (offer.Format != 1 || offer.SpaceId != binding.SpaceId || offer.OperationId != operation || offer.OwnerId != current.Snapshot().OwnerId ||
            offer.TargetId != binding.IdentityId || offer.TargetDeviceId != binding.DeviceId || offer.ExpectedVersion != current.Version || offer.ExpectedHead != current.Hash ||
            offer.PublicationCursor != binding.Cursor || offer.PublicationHead != binding.PublicationHead || offer.ExpiresUnixSeconds <= db.WorkClock.GetUtcNow().ToUnixTimeSeconds() ||
            financialThrough is { } through && through != binding.FinancialCursor || !AllowsLocalReception(binding, current, db.WorkClock, ownershipRotation: true) ||
            current.Device(binding.DeviceId, requireWrite: false) != device.Public) throw Refusal();
    }
}
