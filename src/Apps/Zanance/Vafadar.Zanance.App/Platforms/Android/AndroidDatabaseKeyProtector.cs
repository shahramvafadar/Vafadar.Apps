using Android.Security.Keystore;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Wraps profile DEKs using an application-scoped, non-exportable Android Keystore AES key.</summary>
/// <remarks>Call off the UI thread under the captured migration/access gate. This adapter does not persist
/// envelopes or create replacement keys while unwrapping. App PIN and approved recovery remain separate gates.</remarks>
public static class AndroidDatabaseKeyProtector
{
    private static readonly object CreationGate = new();
    private const int EnvelopeLength = 65; // Versioned header (5), native nonce (12), ciphertext/key (32), GCM tag (16).

    /// <summary>Explicitly prepares one installation wrapping key; never replaces an existing alias.</summary>
    public static void PrepareWrappingKey(Guid installationId)
    {
        lock (CreationGate)
        {
            using var store = OpenStore();
            var alias = GetWrappingKeyAlias(installationId);
            if (!store.ContainsAlias(alias))
            {
                using var generator = KeyGenerator.GetInstance("AES", "AndroidKeyStore")!;
                using var spec = new KeyGenParameterSpec.Builder(alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                    .SetKeySize(256).SetBlockModes(KeyProperties.BlockModeGcm)
                    .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                    .SetRandomizedEncryptionRequired(true).SetUserAuthenticationRequired(false).Build();
                generator.Init(spec);
                using var generated = generator.GenerateKey();
            }
            using var key = RequireKey(store, installationId);
        }
    }

    /// <summary>Protects a 32-byte caller-owned DEK using a native random nonce and authenticated binding.</summary>
    public static byte[] Protect(ReadOnlySpan<byte> key, DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (key.Length != 32) throw new ArgumentException("A data key must contain 256 bits.", nameof(key));
        using var store = OpenStore();
        using var wrappingKey = RequireKey(store, binding.InstallationId);
        using var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, wrappingKey);
        cipher.UpdateAAD(binding.GetAuthenticatedContext());
        var owned = key.ToArray();
        try
        {
            var encrypted = cipher.DoFinal(owned)!;
            var nonce = cipher.GetIV()!;
            if (nonce.Length != 12 || encrypted.Length != 48) throw new CryptographicException("Unsupported native GCM envelope.");
            return [.. "ZKEY"u8, 1, .. nonce, .. encrypted];
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
    }

    /// <summary>Explicitly protects a verified recovered key under a fresh non-exportable alias, retaining old unavailable aliases.</summary>
    public static byte[] ProtectRecovered(ReadOnlySpan<byte> key, DatabaseKeyBinding binding)
        => ProtectIndependent(key, binding, purpose: 1);

    /// <summary>Protects a new generation through its own immutable non-exportable alias, without recreating a lost installation alias.</summary>
    public static byte[] ProtectGeneration(ReadOnlySpan<byte> key, DatabaseKeyBinding binding)
        => ProtectIndependent(key, binding, purpose: 0);

    private static byte[] ProtectIndependent(ReadOnlySpan<byte> key, DatabaseKeyBinding binding, byte purpose)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (key.Length != 32) throw new CryptographicException("Recovery requires the exact 256-bit key.");
        var wrapping = Guid.NewGuid(); PrepareWrappingKey(wrapping);
        using var store = OpenStore(); using var wrappingKey = RequireKey(store, wrapping);
        using var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, wrappingKey);
        var header = new byte[22]; "ZKEY"u8.CopyTo(header); header[4] = 2; header[5] = purpose;
        wrapping.TryWriteBytes(header.AsSpan(6, 16), bigEndian: true, out _);
        cipher.UpdateAAD([.. binding.GetAuthenticatedContext(), .. header]);
        var owned = key.ToArray();
        try
        {
            var encrypted = cipher.DoFinal(owned)!; var nonce = cipher.GetIV()!;
            if (nonce.Length != 12 || encrypted.Length != 48) throw new CryptographicException("Unsupported recovery GCM envelope.");
            return [.. header, .. nonce, .. encrypted];
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
    }

    /// <summary>Authenticates an explicit recovered entry through its captured fresh alias, never the old installation alias.</summary>
    public static byte[] UnprotectRecovered(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding)
        => UnprotectIndependent(envelope, binding, purpose: 1);

    private static byte[] UnprotectIndependent(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding, byte purpose)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (envelope.Length != 82 || !envelope[..4].SequenceEqual("ZKEY"u8) || envelope[4] != 2 || envelope[5] != purpose)
            throw new CryptographicException("Unsupported recovery envelope.");
        var wrapping = new Guid(envelope.Slice(6, 16), bigEndian: true);
        if (wrapping == Guid.Empty) throw new CryptographicException("Recovery wrapping identity is missing.");
        using var store = OpenStore(); using var key = RequireKey(store, wrapping);
        using var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        using var parameters = new GCMParameterSpec(128, envelope.Slice(22, 12).ToArray());
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, key, parameters);
        cipher.UpdateAAD([.. binding.GetAuthenticatedContext(), .. envelope[..22].ToArray()]);
        byte[] restored;
        try { restored = cipher.DoFinal(envelope[34..].ToArray())!; }
        catch (AEADBadTagException) { throw new CryptographicException("Recovery envelope authentication failed."); }
        if (restored.Length == 32) return restored;
        CryptographicOperations.ZeroMemory(restored); throw new CryptographicException("Recovery envelope contains no data key.");
    }

    /// <summary>Unwraps only the bound DEK; malformed data or a missing key cannot create a new alias.</summary>
    public static byte[] Unprotect(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (envelope.Length == 82) return UnprotectIndependent(envelope, binding, purpose: 0);
        // Existing installation-bound version 1 entries remain readable. Neither format creates aliases.

        if (envelope.Length != EnvelopeLength || !envelope[..4].SequenceEqual("ZKEY"u8) || envelope[4] != 1)
            throw new CryptographicException("Unsupported platform key envelope.");
        using var store = OpenStore();
        using var wrappingKey = RequireKey(store, binding.InstallationId);
        using var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        using var parameters = new GCMParameterSpec(128, envelope.Slice(5, 12).ToArray());
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, wrappingKey, parameters);
        cipher.UpdateAAD(binding.GetAuthenticatedContext());
        byte[] restored;
        try { restored = cipher.DoFinal(envelope[17..].ToArray())!; }
        catch (AEADBadTagException) { throw new CryptographicException("Platform key envelope authentication failed."); }
        if (restored.Length == 32) return restored;
        CryptographicOperations.ZeroMemory(restored);
        throw new CryptographicException("Platform envelope does not contain a data key.");
    }

    /// <summary>Reports the OS security level when available; emulator values do not establish physical hardware protection.</summary>
    public static int? ReadSecurityLevel(Guid installationId)
    {
        using var store = OpenStore();
        using var key = RequireKey(store, installationId);
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return null;
        using var factory = SecretKeyFactory.GetInstance("AES", "AndroidKeyStore")!;
        using var type = Java.Lang.Class.FromType(typeof(KeyInfo));
        using var info = (KeyInfo)factory.GetKeySpec(key, type)!;
        return (int)info.SecurityLevel;
    }

    /// <summary>Names only the adapter-owned installation alias; used by independent fixture cleanup.</summary>
    internal static string GetWrappingKeyAlias(Guid installationId)
    {
        if (installationId == Guid.Empty) throw new ArgumentException("A wrapping key requires installation identity.");
        return "pro.vafadar.zanance.database-wrap.v1." + installationId.ToString("N");
    }

    private static Java.Security.KeyStore OpenStore()
    {
        var store = Java.Security.KeyStore.GetInstance("AndroidKeyStore")!;
        try { store.Load(null); return store; }
        catch { store.Dispose(); throw; }
    }

    private static ISecretKey RequireKey(Java.Security.KeyStore store, Guid installationId)
    {
        using var entry = store.GetEntry(GetWrappingKeyAlias(installationId), null);
        if (entry is not Java.Security.KeyStore.SecretKeyEntry secret || secret.SecretKey is not { } key)
            throw new CryptographicException("The installation wrapping key is unavailable.");
        var encoded = key.GetEncoded();
        if (encoded is not null)
        {
            CryptographicOperations.ZeroMemory(encoded);
            key.Dispose();
            throw new CryptographicException("An exportable wrapping key is not accepted.");
        }
        return key;
    }
}
