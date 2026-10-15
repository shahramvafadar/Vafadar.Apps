using System.Globalization;
using System.Security.Cryptography;
using Vafadar.Backup.Security;

namespace Vafadar.Backup;

public sealed partial class BackupService
{
    private const string LastAutomaticKey = "backup.lastAutomaticAt";

    /// <inheritdoc />
    public DateTimeOffset? LastAutomaticBackupAt => ReadAutomaticTime(_options.FileSet?.Invoke());

    private static string AutomaticKey(string? set) => string.IsNullOrEmpty(set) ? LastAutomaticKey : LastAutomaticKey + "." + set;

    private DateTimeOffset? ReadAutomaticTime(string? set) =>
        DateTimeOffset.TryParse(_settings.Get(AutomaticKey(set)), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var value) ? value : null;

    /// <inheritdoc />
    public async Task<BackupFileInfo?> CreateWhenDueAsync(IBackupStorage storage, TimeSpan? interval, Action validate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storage); ArgumentNullException.ThrowIfNull(validate);
        if (interval is null) return null;
        if (storage is not Storage.LocalFolderBackupStorage)
            throw new ArgumentException("Automatic copies require device-local storage.", nameof(storage));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var set = _options.FileSet?.Invoke();
            void Check()
            {
                ct.ThrowIfCancellationRequested(); validate();
                if (!string.Equals(set, _options.FileSet?.Invoke(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Automatic backup belongs to a different profile set.");
            }
            Check();
            var now = _time.GetUtcNow(); var last = ReadAutomaticTime(set);
            if (last is { } previous && now >= previous && now - previous < interval) return null;
            var protection = _automaticProtection
                ?? throw new InvalidOperationException("Automatic backup requires device protection; plaintext fallback is refused.");
            // Validate the set through the same filename rules as portable backups, without exposing data in a name.
            _ = FileAppId();
            var name = AutomaticBackupFileName.Prefix(set) + Guid.NewGuid().ToString("N") + ".vbs";
            var clear = await CreatePackageCoreAsync(now, null, ct, DeviceSafetyEncryption.MaximumPackageBytes).ConfigureAwait(false);
            byte[]? encrypted = null;
            try
            {
                Check();
                encrypted = await protection.ProtectAutomaticAsync(clear, Check, ct).ConfigureAwait(false);
                if (!DeviceSafetyEncryption.IsEncrypted(encrypted))
                    throw new IOException("Automatic protection did not produce a device-bound envelope.");
                Check();
                using var content = new MemoryStream(encrypted, writable: false);
                var file = await storage.UploadAsync(name, content, ct).ConfigureAwait(false);
                if (file.FileName != name || file.Id != name) throw new IOException("Automatic local upload identity differs.");
                Check();
                await protection.CompleteAutomaticAsync(file, Check, ct).ConfigureAwait(false);
                Check();
                _settings.Set(AutomaticKey(set), now.ToString("O", CultureInfo.InvariantCulture));
                return file;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clear);
                if (encrypted is not null) CryptographicOperations.ZeroMemory(encrypted);
            }
        }
        finally { _gate.Release(); }
    }
}
