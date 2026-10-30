using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdJournal
{
    /// <summary>Changes only local first-run completion under the original protected connected household and native owner.</summary>
    public async Task CompleteOnboardingAsync(HouseholdJournalAuthority authority, Action validateOriginal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(validateOriginal); validateOriginal();
        await using var context = await OpenAsync(ct);
        var binding = await RequireBindingAsync(context, authority, ct);
        if (binding.MembershipVersion <= 0) throw Refusal();
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        _ = await ReadMembershipAsync(context.Db, binding, device, ct);
        var original = await context.Db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
        if (original is null || !original.Enabled || original.GenerationPath != context.Path ||
            original.TransportKind != ProfileSynchronizationKind.Household || original.ProfileId != binding.SpaceId ||
            original.DeviceId != binding.DeviceId || original.IdentityId != binding.IdentityId || original.PeerKey != binding.PeerKey)
            throw Refusal();
        var settings = await context.Db.Settings.OrderBy(row => row.CreatedAt).FirstOrDefaultAsync(ct) ?? throw Refusal();
        settings.OnboardingCompleted = true;
        await context.CommitAsync(ct, validateOriginal);
    }
}
