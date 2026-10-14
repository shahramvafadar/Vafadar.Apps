using Vafadar.Core.Settings;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Composes durable device Add intent, idempotent native preparation and actual catalogue persistence.</summary>
/// <remarks>Native preparation requalifies existing identities/keys/files on retry, without replacing missing keys.
/// Device profile names follow existing preference privacy; this request is never portable.</remarks>
public sealed class ProtectedProfileCreationHost(ISettingsPersistence persistence,
    Func<string, string, DatabaseAccessGate.ExclusiveLease, CancellationToken, Task> prepare) : IProfileCreationHost
{
    /// <inheritdoc />
    public async Task CreateReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        var raw = profiles.CaptureCreation(review, profile, owner);
        await ContinueAsync(profiles, raw, owner, ct);
    }
    /// <inheritdoc />
    public async Task ResumePendingAsync(ProfileService profiles, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        if (profiles.ReadCreation(owner) is { } raw) await ContinueAsync(profiles, raw, owner, ct);
    }
    private async Task ContinueAsync(ProfileService profiles, string raw, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); var profile = profiles.ValidateCreation(raw, owner);
        // Requalify exact intent even when an observable native write previously reported failure.
        await persistence.PersistAsync(ct);
        if (profiles.ReadCreation(owner) != raw) throw new IOException("Persisted creation intent differs.");
        if (raw.Split('|')[1] == "0")
        {
            await prepare(profile.Id, profiles.RetirementOriginalPath(profile.Id), owner, ct);
            ct.ThrowIfCancellationRequested(); profiles.PublishCreation(raw, owner);
            await persistence.PersistAsync(ct);
            _ = profiles.ValidateCreation(raw, owner);
            raw = profiles.CompleteCreation(raw, owner);
            await persistence.PersistAsync(ct);
            if (profiles.ReadCreation(owner) != raw) throw new IOException("Persisted creation completion differs.");
        }
        profiles.ForgetCreation(raw, owner);
        await persistence.PersistAsync(ct);
        if (profiles.ReadCreation(owner) is not null) throw new IOException("Completed creation intent remains.");
        profiles.NotifyCatalogueChanged(owner);
    }
}
