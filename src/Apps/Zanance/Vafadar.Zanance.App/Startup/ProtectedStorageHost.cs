using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Composes the application's protected storage, original profile lifecycle and safety protection.</summary>
/// <remarks>Construction performs no database/key work. Covered startup supplies explicit installation authority;
/// missing keys or identities cannot select a replacement installation or plaintext provider.</remarks>
internal abstract partial class ProtectedStorageHost(IServiceProvider services,
    Func<CancellationToken, Task<ProtectedStorageInitialization>> initialization, Func<CancellationToken, Task> complete)
    : IAppDatabaseStartup, IProfileCreationHost, IProfileRetirementHost, IBackupSafetyCopyProtection, IBackupPackageRestoreReceiver, IProtectedProfileSessionSource, IBackupSafetyCopyMaintenance, IAutomaticLocalBackupProtection, IBackupSafetyCopyDiscovery
{
    private readonly SemaphoreSlim preparation = new(1, 1);
    private ProtectedProfileStorage? storage;
    private RecoveryCapturingGenerationKeyStore? keys;
    private IDatabaseGenerationFileDurability? durability;
    private ProtectedProfileCreationHost? creation;
    private ProtectedProfileRetirementHost? retirement;
    private IAppDatabaseStartup? startup;
    private string? identityPath;
    private Action? nativeValidation;

    /// <summary>Returns the single original protected session only after covered native composition.</summary>
    public SqlCipherProfileSession<ZananceDbContext> Session => Storage.Session;
    private ProtectedProfileStorage Storage => storage ?? throw new InvalidOperationException("Protected startup has not composed storage.");

    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken ct = default)
    {
        await preparation.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (services.GetService<IProtectedBackupRecovery>() is { } recovery)
            {
                if (await recovery.ReplayAsync(ct).ConfigureAwait(false))
                {
                    startup = null; storage = null; recoveryCatalogue = null; creation = null; retirement = null;
                }
                await recovery.ValidateSelectedAsync(ct).ConfigureAwait(false);
            }
            if (startup is null)
            {
                var authority = await initialization(ct).ConfigureAwait(false);
                await ComposeAsync(ct).ConfigureAwait(false);
                startup = Storage.CreateStartup(authority, this, this);
            }
            await startup.PrepareAsync(ct).ConfigureAwait(false);
            await complete(ct).ConfigureAwait(false);
        }
        finally { preparation.Release(); }
    }

    /// <summary>Builds native effects in the covered worker, without preparing a generation key.</summary>
    private async Task ComposeAsync(CancellationToken ct)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (!location.RequiresProtection) throw new InvalidOperationException("Cipher host requires unpublished protection.");
        var root = Path.GetDirectoryName(location.Path)!;
        var roots = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["database"] = root, ["keys"] = Path.Combine(root, "database-keys"),
            ["portable"] = Path.Combine(FileSystem.AppDataDirectory, "backups"), ["safety"] = Path.Combine(FileSystem.AppDataDirectory, "backups-safety")
        };
        foreach (var path in roots.Values) Directory.CreateDirectory(path);
        durability = CreateDurability(root);
        var keyDurability = CreateDurability(roots["keys"]);
        void Validate()
        {
            // Native validation rejects redirected/foreign roots before any key or financial effect.
            _ = durability.AvailableBytes; _ = keyDurability.AvailableBytes;
        }
        Validate(); nativeValidation = Validate;
        keys = CreateKeys(roots["keys"], Validate);
        identityPath = Path.Combine(root, "database-identities.sqlite");
        var publicRecovery = Path.Combine(root, "recovery-public.sqlite");
        if (File.Exists(publicRecovery))
        {
            var publicFiles = CreateFiles(roots, Validate);
            _ = await publicFiles.CaptureAsync("database", "recovery-public.sqlite", ct).ConfigureAwait(false);
            var identity = await SqliteRecoveryEnvelopeCatalogue.ReadExistingIdentityAsync(publicRecovery, durability, Validate, ct).ConfigureAwait(false);
            recoveryCatalogue = new(publicRecovery, identity, durability, Validate);
        }
        using (var maintenanceOwner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false))
        {
            void Maintain() { location.Access.RequireDrainedOwner(maintenanceOwner); Validate(); }
            await CreateRetention(root, roots, keys, recoveryCatalogue, durability, Maintain).ResumeAsync(ct).ConfigureAwait(false);
        }
        if (recoveryCatalogue is not null)
        {
            await ResumeRecoveryCaptureAsync(recoveryCatalogue, roots, ct).ConfigureAwait(false);
            keys.Attach(recoveryCatalogue);
        }
        var settings = services.GetRequiredService<ISettingsStore>();
        var persistence = services.GetRequiredService<ISettingsPersistence>();
        storage = new(location, () => services.GetRequiredService<ProfileService>(), identityPath, keys,
            durability, Validate, settings, persistence, LocalizationService.PortableKeys,
            Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes, "4.19.0");
        creation = new(persistence, (id, original, owner, ct) => Storage.PrepareNewAsync(id, original, owner, ct));
        retirement = CreateRetirement(root, roots, identityPath, persistence, keys, durability, Validate, recoveryCatalogue);
    }

    /// <inheritdoc />
    public Task CreateReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct) =>
        (creation ?? throw new InvalidOperationException("Protected startup is unavailable.")).CreateReviewedAsync(profiles, review, profile, owner, ct);

    /// <inheritdoc />
    public Task ResumePendingAsync(ProfileService profiles, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct) =>
        (creation ?? throw new InvalidOperationException("Protected startup is unavailable.")).ResumePendingAsync(profiles, owner, ct);

    /// <inheritdoc />
    public Task DeleteReviewedAsync(ProfileService profiles, ProfileCatalogueReview review, LocalProfile profile,
        LocalDatabaseLocation<ZananceDbContext> location, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct) =>
        (retirement ?? throw new InvalidOperationException("Protected startup is unavailable.")).DeleteReviewedAsync(profiles, review, profile, location, owner, ct);

    /// <inheritdoc />
    public Task ResumePendingAsync(ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct) =>
        (retirement ?? throw new InvalidOperationException("Protected startup is unavailable.")).ResumePendingAsync(profiles, location, owner, ct);

    /// <inheritdoc />
    public Task RestoreAsync(BackupManifest manifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries,
        CancellationToken ct) => new Features.Backup.ProtectedBackupRestoreReceiver(
            services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), Storage).RestoreAsync(manifest, entries, ct);

    /// <inheritdoc />
    public Task<byte[]> ProtectAsync(ReadOnlyMemory<byte> package, CancellationToken ct) => UseSafetyAsync(package, true, ct);

    /// <inheritdoc />
    public Task<byte[]> OpenAsync(ReadOnlyMemory<byte> envelope, CancellationToken ct) => UseSafetyAsync(envelope, false, ct);

    /// <inheritdoc />
    public Task<byte[]> ProtectAutomaticAsync(ReadOnlyMemory<byte> package, Action validate, CancellationToken ct) =>
        UseSafetyAsync(package, true, ct, validate);

    /// <inheritdoc />
    public Task CompleteAutomaticAsync(BackupFileInfo file, Action validate, CancellationToken ct) =>
        MaintainCopyAsync(file, ct, validate);

    /// <inheritdoc />
    public Task AfterUploadAsync(BackupFileInfo file, CancellationToken ct = default) => MaintainCopyAsync(file, ct);

    private async Task MaintainCopyAsync(BackupFileInfo file, CancellationToken ct, Action? capturedValidation = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        Session.RequireStable(owner); capturedValidation?.Invoke();
        var capture = services.GetRequiredService<ProfileService>().CaptureStorage();
        void Validate() { location.Access.RequireDrainedOwner(owner); Session.RequireStable(owner); capture.Validate(); capturedValidation?.Invoke(); }
        var root = Path.GetDirectoryName(identityPath!)!;
        var roots = new Dictionary<string, string>
        { ["database"] = root, ["keys"] = Path.Combine(root, "database-keys"),
            ["portable"] = Path.Combine(FileSystem.AppDataDirectory, "backups"), ["safety"] = Path.Combine(FileSystem.AppDataDirectory, "backups-safety") };
        var id = capture.OriginalPaths.Single(pair => pair.Value == capture.CurrentOriginalPath).Key;
        var identity = (await new SqliteDatabaseIdentityCatalogue(identityPath!, Validate).ReadAsync(ct).ConfigureAwait(false))
            .Single(row => row.LegacyProfileId == id);
        var keep = services.GetRequiredService<BackupOptions>().MaxBackupsToKeep;
        await CreateRetention(root, roots, keys!, recoveryCatalogue, durability!, Validate).TrimAsync(identity,
            capture.CurrentOriginalPath, file.FileName, keep,
            services.GetRequiredService<IBackupPackageReader>(), ct).ConfigureAwait(false);
    }

    /// <summary>Binds each safety operation to the exact selected profile under drained original ownership.</summary>
    private async Task<byte[]> UseSafetyAsync(ReadOnlyMemory<byte> input, bool protect, CancellationToken ct, Action? capturedValidation = null)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        Storage.Session.RequireStable(owner); capturedValidation?.Invoke();
        var profiles = services.GetRequiredService<ProfileService>();
        var capture = profiles.CaptureStorage(); capture.Validate();
        var id = capture.OriginalPaths.Single(pair => pair.Value == capture.CurrentOriginalPath).Key;
        void Validate() { location.Access.RequireDrainedOwner(owner); Storage.Session.RequireStable(owner); capture.Validate(); capturedValidation?.Invoke(); }
        var identity = (await new SqliteDatabaseIdentityCatalogue(identityPath!, Validate).ReadAsync(ct).ConfigureAwait(false))
            .Single(row => row.LegacyProfileId == id);
        var protection = new DeviceKeySafetyCopyProtection(identity.ProfileId, identity.InstallationId,
            keys ?? throw new InvalidOperationException("Protected keys are unavailable."), Validate);
        return protect ? await protection.ProtectAsync(input, ct).ConfigureAwait(false)
            : await protection.OpenAsync(input, ct).ConfigureAwait(false);
    }
}
