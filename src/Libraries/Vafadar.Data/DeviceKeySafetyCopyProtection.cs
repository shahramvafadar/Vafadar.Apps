using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Backup.Security;

namespace Vafadar.Data;

/// <summary>Protects each safety copy with its own durable platform-envelope key bound to the captured profile/installation.</summary>
/// <remarks>Hosts retain referenced keys and reconcile failed publication artifacts. Reads never prepare keys;
/// registration, private-storage restrictions and reference-aware retention belong to the final native host.</remarks>
public sealed class DeviceKeySafetyCopyProtection : IBackupSafetyCopyProtection
{
    private readonly Guid profile;
    private readonly Guid installation;
    private readonly IDatabaseGenerationKeyStore keys;
    private readonly Action validate;

    /// <summary>Captures a known profile/installation and actual durable platform key effects without borrowing its database key.</summary>
    public DeviceKeySafetyCopyProtection(Guid profile, Guid installation, IDatabaseGenerationKeyStore keys, Action validateAccess)
    {
        _ = new DatabaseKeyBinding(profile, installation, Guid.NewGuid());
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(validateAccess);
        this.profile = profile; this.installation = installation; this.keys = keys; validate = validateAccess;
    }

    /// <inheritdoc />
    public async Task<byte[]> ProtectAsync(ReadOnlyMemory<byte> package, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); validate();
        if (package.Length is 0 or > DeviceSafetyEncryption.MaximumPackageBytes) throw new ArgumentException("Safety package exceeds its bound.");
        var binding = new DatabaseKeyBinding(profile, installation, Guid.NewGuid());
        await keys.PrepareAsync(binding, ct).ConfigureAwait(false); validate();
        var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
        try
        {
            ct.ThrowIfCancellationRequested(); validate();
            var identity = new byte[48];
            binding.ProfileId.TryWriteBytes(identity.AsSpan(0, 16), bigEndian: true, out _);
            binding.InstallationId.TryWriteBytes(identity.AsSpan(16, 16), bigEndian: true, out _);
            binding.GenerationId.TryWriteBytes(identity.AsSpan(32, 16), bigEndian: true, out _);
            return DeviceSafetyEncryption.Protect(package.Span, key, identity);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <inheritdoc />
    public async Task<byte[]> OpenAsync(ReadOnlyMemory<byte> envelope, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); validate();
        var identity = DeviceSafetyEncryption.ReadIdentity(envelope.Span);
        var binding = new DatabaseKeyBinding(new Guid(identity.AsSpan(0, 16), bigEndian: true),
            new Guid(identity.AsSpan(16, 16), bigEndian: true), new Guid(identity.AsSpan(32, 16), bigEndian: true));
        if (binding.ProfileId != profile || binding.InstallationId != installation)
            throw new BackupException(BackupError.InvalidFormat, "Safety copy belongs to another local context.");
        var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
        try { ct.ThrowIfCancellationRequested(); validate(); return DeviceSafetyEncryption.Open(envelope.Span, key); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
