using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>One profile-local original ownership operation; public proofs/opaque grants only, with private candidates retained in existing fixed secret slots.</summary>
internal sealed class HouseholdLocalOwnership
{
    /// <summary>Singleton explicit pending operation in this exact protected generation.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Immutable original source operation.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Original historical local root checkpoint.</summary>
    public long AnchorVersion { get; set; }
    /// <summary>Only staged public metadata and standard opaque recipient envelopes.</summary>
    public string BodyJson { get; set; } = "";
    /// <summary>Exact current body digest, checked before allowing a stage transition.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Current locally retained handshake stage; no stage alone changes membership authority.</summary>
    public HouseholdOwnershipStage Stage { get; set; }
}

/// <summary>Finite protected local protocol metadata, excluded from portable financial copies.</summary>
internal sealed class HouseholdLocalOwnershipModel : IEntityTypeConfiguration<HouseholdLocalOwnership>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdLocalOwnership> model)
    {
        model.ToTable("HouseholdLocalOwnership", table =>
        {
            table.HasCheckConstraint("CK_HouseholdLocalOwnership_Singleton", "\"Id\"=1");
            table.HasCheckConstraint("CK_HouseholdLocalOwnership_Bound", "length(CAST(\"BodyJson\" AS BLOB)) BETWEEN 1 AND 2097152");
        });
        model.HasKey(row => row.Id); model.Property(row => row.Id).ValueGeneratedNever();
        model.Property(row => row.BodyJson).HasMaxLength(HouseholdOwnershipProtocol.MaximumBytes); model.Property(row => row.Digest).HasMaxLength(64);
    }
}

public sealed partial class HouseholdJournal
{
    /// <summary>Returns an exact already verified historical checkpoint; a server-supplied version index never selects an untrusted root.</summary>
    public async Task<HouseholdVerifiedMembership> ReadMembershipVersionAsync(HouseholdJournalAuthority authority, long version, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (!membership.ByVersion.TryGetValue(version, out var result)) throw Refusal(); context.Check(); return result;
    }

    /// <summary>Reads only the fixed original public pending handoff, never private root/epoch frames.</summary>
    public async Task<HouseholdOwnershipObservation?> ReadOwnershipAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalOwnership>().AsNoTracking().SingleOrDefaultAsync(ct); if (row is null) return null;
        if (Encoding.UTF8.GetByteCount(row.BodyJson) > HouseholdOwnershipProtocol.MaximumBytes || Digest(row.BodyJson) != row.Digest) throw Refusal();
        var result = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(row.BodyJson), HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation, HouseholdOwnershipProtocol.MaximumBytes);
        if (result.OperationId != row.OperationId || result.AnchorVersion != row.AnchorVersion || result.Stage != row.Stage) throw Refusal();
        context.Check(); return result;
    }

    /// <summary>Retains an exact stage and, for the actual target only, its independently generated candidates atomically before HTTP.</summary>
    internal async Task RetainOwnershipAsync(HouseholdJournalAuthority authority, HouseholdOwnershipObservation next, HouseholdOwnershipObservation? expected,
        SigningRecipientIdentity? proposedRoot, HouseholdEpochMaterial? proposedEpoch, CancellationToken ct)
    {
        if (next.OperationId is not { } operation || operation == Guid.Empty || next.AnchorVersion < 1 || next.SignedOffer is null || next.Stage is < HouseholdOwnershipStage.Offered or > HouseholdOwnershipStage.Expired) throw Refusal();
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var history = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (!history.ByVersion.TryGetValue(next.AnchorVersion, out var anchor)) throw Refusal();
        var offer = StandardJoseSignatures.VerifyJson(next.SignedOffer, anchor.Root, HouseholdOwnershipProtocol.OfferType, 8192, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
        if (offer.SpaceId != binding.SpaceId || offer.OperationId != operation || offer.ExpectedVersion != anchor.Version || offer.ExpectedHead != anchor.Hash ||
            offer.OwnerId != anchor.Snapshot().OwnerId || offer.OwnerId != binding.IdentityId && offer.TargetId != binding.IdentityId) throw Refusal();
        var row = await context.Db.Set<HouseholdLocalOwnership>().SingleOrDefaultAsync(ct);
        if (row is null && expected is not null || row is not null && (expected is null || row.OperationId != operation || row.AnchorVersion != next.AnchorVersion ||
            next.SignedOffer != expected.SignedOffer ||
            row.Digest != Digest(JsonSerializer.Serialize(expected, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation)) ||
            row.BodyJson != JsonSerializer.Serialize(expected, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation))) throw Refusal();
        var json = JsonSerializer.Serialize(next, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
        if (Encoding.UTF8.GetByteCount(json) > HouseholdOwnershipProtocol.MaximumBytes) throw Refusal();
        var privateFrames = new List<byte[]>();
        try
        {
        if (proposedRoot is not null || proposedEpoch is not null)
        {
            if (proposedRoot is null || proposedEpoch is null || offer.TargetId != binding.IdentityId || offer.TargetDeviceId != binding.DeviceId || next.Preparation is null ||
                next.Preparation.Statement.OwnerKeys != new HouseholdPublicKeys(proposedRoot.Public.SigningKey, proposedRoot.Public.WrappingKey) ||
                proposedEpoch.Epoch != next.Preparation.Statement.KeyEpoch || proposedEpoch.KeyCommitment != next.Preparation.Statement.KeyCommitment ||
                proposedEpoch.AddressKeyCommitment != next.Preparation.Statement.AddressKeyCommitment) throw Refusal();
            var rootBytes = proposedRoot.ExportForProtectedStorage(); var epochBytes = proposedEpoch.ExportForProtectedStorage();
            privateFrames.Add(rootBytes); privateFrames.Add(epochBytes);
            await StoreRootAsync(context.Db, proposedRoot.Public, rootBytes, ct);
            await AddImmutableSecretAsync(context.Db, PreparedEpochSlot(operation), epochBytes, ct);
        }
        row ??= new(); if (context.Db.Entry(row).State == EntityState.Detached) context.Db.Add(row);
        row.OperationId = operation; row.AnchorVersion = next.AnchorVersion; row.BodyJson = json; row.Digest = Digest(json); row.Stage = next.Stage;
        await context.CommitAsync(ct);
        }
        finally { foreach (var frame in privateFrames) CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Forgets only an explicitly finished exact terminal operation; original committed root/epochs and financial history remain untouched.</summary>
    internal async Task FinishOwnershipAsync(HouseholdJournalAuthority authority, Guid operation, CancellationToken ct)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        var row = await context.Db.Set<HouseholdLocalOwnership>().SingleOrDefaultAsync(ct); if (row is null) return;
        if (row.OperationId != operation || row.Stage is not (HouseholdOwnershipStage.Published or HouseholdOwnershipStage.Canceled or HouseholdOwnershipStage.Expired) || Digest(row.BodyJson) != row.Digest) throw Refusal();
        var original = JsonSerializer.Deserialize(row.BodyJson, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation) ?? throw Refusal();
        if (original.Preparation is { } candidate)
        {
            using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
            var committed = membership.ByVersion.Values.Any(item => item.Snapshot().OperationId == operation);
            if (committed != (row.Stage == HouseholdOwnershipStage.Published)) throw Refusal();
            if (!committed && candidate.Statement.OwnerId == binding.IdentityId)
            {
                var fingerprint = new PublicSigningRecipientIdentity(candidate.Statement.OwnerKeys.SigningKey, candidate.Statement.OwnerKeys.WrappingKey).Fingerprint;
                if (membership.ByVersion.Values.Any(item => item.Root.Fingerprint == fingerprint)) throw Refusal();
                var slot = RootSlot(fingerprint); var unused = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(item => item.Slot == slot, ct);
                if (unused is not null) context.Db.Remove(unused);
            }
            var epochSlot = PreparedEpochSlot(operation); var prepared = await context.Db.Set<HouseholdLocalSecret>().SingleOrDefaultAsync(item => item.Slot == epochSlot, ct);
            if (prepared is not null)
            {
                if (committed) { using var current = await ReadEpochCoreAsync(context.Db, membership, candidate.Statement.KeyEpoch, ct); }
                context.Db.Remove(prepared);
            }
        }
        var proposal = await context.Db.Set<HouseholdLocalMembershipProposal>().SingleOrDefaultAsync(ct);
        if (proposal is not null)
        {
            // A terminal participant observation authorizes discarding only this exact
            // handoff's original staged publication. An unrelated uncertain action stays put.
            if (proposal.OperationId != operation || original.Preparation is null || original.Acceptance is null || original.SignedStatement is null) throw Refusal();
            var expected = new HouseholdPublishMembership(binding.DeviceId, original.SignedStatement, original.Acceptance.SignedAcceptance, null,
                original.Preparation.Grants, OwnerRootGrants: original.Acceptance.OwnerRootGrants);
            var expectedJson = JsonSerializer.Serialize(expected, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership);
            if (proposal.BodyJson != expectedJson || proposal.Hash != Digest(expectedJson)) throw Refusal();
            context.Db.Remove(proposal);
        }
        context.Db.Remove(row); await context.CommitAsync(ct);
    }

    /// <summary>Finite exact public protocol digest; no private frame enters this path.</summary>
    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
