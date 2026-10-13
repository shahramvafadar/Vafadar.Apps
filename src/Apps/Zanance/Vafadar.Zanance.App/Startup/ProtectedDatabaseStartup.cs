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
    bool initializeInstallation, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
    Func<string, ProfileRestoreReference, SqlCipherPackageRestoreReceiver> createReceiver,
    int maximumBytes, string cipherVersion) : IAppDatabaseStartup
{
    private readonly SemaphoreSlim operations = new(1, 1);
    private bool completed;

    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        await operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (completed) return;
            using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken).ConfigureAwait(false);
            var capture = profiles.CaptureStorage();
            capture.Validate();
            if (location.IsProtected || location.Path != capture.CurrentOriginalPath)
                throw new InvalidOperationException("Protected startup requires the original unpublished endpoint.");
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath, capture.OriginalPaths,
                initializeInstallation, keys, durability, createReceiver, capture.Validate,
                maximumBytes, cipherVersion, cancellationToken).ConfigureAwait(false);
            // No context is published midway through all-profile preparation, even for a non-main selection.
            capture.Validate(); cancellationToken.ThrowIfCancellationRequested();
            await session.StartAsync(capture.CurrentOriginalPath, owner, cancellationToken).ConfigureAwait(false);
            // Publication succeeded. A repeated callback must retain the same owner rather than resolve it twice.
            completed = true;
        }
        finally { operations.Release(); }
    }
}
