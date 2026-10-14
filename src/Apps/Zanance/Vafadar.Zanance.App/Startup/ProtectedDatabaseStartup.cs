using Microsoft.Data.Sqlite;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Connects complete native profile preparation to the covered application startup contract.</summary>
/// <remarks>The final native host supplies verified provider/key/file/restore effects. Explicit first-install
/// authority is supplied by that host, never inferred here from missing metadata. Main registration remains a
/// separate adoption step, coupled to recovery and OS policy; this class does not supply plaintext fallback.</remarks>
public sealed class ProtectedDatabaseStartup(LocalDatabaseLocation<ZananceDbContext> location,
    ProfileService profiles, SqlCipherProfileSession<ZananceDbContext> session, string identityPath,
    ProtectedStorageInitialization initialization, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
    Func<string, ProfileRestoreReference, SqlCipherPackageRestoreReceiver> createReceiver,
    int maximumBytes, string cipherVersion,
    Func<SqliteConnection, CancellationToken, Task>? buildEmptySchema = null,
    IProfileRetirementHost? retirement = null,
    IProfileCreationHost? creation = null,
    Func<string, ProfileRestoreReference, DatabaseAccessGate.ExclusiveLease, SqlCipherPackageRestoreReceiver>? createOwnedReceiver = null) : IAppDatabaseStartup
{
    private readonly SemaphoreSlim operations = new(1, 1);
    private bool completed;
    private ProfileStorageCapture? preparedCapture;

    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        await operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (completed) return;
            using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken).ConfigureAwait(false);
            if (retirement is not null && preparedCapture is null)
                await retirement.ResumePendingAsync(profiles, location, owner, cancellationToken).ConfigureAwait(false);
            if (creation is not null && preparedCapture is null)
                await creation.ResumePendingAsync(profiles, owner, cancellationToken).ConfigureAwait(false);
            var capture = profiles.CaptureStorage();
            capture.Validate();
            if (location.IsProtected && preparedCapture is not null)
            {
                // Only this startup's already prepared catalogue may finish after an observer failure.
                // The session verifies its exact retained publication; no bootstrap/key work is repeated.
                preparedCapture.Validate();
                await session.StartAsync(preparedCapture.CurrentOriginalPath, owner, cancellationToken).ConfigureAwait(false);
                completed = true; return;
            }
            if (location.IsProtected || location.Path != capture.CurrentOriginalPath)
                throw new InvalidOperationException("Protected startup requires the original unpublished endpoint.");
            if (!Enum.IsDefined(initialization)) throw new ArgumentOutOfRangeException(nameof(initialization));
            if (initialization == ProtectedStorageInitialization.NewInstallation)
            {
                if (buildEmptySchema is null || capture.OriginalPaths.Count != 1 || !capture.OriginalPaths.ContainsKey(""))
                    throw new InvalidOperationException("New installation requires only the captured main profile and its real schema builder.");
                var original = capture.OriginalPaths[""];
                if (!Path.IsPathFullyQualified(identityPath) || Path.GetDirectoryName(Path.GetFullPath(identityPath)) != Path.GetDirectoryName(original))
                    throw new IOException("New installation metadata must remain in the captured private directory.");
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, capture.Validate);
                if (!File.Exists(identityPath))
                {
                    // Explicit new-install authority never permits recreating lost existing identities.
                    // Only its own staged identity publication may resume before any profile intent exists.
                    var directory = Path.GetDirectoryName(original)!;
                    if (Directory.EnumerateFiles(directory).Any(path =>
                        path != identityPath + ".creating" &&
                        (Path.GetFileName(path).EndsWith(".key", StringComparison.OrdinalIgnoreCase)
                            || Path.GetFileName(path).Contains(".db", StringComparison.OrdinalIgnoreCase)
                            || Path.GetFileName(path).Contains(".sqlite", StringComparison.OrdinalIgnoreCase))))
                        throw new IOException("Retained financial/key state requires its original installation identity.");
                    await identities.CreateNewAsync([""], durability, cancellationToken).ConfigureAwait(false);
                }
                using var prepared = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, true,
                    keys, durability, buildEmptySchema, owner, capture.Validate,
                    maximumBytes, cipherVersion, cancellationToken).ConfigureAwait(false);
            }
            // Bootstrap identifies profiles by legacy id; the host receiver owns an exact original path.
            // Translate only through this unchanged complete capture, never through current selection.
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath, capture.OriginalPaths,
                initialization == ProtectedStorageInitialization.LegacyPlaintextProfiles, keys, durability,
                (legacy, reference) => createOwnedReceiver is null ? createReceiver(legacy, reference) : createOwnedReceiver(capture.OriginalPaths[legacy], reference, owner), capture.Validate,
                maximumBytes, cipherVersion, cancellationToken).ConfigureAwait(false);
            // No context is published midway through all-profile preparation, even for a non-main selection.
            capture.Validate(); cancellationToken.ThrowIfCancellationRequested();
            preparedCapture = capture;
            await session.StartAsync(capture.CurrentOriginalPath, owner, cancellationToken).ConfigureAwait(false);
            // Publication succeeded. A repeated callback must retain the same owner rather than resolve it twice.
            completed = true;
        }
        finally { operations.Release(); }
    }
}

/// <summary>Explicit host authority for startup; a missing file never chooses one of these modes.</summary>
public enum ProtectedStorageInitialization
{
    /// <summary>Only existing durable identities/history may be reopened; missing state fails closed.</summary>
    ExistingInstallation,
    /// <summary>Known existing plaintext originals may receive their initial installation identities.</summary>
    LegacyPlaintextProfiles,
    /// <summary>An explicitly new single-main installation creates its real schema only in memory.</summary>
    NewInstallation
}
