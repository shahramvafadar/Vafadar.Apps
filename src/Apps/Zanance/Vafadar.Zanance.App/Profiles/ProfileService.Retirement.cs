using System.Security.Cryptography;
using System.Text;
using Vafadar.Core.Settings;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    /// <summary>Returns the actual captured app-private original namespace for native retirement composition.</summary>
    internal string RetirementOriginalPath(string legacy) => PathOf(MainPath(), legacy);
    /// <summary>Captures exact original/removed preference digests for a reviewed inactive target under drained ownership.</summary>
    public ProfileRetirementCatalogueDigest CaptureRetirementCatalogue(ProfileCatalogueReview review, LocalProfile profile,
        ProfileRetirementCheckpoint request, DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            location.Access.RequireDrainedOwner(owner); ValidateWorkCore(review, true); RequireReviewedProfile(review, profile);
            RequireRetirementTarget(request, owner);
            if (request.LegacyProfileId != profile.Id) throw new InvalidOperationException("Retirement differs from the reviewed target.");
            return new(CatalogueDigest(preferences.Get(CatalogueKey)), CatalogueDigest(RemovedCatalogue(review, profile, request)));
        }
    }

    /// <summary>Returns whether the actual complete preference state is the recorded post-removal state; unrelated changes fail.</summary>
    public bool ValidateRetirementCatalogue(ProfileRetirementCheckpoint request, ProfileRetirementCatalogueDigest captured,
        DatabaseAccessGate.ExclusiveLease owner)
    {
        lock (CatalogueGate)
        {
            RequireRetirementTarget(request, owner);
            var actual = CatalogueDigest(preferences.Get(CatalogueKey));
            if (actual != captured.Original && actual != captured.Removed)
                throw new InvalidOperationException("Device catalogue differs from the reviewed retirement.");
            var removed = actual == captured.Removed;
            if (All.Any(profile => profile.Id == request.LegacyProfileId) == removed)
                throw new InvalidOperationException("Retirement catalogue identity does not match its fingerprint.");
            return removed;
        }
    }

    /// <summary>Publishes exactly the captured atomic removal, then requires native settings persistence and unchanged readback.</summary>
    public async Task RemoveRetirementCatalogueAsync(ProfileRetirementCheckpoint request, SqliteProfileRetirementJournal journal,
        ISettingsPersistence persistence, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(persistence); ct.ThrowIfCancellationRequested();
        if (await journal.ReadAsync(ct) != request || request.Phase != ProfileRetirementPhase.Authorized)
            throw new InvalidOperationException("Catalogue removal requires the exact Authorized request.");
        var captured = await journal.ReadCatalogueAsync(ct);
        lock (CatalogueGate)
        {
            if (!ValidateRetirementCatalogue(request, captured, owner))
            {
                var review = ReadWorkReview(); var profile = review.Profiles.Single(item => item.Id == request.LegacyProfileId);
                var after = RemovedCatalogue(review, profile, request);
                if (CatalogueDigest(after) != captured.Removed) throw new InvalidOperationException("Retirement projection changed before publication.");
                preferences.Set(CatalogueKey, after);
            }
        }
        // A failed native write retains the exact in-memory removal for idempotent persistence retry;
        // files and identities remain until the host records the durable catalogue barrier.
        await persistence.PersistAsync(ct);
        lock (CatalogueGate)
        {
            if (!ValidateRetirementCatalogue(request, captured, owner)) throw new IOException("Persisted retirement catalogue differs.");
        }
        if (await journal.ReadAsync(ct) != request) throw new IOException("Retirement changed during catalogue persistence.");
    }

    /// <summary>Publishes one catalogue notification after an actual native catalogue operation finishes.</summary>
    public void NotifyCatalogueChanged(DatabaseAccessGate.ExclusiveLease owner)
    { location.Access.RequireDrainedOwner(owner); Changed?.Invoke(this, EventArgs.Empty); }

    private void RequireRetirementTarget(ProfileRetirementCheckpoint request, DatabaseAccessGate.ExclusiveLease owner)
    {
        location.Access.RequireDrainedOwner(owner);
        if (request.LegacyProfileId.Length == 0 || CurrentId(preferences) == request.LegacyProfileId)
            throw new InvalidOperationException("Main and current profiles cannot be retired.");
        var original = PathOf(MainPath(), request.LegacyProfileId);
        if (location.Path == original || location.Path.StartsWith(original + ".", StringComparison.Ordinal))
            throw new InvalidOperationException("The target still owns the current database endpoint.");
    }
    private string RemovedCatalogue(ProfileCatalogueReview review, LocalProfile profile, ProfileRetirementCheckpoint request)
    {
        var state = ReadCatalogue(preferences) with { List = EncodeProfiles(review.Profiles.Where(item => !item.IsMain && item.Id != profile.Id)) };
        if (review.Maximum is not null && !review.RequiresSelection)
            state = state with { Choice = EncodeField(string.Join(';', review.SelectedIds.Where(id => id != profile.Id).Select(id => id.Length == 0 ? "~" : id))),
                ChoiceRevision = request.OperationId.ToString("N") };
        return EncodeCatalogue(state, request.OperationId);
    }
    private string CatalogueDigest(string? packet)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var value in new[] { Path.GetFullPath(MainPath()), packet, preferences.Get(ListKey), preferences.Get(MainNameKey), preferences.Get(CurrentKey) })
        {
            var bytes = value is null ? [] : Encoding.UTF8.GetBytes(value);
            var size = new byte[4]; System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(size, value is null ? -1 : bytes.Length);
            digest.AppendData(size); digest.AppendData(bytes);
        }
        return Convert.ToHexStringLower(digest.GetHashAndReset());
    }
}
