using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Profiles;

/// <summary>Composes real device catalogue publication, authenticated native inventory and exact identity retirement.</summary>
/// <remarks>The native host supplies actual private roots/key/file/durability/settings effects. This is not
/// default main registration, key recovery, orphan creation reconciliation or reference-aware retention.</remarks>
public sealed class ProtectedProfileRetirementHost(
    string directory, string identityPath, ISettingsPersistence persistence, IDatabaseGenerationKeyStore keys,
    IDatabaseGenerationFileDurability durability, IProfileRetirementReferenceFiles files,
    Func<ProfileRetirementCheckpoint, SqliteProfileRetirementJournal, CancellationToken, Task> removeFiles,
    Func<SqliteProfileRetirementJournal, CancellationToken, Task> verifyFilesRemoved) : IProfileRetirementHost
{
    private const string Prefix = "profile-retirement-";

    /// <inheritdoc />
    public async Task DeleteReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        LocalDatabaseLocation<ZananceDbContext> location, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); }
        Check(); profiles.ValidateWorkReview(review);
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Check);
        var identity = (await identities.ReadAsync(ct)).Single(row => row.LegacyProfileId == profile.Id);
        RequireDirectory();
        var pending = new List<(ProfileRetirementCheckpoint Request, SqliteProfileRetirementJournal Journal)>();
        var existing = Directory.EnumerateFiles(directory, Prefix + "*.sqlite").Take(1025).ToArray();
        if (existing.Length > 1024) throw new IOException("Retirement request listing exceeds its bound.");
        foreach (var path in existing)
        {
            Check(); var prior = RequestFromName(Path.GetFileName(path));
            if (prior.LegacyProfileId != profile.Id || prior.ProfileId != identity.ProfileId || prior.InstallationId != identity.InstallationId) continue;
            var saved = new SqliteProfileRetirementJournal(path, prior, Check);
            if ((await saved.SynchronizeAndReadAsync(durability, ct)).Phase != ProfileRetirementPhase.Completed) pending.Add((prior, saved));
        }
        if (pending.Count > 1) throw new IOException("Multiple pending requests require exact retirement reconciliation.");
        if (pending.Count == 1)
        {
            await ResumeAsync(profiles, location, owner, pending[0].Request, pending[0].Journal, ct);
            profiles.NotifyCatalogueChanged(owner); return;
        }
        var request = new ProfileRetirementCheckpoint(profile.Id, identity.ProfileId, identity.InstallationId, Guid.NewGuid());
        var captured = profiles.CaptureRetirementCatalogue(review, profile, request, owner);
        var journal = new SqliteProfileRetirementJournal(RequestPath(request), request, Check);
        await journal.CreateNewAsync(captured, durability, ct);
        await ProfileRetirementReferenceDiscovery.CaptureAsync(journal, identities, profiles.RetirementOriginalPath(profile.Id),
            "pro.vafadar.zanance", files, keys, durability, Check, ct);
        await ResumeAsync(profiles, location, owner, request, journal, ct);
        profiles.NotifyCatalogueChanged(owner);
    }

    /// <inheritdoc />
    public async Task ResumePendingAsync(ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); }
        Check(); RequireDirectory();
        var names = Directory.EnumerateFiles(directory, Prefix + "*.sqlite").Take(1025).ToArray();
        if (names.Length > 1024) throw new IOException("Retirement request listing exceeds its bound.");
        foreach (var path in names)
        {
            Check(); var request = RequestFromName(Path.GetFileName(path));
            var journal = new SqliteProfileRetirementJournal(path, request, Check);
            var state = await journal.SynchronizeAndReadAsync(durability, ct);
            // Completion has already verified durable catalogue/artifacts/identity. Later legitimate
            // catalogue edits must not replay a historical removal or be compared with its old digest.
            if (state.Phase == ProfileRetirementPhase.Completed) continue;
            await ResumeAsync(profiles, location, owner, request, journal, ct);
            profiles.NotifyCatalogueChanged(owner);
        }
    }

    private async Task ResumeAsync(ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location,
        DatabaseAccessGate.ExclusiveLease owner, ProfileRetirementCheckpoint request, SqliteProfileRetirementJournal journal, CancellationToken ct)
    {
        var captured = await journal.ReadCatalogueAsync(ct);
        if (!await journal.HasArtifactInventoryAsync(ct))
        {
            if (await journal.ReadAsync(ct) != request || profiles.ValidateRetirementCatalogue(request, captured, owner))
                throw new IOException("Missing inventory cannot resume after catalogue removal.");
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            await ProfileRetirementReferenceDiscovery.CaptureAsync(journal, identities, profiles.RetirementOriginalPath(request.LegacyProfileId),
                "pro.vafadar.zanance", files, keys, durability, owner.Validate, ct);
        }
        var effects = new Effects(profiles, location, owner, persistence, identityPath, durability,
            journal, captured, removeFiles, verifyFilesRemoved);
        await new ProfileRetirementCoordinator(journal, durability, effects).ResumeAsync(ct);
    }
    private string RequestPath(ProfileRetirementCheckpoint request)
    {
        RequireDirectory();
        return Path.Combine(directory, $"{Prefix}{request.LegacyProfileId}-{request.ProfileId:N}-{request.InstallationId:N}-{request.OperationId:N}.sqlite");
    }
    private static ProfileRetirementCheckpoint RequestFromName(string name)
    {
        var parts = name[Prefix.Length..^".sqlite".Length].Split('-');
        if (parts.Length != 4 || parts.Any(part => !Guid.TryParseExact(part, "N", out var id) || id == Guid.Empty || id.ToString("N") != part))
            throw new IOException("Retirement request filename is not canonical.");
        return new(parts[0], Guid.ParseExact(parts[1], "N"), Guid.ParseExact(parts[2], "N"), Guid.ParseExact(parts[3], "N"));
    }
    private void RequireDirectory()
    {
        if (!Path.IsPathFullyQualified(directory) || Path.GetDirectoryName(Path.GetFullPath(identityPath)) != Path.GetFullPath(directory)
            || !Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Retirement requires the captured private identity directory.");
    }
    private sealed class Effects(ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location,
        DatabaseAccessGate.ExclusiveLease owner, ISettingsPersistence persistence, string identityPath,
        IDatabaseGenerationFileDurability durability, SqliteProfileRetirementJournal journal, ProfileRetirementCatalogueDigest captured,
        Func<ProfileRetirementCheckpoint, SqliteProfileRetirementJournal, CancellationToken, Task> remove,
        Func<SqliteProfileRetirementJournal, CancellationToken, Task> verify) : IProfileRetirementEffects
    {
        public async Task ValidateAsync(ProfileRetirementCheckpoint state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner);
            var removed = profiles.ValidateRetirementCatalogue(state, captured, owner);
            if (state.Phase >= ProfileRetirementPhase.CatalogueRemoved && !removed)
                throw new IOException("Recorded catalogue removal did not occur.");
            if (state.Phase >= ProfileRetirementPhase.ArtifactsRemoved) await verify(journal, ct);
            if (state.Phase == ProfileRetirementPhase.Completed)
            {
                var rows = await new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate).ReadAsync(ct);
                if (rows.Any(row => row.ProfileId == state.ProfileId)) throw new IOException("Recorded identity retirement did not occur.");
            }
        }
        public Task RemoveCatalogueAsync(ProfileRetirementCheckpoint state, CancellationToken ct) => profiles.RemoveRetirementCatalogueAsync(state, journal, persistence, owner, ct);
        public Task RemoveArtifactsAsync(ProfileRetirementCheckpoint state, CancellationToken ct) => remove(state, journal, ct);
        public Task RetireIdentityAsync(ProfileRetirementCheckpoint state, CancellationToken ct) =>
            new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate).RetireAsync(state, journal, durability, token => verify(journal, token), ct);
    }
}
