using System.Globalization;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Vafadar.Backup.Security;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;

namespace Vafadar.Backup;

/// <summary>
/// Default <see cref="IBackupService"/>.
/// </summary>
/// <remarks>
/// A backup is: all sources → ZIP package with manifest and checksums → optional AES-GCM encryption → upload.
/// A restore validates everything (password, app id, app version, checksums) before any source is touched.
/// Only one backup or restore runs at a time. The heavy work – the database snapshot, compression, checksums and the
/// PBKDF2 key derivation (600,000 iterations) – runs on the thread pool, so a caller on the UI thread stays responsive.
/// </remarks>
public sealed partial class BackupService : IBackupService, IBackupPackageReader, IAutomaticLocalBackupService
{
    internal const string LastBackupKey = "backup.lastBackupAt";

    private readonly IReadOnlyList<IBackupSource> _sources;
    private readonly IReadOnlyList<IBackupSummaryProvider> _summaries;
    private readonly IAppEnvironment _app;
    private readonly ISettingsStore _settings;
    private readonly TimeProvider _time;
    private readonly BackupOptions _options;
    private readonly IBackupPackageRestoreReceiver? _restoreReceiver;
    private readonly IBackupSafetyCopyProtection? _safetyProtection;
    private readonly IAutomaticLocalBackupProtection? _automaticProtection;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the service.</summary>
    public BackupService(
        IEnumerable<IBackupSource> sources,
        IAppEnvironment app,
        ISettingsStore settings,
        TimeProvider time,
        BackupOptions options,
        IBackupPackageRestoreReceiver? restoreReceiver = null,
        IBackupSafetyCopyProtection? safetyProtection = null,
        IAutomaticLocalBackupProtection? automaticProtection = null)
        : this(sources, [], app, settings, time, options, restoreReceiver, safetyProtection, automaticProtection)
    {
    }

    /// <summary>Creates the service with summary providers.</summary>
    public BackupService(
        IEnumerable<IBackupSource> sources,
        IEnumerable<IBackupSummaryProvider> summaries,
        IAppEnvironment app,
        ISettingsStore settings,
        TimeProvider time,
        BackupOptions options,
        IBackupPackageRestoreReceiver? restoreReceiver = null,
        IBackupSafetyCopyProtection? safetyProtection = null,
        IAutomaticLocalBackupProtection? automaticProtection = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(summaries);
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(options);

        _sources = [.. sources];
        _summaries = [.. summaries];
        _app = app;
        _settings = settings;
        _time = time;
        _options = options;
        _restoreReceiver = restoreReceiver;
        _safetyProtection = safetyProtection;
        _automaticProtection = automaticProtection;
    }

    /// <inheritdoc />
    public DateTimeOffset? LastBackupAt =>
        DateTimeOffset.TryParse(_settings.Get(LastKey()), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;

    /// <inheritdoc />
    public bool IsAutomaticBackupDue() =>
        _options.AutomaticBackupInterval is { } interval
        && (LastBackupAt is not { } last || _time.GetUtcNow() - last >= interval);

    // The app id in file names, with the backup set when one is used (BackupOptions.FileSet).
    private string FileAppId() => FileAppId(_options.FileSet?.Invoke());

    // One call captures its original profile before waiting; filenames and history must not borrow a later profile.
    private string FileAppId(string? set)
    {
        if (string.IsNullOrEmpty(set))
        {
            return _app.AppId;
        }

        if (set.IndexOfAny(['_', '~', '/', '\\']) >= 0)
        {
            throw new InvalidOperationException("A backup set must not contain '_', '~', '/' or '\\'.");
        }

        return $"{_app.AppId}~{set}";
    }

    private string LastKey() => LastKey(_options.FileSet?.Invoke());
    private static string LastKey(string? set) => set is { Length: > 0 } ? $"{LastBackupKey}.{set}" : LastBackupKey;
    private void RequireFileAppId(string original, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.Equals(original, FileAppId(), StringComparison.Ordinal))
            throw new InvalidOperationException("The original backup profile changed.");
    }

    /// <inheritdoc />
    public Task<BackupFileInfo> CreateBackupAsync(IBackupStorage storage, string? password = null, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(storage, password, recordAsBackup: true, cancellationToken);

    /// <inheritdoc />
    public Task<BackupFileInfo> CreateSafetyCopyAsync(IBackupStorage storage, CancellationToken cancellationToken = default) =>
        CreateCoreAsync(storage, null, recordAsBackup: false, cancellationToken);

    private async Task<BackupFileInfo> CreateCoreAsync(IBackupStorage storage, string? password, bool recordAsBackup, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        var set = _options.FileSet?.Invoke();
        var originalAppId = FileAppId(set);
        var originalLastKey = LastKey(set);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            RequireFileAppId(originalAppId, cancellationToken);
            var createdAt = _time.GetUtcNow();
            var protectedSafety = !recordAsBackup && _safetyProtection is not null;
            var package = await CreatePackageCoreAsync(createdAt, password, cancellationToken,
                protectedSafety ? DeviceSafetyEncryption.MaximumPackageBytes : null);
            BackupFileInfo file;
            try
            {
                RequireFileAppId(originalAppId, cancellationToken);
                if (protectedSafety)
                {
                    var plaintext = package;
                    try { package = await Task.Run(() => _safetyProtection!.ProtectAsync(plaintext, cancellationToken), cancellationToken); }
                    finally { CryptographicOperations.ZeroMemory(plaintext); }
                }
                using var content = new MemoryStream(package, writable: false);
                // Separate opaque safety identities cannot overwrite another copy in the same second,
                // and cannot enter portable discovery or its timestamp-based retention.
                RequireFileAppId(originalAppId, cancellationToken);
                var name = protectedSafety ? $"safety-{Guid.NewGuid():N}.vbs" : BackupFileName.Create(originalAppId, createdAt);
                file = await storage.UploadAsync(name, content, cancellationToken);
            }
            finally { CryptographicOperations.ZeroMemory(package); }

            if (recordAsBackup)
            {
                try { _settings.Set(originalLastKey, createdAt.ToString("O", CultureInfo.InvariantCulture)); }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // The original upload receipt is already known; a preference-store failure must not invite
                    // another export/upload. The visible discovered file remains the authority for that copy.
                    System.Diagnostics.Debug.WriteLine($"Stored backup timestamp not recorded: {ex.GetType().Name}");
                }
            }

            try
            {
                // Internal key/file references require their own recovery-aware retention, never portable pruning.
                if (!protectedSafety) await ApplyRetentionCoreAsync(storage, cancellationToken, originalAppId);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Upload has returned its durable receipt. Cancelled/retired retention cannot turn that stored copy
                // into an unknown failure and encourage a second upload; later explicit work may retry pruning.
                System.Diagnostics.Debug.WriteLine($"Backup retention failed: {ex.GetType().Name}");
            }

            return file;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> CreatePackageAsync(string? password = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await CreatePackageCoreAsync(_time.GetUtcNow(), password, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(IBackupStorage storage, CancellationToken cancellationToken = default)
        => await ListCoreAsync(storage, allSets: false, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> DiscoverBackupsAsync(IBackupStorage storage, CancellationToken cancellationToken = default)
        => await ListCoreAsync(storage, allSets: true, cancellationToken);

    // Restore on another installation must discover unknown profile ids, while retention stays set-specific.
    private async Task<IReadOnlyList<BackupFileInfo>> ListCoreAsync(IBackupStorage storage, bool allSets, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);

        var fileAppId = FileAppId();
        var files = await storage.ListAsync(cancellationToken);
        // A remote provider can complete after cancellation. Discovery spans profile sets,
        // but that does not permit a retired native request to publish its late result.
        cancellationToken.ThrowIfCancellationRequested();
        if (!allSets) RequireFileAppId(fileAppId, cancellationToken);
        return
        [
            .. files
                .Select(file => (File: file, Parsed: BackupFileName.TryParse(file.FileName, out var appId, out var createdAt), AppId: appId, CreatedAt: createdAt))
                .Where(x => x.Parsed && (string.Equals(x.AppId, allSets ? _app.AppId : fileAppId, StringComparison.Ordinal)
                    || (allSets && x.AppId!.StartsWith(_app.AppId + "~", StringComparison.Ordinal))))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.File),
        ];
    }

    /// <inheritdoc />
    public async Task<BackupManifest> RestoreAsync(IBackupStorage storage, BackupFileInfo file, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(file);

        byte[] package;
        await using (var stream = await storage.OpenReadAsync(file.Id, cancellationToken))
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken);
            package = buffer.ToArray();
        }

        return await RestorePackageAsync(package, password, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReadValidatedPortableAsync(byte[] package, string? password, int maximumBytes,
        Func<BackupManifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>, CancellationToken, Task> consume,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(package); ArgumentNullException.ThrowIfNull(consume);
        if (maximumBytes <= 0 || package.Length > (long)maximumBytes + 1024)
            throw new BackupException(BackupError.InvalidFormat, "Portable recovery package exceeds its bound.");
        if (DeviceSafetyEncryption.IsEncrypted(package))
            throw new BackupException(BackupError.InvalidFormat, "Device-local safety copies are not portable key-loss recovery files.");
        // This bounded portable-only reader neither reads live sources nor opens device keys. Retention may
        // invoke it while creation owns the stateful backup gate and native maintenance owns the database.
        // Reentering that gate would deadlock; state-changing create/restore operations remain serialized.
        var (manifest, entries) = await Task.Run(() => OpenCoreAsync(package, password, ct, maximumBytes), ct).ConfigureAwait(false);
        try
        {
            var borrowed = new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(
                entries.ToDictionary(entry => entry.Key, entry => (ReadOnlyMemory<byte>)entry.Value, StringComparer.Ordinal));
            await consume(manifest, borrowed, ct).ConfigureAwait(false);
        }
        finally { foreach (var content in entries.Values) CryptographicOperations.ZeroMemory(content); }
    }

    /// <inheritdoc />
    public async Task<BackupManifest> InspectPackageAsync(byte[] package, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        var (manifest, entries) = await OpenAsync(package, password, cancellationToken);
        try { return manifest; }
        finally { foreach (var content in entries.Values) CryptographicOperations.ZeroMemory(content); }
    }

    /// <inheritdoc />
    public async Task<BackupManifest> RestorePackageAsync(byte[] package, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var (manifest, entries) = await OpenAsync(package, password, cancellationToken);
            try
            {
                if (_restoreReceiver is not null)
                {
                    // D-201: whole-package ownership is exclusive. A failed receiver must never trigger legacy writes.
                    var borrowed = new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(
                        entries.ToDictionary(entry => entry.Key, entry => (ReadOnlyMemory<byte>)entry.Value, StringComparer.Ordinal));
                    await Task.Run(() => _restoreReceiver.RestoreAsync(manifest, borrowed, cancellationToken), cancellationToken);
                    return manifest;
                }
                // Everything is validated; only now is existing data replaced. Sources without an entry (e.g. added in a
                // later app version than the backup) keep their current data.
                await Task.Run(
                    async () =>
                    {
                        foreach (var source in _sources)
                        {
                            if (entries.TryGetValue(source.Name, out var content))
                            {
                                using var stream = new MemoryStream(content, writable: false);
                                await source.RestoreAsync(stream, cancellationToken);
                            }
                        }
                    },
                    cancellationToken);

                return manifest;
            }
            finally
            {
                foreach (var content in entries.Values) CryptographicOperations.ZeroMemory(content);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> ApplyRetentionAsync(IBackupStorage storage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        var originalAppId = FileAppId();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            RequireFileAppId(originalAppId, cancellationToken);
            return await ApplyRetentionCoreAsync(storage, cancellationToken, originalAppId);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Decrypts, reads and validates a package without touching any data (on the thread pool, see the remarks).
    private Task<(BackupManifest Manifest, IReadOnlyDictionary<string, byte[]> Entries)> OpenAsync(byte[] package, string? password, CancellationToken cancellationToken) =>
        Task.Run(() => OpenCoreAsync(package, password, cancellationToken), cancellationToken);

    private async Task<(BackupManifest Manifest, IReadOnlyDictionary<string, byte[]> Entries)> OpenCoreAsync(byte[] package, string? password, CancellationToken cancellationToken, int? maximumBytes = null)
    {
        byte[]? decoded = null;
        var originalPackage = package;
        try
        {
            if (DeviceSafetyEncryption.IsEncrypted(package))
            {
                if (_safetyProtection is null) throw new BackupException(BackupError.InvalidFormat, "This internal safety copy requires its existing local key context.");
                decoded = await _safetyProtection.OpenAsync(package, cancellationToken);
                package = decoded;
            }
            else if (BackupEncryption.IsEncrypted(package))
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new BackupException(BackupError.PasswordRequired, "This backup is protected with a password.");
                }

                decoded = BackupEncryption.Decrypt(package, password);
                package = decoded;
            }

            var result = await BackupPackage.ReadAsync(package, cancellationToken,
                maximumBytes ?? (decoded is not null && DeviceSafetyEncryption.IsEncrypted(originalPackage) ? DeviceSafetyEncryption.MaximumPackageBytes : null));
            try { Validate(result.Manifest); return result; }
            catch
            {
                foreach (var content in result.Entries.Values) CryptographicOperations.ZeroMemory(content);
                throw;
            }
        }
        finally { if (decoded is not null) CryptographicOperations.ZeroMemory(decoded); }
    }

    // Builds the package on the thread pool (see the remarks).
    private Task<byte[]> CreatePackageCoreAsync(DateTimeOffset createdAt, string? password, CancellationToken cancellationToken, int? maximumBytes = null) =>
        Task.Run(() => BuildPackageAsync(createdAt, password, cancellationToken, maximumBytes), cancellationToken);

    private async Task<byte[]> BuildPackageAsync(DateTimeOffset createdAt, string? password, CancellationToken cancellationToken, int? maximumBytes)
    {
        if (_sources.Count == 0)
        {
            throw new InvalidOperationException("No backup sources are registered.");
        }

        Dictionary<string, string>? summary = null;
        foreach (var provider in _summaries)
        {
            summary ??= new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in await provider.GetSummaryAsync(cancellationToken))
            {
                summary[key] = value;
            }
        }

        var package = await BackupPackage.CreateAsync(
            _sources,
            entries => new BackupManifest
            {
                FormatVersion = BackupManifest.CurrentFormatVersion,
                AppId = _app.AppId,
                AppVersion = _app.Version.ToString(),
                CreatedAt = createdAt,
                DeviceName = _app.DeviceName,
                Platform = _app.Platform,
                Entries = entries,
                Summary = summary,
            },
            cancellationToken, maximumBytes);

        if (string.IsNullOrEmpty(password)) return package;
        try { return BackupEncryption.Encrypt(package, password); }
        finally { CryptographicOperations.ZeroMemory(package); }
    }

    private async Task<int> ApplyRetentionCoreAsync(IBackupStorage storage, CancellationToken cancellationToken, string originalAppId)
    {
        if (_options.MaxBackupsToKeep <= 0)
        {
            return 0;
        }

        RequireFileAppId(originalAppId, cancellationToken);
        var files = await storage.ListAsync(cancellationToken);
        RequireFileAppId(originalAppId, cancellationToken);
        var obsolete = files.Select(file => (File: file,
                Parsed: BackupFileName.TryParse(file.FileName, out var appId, out var createdAt), AppId: appId, CreatedAt: createdAt))
            .Where(item => item.Parsed && string.Equals(item.AppId, originalAppId, StringComparison.Ordinal))
            .OrderByDescending(item => item.CreatedAt).Skip(_options.MaxBackupsToKeep).Select(item => item.File).ToList();
        foreach (var file in obsolete)
        {
            RequireFileAppId(originalAppId, cancellationToken);
            await storage.DeleteAsync(file.Id, cancellationToken);
        }

        return obsolete.Count;
    }

    private void Validate(BackupManifest manifest)
    {
        if (!string.Equals(manifest.AppId, _app.AppId, StringComparison.Ordinal))
        {
            throw new BackupException(BackupError.WrongApp, $"This backup belongs to another app ({manifest.AppId}).");
        }

        if (!Version.TryParse(manifest.AppVersion, out var backupVersion))
        {
            throw new BackupException(BackupError.InvalidFormat, "The backup manifest has an invalid app version.");
        }

        if (Normalize(backupVersion) > Normalize(_app.Version))
        {
            throw new BackupException(
                BackupError.CreatedByNewerAppVersion,
                $"This backup was created by version {manifest.AppVersion}; update the app (currently {_app.Version}) to restore it.");
        }
    }

    // "1.0" and "1.0.0" must compare equal; System.Version treats missing components as smaller.
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
}
