using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Persists only an exact verified current-owner root; a missing existing frame is never silently replaced by different private material.</summary>
    public async Task RecordOwnerRootAsync(HouseholdJournalAuthority authority, HouseholdOwnerRootGrant grant, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var history = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (history.Current.Snapshot().OwnerId != authority.IdentityId || !history.ByVersion.TryGetValue(grant.MembershipVersion, out var granting)) throw Refusal();
        using var root = HouseholdRecoveryMaterial.OpenOwnerRoot(cryptography, granting, history.Current, grant, binding.DeviceId, device);
        var frame = root.ExportForProtectedStorage();
        try { await StoreRootAsync(context.Db, root.Public, frame, ct); await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Retains a proposed complete replacement before any remote request, tied to the captured protected generation and independently verified old root.</summary>
    public async Task StageOwnerRootAsync(HouseholdJournalAuthority authority, SigningRecipientIdentity proposed, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var current = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (current.Current.Snapshot().OwnerId != authority.IdentityId || proposed.Public.SigningKey == current.Current.Root.SigningKey ||
            proposed.Public.WrappingKey == current.Current.Root.WrappingKey) throw Refusal();
        var frame = proposed.ExportForProtectedStorage();
        try { await StoreRootAsync(context.Db, proposed.Public, frame, ct); await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Reads an existing candidate only; the caller still needs its retained public intent and current complete signed transition.</summary>
    public async Task<SigningRecipientIdentity> ReadPreparedOwnerRootAsync(HouseholdJournalAuthority authority,
        PublicSigningRecipientIdentity proposed, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); _ = await RequireBindingAsync(context, authority, ct);
        var root = await ReadIdentityAsync(context.Db, RootSlot(proposed.Fingerprint), ct);
        if (root.Public != proposed) { root.Dispose(); throw Refusal(); } context.Check(); return root;
    }

    private static async Task StoreRootAsync(ZananceDbContext db, PublicSigningRecipientIdentity root, byte[] frame, CancellationToken ct)
    {
        var slot = RootSlot(root.Fingerprint);
        var existing = await db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == slot, ct);
        if (existing is null) db.Add(new HouseholdLocalSecret { Slot = slot, Frame = frame });
        else
        {
            try { if (!CryptographicOperations.FixedTimeEquals(existing.Frame, frame)) throw Refusal(); }
            finally { CryptographicOperations.ZeroMemory(existing.Frame); }
        }
    }
}
