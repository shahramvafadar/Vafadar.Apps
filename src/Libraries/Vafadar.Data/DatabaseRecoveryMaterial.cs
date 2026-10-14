using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Optional installation recovery identity; the public key alone can protect newly prepared generation keys.</summary>
public sealed class DatabaseRecoveryIdentity
{
    private readonly byte[] publicKey;

    /// <summary>Captures canonical RSA-3072 public material and complete non-secret local recovery identity.</summary>
    public DatabaseRecoveryIdentity(Guid installationId, Guid recoveryId, ReadOnlySpan<byte> publicKey)
    {
        if (installationId == Guid.Empty || recoveryId == Guid.Empty) throw new ArgumentException("Recovery identity is incomplete.");
        if (publicKey.Length is <= 0 or > 1024) throw new CryptographicException("Recovery public material exceeds its bound.");
        using var rsa = RSA.Create(); rsa.ImportSubjectPublicKeyInfo(publicKey, out var read);
        if (read != publicKey.Length || rsa.KeySize != 3072 || publicKey.Length > 1024)
            throw new CryptographicException("Recovery requires exact RSA-3072 public material.");
        this.publicKey = rsa.ExportSubjectPublicKeyInfo();
        if (!publicKey.SequenceEqual(this.publicKey)) throw new CryptographicException("Recovery public material is not canonical.");
        InstallationId = installationId; RecoveryId = recoveryId;
    }
    /// <summary>The existing installation, independent from profile display names.</summary>
    public Guid InstallationId { get; }
    /// <summary>The explicitly enabled recovery generation, distinct from a database generation.</summary>
    public Guid RecoveryId { get; }
    /// <summary>Returns an owned non-secret public-key copy; no private material is exposed.</summary>
    public byte[] ExportPublicKey() => (byte[])publicKey.Clone();

    /// <summary>Wraps an exact generation key with standard RSA-OAEP-SHA256, authenticating its existing context inside the payload.</summary>
    public byte[] Protect(ReadOnlySpan<byte> key, DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (key.Length != 32 || binding.InstallationId != InstallationId) throw new CryptographicException("Recovery key context differs.");
        using var rsa = RSA.Create(); rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        var context = binding.GetAuthenticatedContext();
        var payload = new byte[16 + context.Length + 32];
        try
        {
            RecoveryId.TryWriteBytes(payload.AsSpan(0, 16), bigEndian: true, out _);
            context.CopyTo(payload, 16); key.CopyTo(payload.AsSpan(16 + context.Length));
            return rsa.Encrypt(payload, RSAEncryptionPadding.OaepSHA256);
        }
        finally { CryptographicOperations.ZeroMemory(payload); }
    }

    /// <summary>Confirms stored ciphertext using the actual DEK and standard HMAC-SHA256 without unlocking private recovery material.</summary>
    internal byte[] Confirm(ReadOnlySpan<byte> key, DatabaseKeyBinding binding, ReadOnlySpan<byte> envelope)
    {
        if (key.Length != 32 || envelope.Length != 384 || binding.InstallationId != InstallationId)
            throw new CryptographicException("Recovery confirmation context differs.");
        var context = binding.GetAuthenticatedContext(); var packet = new byte[16 + context.Length + envelope.Length];
        RecoveryId.TryWriteBytes(packet.AsSpan(0, 16), bigEndian: true, out _);
        context.CopyTo(packet, 16); envelope.CopyTo(packet.AsSpan(16 + context.Length));
        return HMACSHA256.HashData(key, packet);
    }
}

/// <summary>Owned unlocked recovery private material; callers dispose it immediately after explicit recovery/provisioning.</summary>
/// <remarks>RSA generation/encryption/decryption use the platform cryptography implementation. No custom cipher or KDF
/// is introduced. Private exports are owned buffers for the encrypted vault writer and must be cleared by their caller.</remarks>
public sealed class DatabaseRecoveryMaterial : IDisposable
{
    private readonly RSA rsa;
    private bool disposed;
    private DatabaseRecoveryMaterial(RSA rsa, DatabaseRecoveryIdentity identity) { this.rsa = rsa; Identity = identity; }
    /// <summary>The non-secret identity needed to wrap future keys without unlocking private material.</summary>
    public DatabaseRecoveryIdentity Identity { get; }

    /// <summary>Creates a new optional identity only after the host has explicit enablement authority.</summary>
    public static DatabaseRecoveryMaterial Create(Guid installationId)
    {
        if (installationId == Guid.Empty) throw new ArgumentException("Recovery requires the existing installation.");
        var rsa = RSA.Create(3072);
        try { return new(rsa, new(installationId, Guid.NewGuid(), rsa.ExportSubjectPublicKeyInfo())); }
        catch { rsa.Dispose(); throw; }
    }

    /// <summary>Imports the exact private material read from an authenticated encrypted vault.</summary>
    internal static DatabaseRecoveryMaterial Import(DatabaseRecoveryIdentity identity, ReadOnlySpan<byte> privateKey)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(privateKey, out var read);
            if (read != privateKey.Length || rsa.KeySize != 3072 || !rsa.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(identity.ExportPublicKey()))
                throw new CryptographicException("Recovery private material differs from its public identity.");
            return new(rsa, identity);
        }
        catch { rsa.Dispose(); throw; }
    }

    /// <summary>Returns private PKCS#8 only for the encrypted vault writer; caller clears the owned buffer.</summary>
    internal byte[] ExportPrivateKey() { ObjectDisposedException.ThrowIf(disposed, this); return rsa.ExportPkcs8PrivateKey(); }

    /// <summary>Unwraps only an exact authenticated generation; returns an owned 32-byte key that the caller clears.</summary>
    public byte[] Open(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(binding);
        if (envelope.Length != 384 || binding.InstallationId != Identity.InstallationId)
            throw new CryptographicException("Recovery envelope context differs.");
        var payload = rsa.Decrypt(envelope, RSAEncryptionPadding.OaepSHA256);
        var context = binding.GetAuthenticatedContext(); var recoveryId = new byte[16];
        Identity.RecoveryId.TryWriteBytes(recoveryId, bigEndian: true, out _);
        try
        {
            if (payload.Length != 16 + context.Length + 32
                || !CryptographicOperations.FixedTimeEquals(payload.AsSpan(0, 16), recoveryId)
                || !CryptographicOperations.FixedTimeEquals(payload.AsSpan(16, context.Length), context))
                throw new CryptographicException("Recovery envelope authentication differs.");
            return payload.AsSpan(16 + context.Length).ToArray();
        }
        finally { CryptographicOperations.ZeroMemory(payload); }
    }

    /// <summary>Unwraps and validates the recorded standard HMAC confirmation before returning any usable key.</summary>
    public byte[] OpenConfirmed(ReadOnlySpan<byte> envelope, ReadOnlySpan<byte> confirmation, DatabaseKeyBinding binding)
    {
        var key = Open(envelope, binding);
        try
        {
            if (confirmation.Length != 32 || !CryptographicOperations.FixedTimeEquals(Identity.Confirm(key, binding, envelope), confirmation))
                throw new CryptographicException("Recovery key confirmation differs.");
            return key;
        }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }
    /// <summary>Disposes native private material; a retired owner cannot unwrap keys.</summary>
    public void Dispose() { if (disposed) return; disposed = true; rsa.Dispose(); }
}
