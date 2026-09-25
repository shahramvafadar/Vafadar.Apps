using System.Globalization;
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
/// Only one backup or restore runs at a time.
/// </remarks>
public sealed class BackupService : IBackupService
{
    internal const string LastBackupKey = "backup.lastBackupAt";

    private readonly IReadOnlyList<IBackupSource> _sources;
    private readonly IAppEnvironment _app;
    private readonly ISettingsStore _settings;
    private readonly TimeProvider _time;
    private readonly BackupOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the service.</summary>
    public BackupService(
        IEnumerable<IBackupSource> sources,
        IAppEnvironment app,
        ISettingsStore settings,
        TimeProvider time,
        BackupOptions options)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(options);

        _sources = [.. sources];
        _app = app;
        _settings = settings;
        _time = time;
        _options = options;
    }

    /// <inheritdoc />
    public DateTimeOffset? LastBackupAt =>
        DateTimeOffset.TryParse(_settings.Get(LastBackupKey), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;

    /// <inheritdoc />
    public bool IsAutomaticBackupDue() =>
        _options.AutomaticBackupInterval is { } interval
        && (LastBackupAt is not { } last || _time.GetUtcNow() - last >= interval);

    /// <inheritdoc />
    public async Task<BackupFileInfo> CreateBackupAsync(IBackupStorage storage, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var createdAt = _time.GetUtcNow();
            var package = await CreatePackageCoreAsync(createdAt, password, cancellationToken);

            BackupFileInfo file;
            using (var content = new MemoryStream(package, writable: false))
            {
                file = await storage.UploadAsync(BackupFileName.Create(_app.AppId, createdAt), content, cancellationToken);
            }

            _settings.Set(LastBackupKey, createdAt.ToString("O", CultureInfo.InvariantCulture));
            await ApplyRetentionCoreAsync(storage, cancellationToken);
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
    {
        ArgumentNullException.ThrowIfNull(storage);

        var files = await storage.ListAsync(cancellationToken);
        return
        [
            .. files
                .Select(file => (File: file, Parsed: BackupFileName.TryParse(file.FileName, out var appId, out var createdAt), AppId: appId, CreatedAt: createdAt))
                .Where(x => x.Parsed && string.Equals(x.AppId, _app.AppId, StringComparison.Ordinal))
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
    public async Task<BackupManifest> RestorePackageAsync(byte[] package, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (BackupEncryption.IsEncrypted(package))
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new BackupException(BackupError.PasswordRequired, "This backup is protected with a password.");
                }

                package = BackupEncryption.Decrypt(package, password);
            }

            var (manifest, entries) = await BackupPackage.ReadAsync(package, cancellationToken);
            Validate(manifest);

            // Everything is validated; only now is existing data replaced. Sources without an entry (e.g. added in a
            // later app version than the backup) keep their current data.
            foreach (var source in _sources)
            {
                if (entries.TryGetValue(source.Name, out var content))
                {
                    using var stream = new MemoryStream(content, writable: false);
                    await source.RestoreAsync(stream, cancellationToken);
                }
            }

            return manifest;
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

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ApplyRetentionCoreAsync(storage, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<byte[]> CreatePackageCoreAsync(DateTimeOffset createdAt, string? password, CancellationToken cancellationToken)
    {
        if (_sources.Count == 0)
        {
            throw new InvalidOperationException("No backup sources are registered.");
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
            },
            cancellationToken);

        return string.IsNullOrEmpty(password) ? package : BackupEncryption.Encrypt(package, password);
    }

    private async Task<int> ApplyRetentionCoreAsync(IBackupStorage storage, CancellationToken cancellationToken)
    {
        if (_options.MaxBackupsToKeep <= 0)
        {
            return 0;
        }

        var obsolete = (await ListBackupsAsync(storage, cancellationToken)).Skip(_options.MaxBackupsToKeep).ToList();
        foreach (var file in obsolete)
        {
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
