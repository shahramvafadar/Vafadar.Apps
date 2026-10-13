using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Seals 256-bit data keys using Windows CurrentUser DPAPI and authenticated binding context.</summary>
/// <remarks>Does not read existing credentials or persist files. A caller owns envelope ACLs, durable storage,
/// access gates and recovery. CurrentUser protection is not hardware-backed or a defence against same-user code.</remarks>
[SupportedOSPlatform("windows")]
public static class WindowsDatabaseKeyProtector
{
    /// <summary>Maximum accepted opaque DPAPI envelope length; oversized input is refused before native processing.</summary>
    public const int MaximumEnvelopeBytes = 4096;

    /// <summary>Protects a caller-owned 32-byte DEK; clears its owned temporary key copy on every outcome.</summary>
    public static byte[] Protect(ReadOnlySpan<byte> key, DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (key.Length != 32) throw new ArgumentException("A data key must contain 256 bits.", nameof(key));
        var owned = key.ToArray();
        try
        {
            var envelope = ProtectedData.Protect(owned, binding.GetAuthenticatedContext(), DataProtectionScope.CurrentUser);
            if (envelope.Length == 0 || envelope.Length > MaximumEnvelopeBytes)
                throw new CryptographicException("Platform key envelope size is unsupported.");
            return envelope;
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
    }

    /// <summary>Unseals only the captured binding's 32-byte key; missing, modified or foreign envelopes never generate a replacement.</summary>
    public static byte[] Unprotect(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (envelope.Length == 0 || envelope.Length > MaximumEnvelopeBytes)
            throw new CryptographicException("Platform key envelope size is unsupported.");
        var restored = ProtectedData.Unprotect(envelope.ToArray(), binding.GetAuthenticatedContext(), DataProtectionScope.CurrentUser);
        if (restored.Length == 32) return restored; // Caller owns and must clear this successful key buffer.
        CryptographicOperations.ZeroMemory(restored);
        throw new CryptographicException("Platform envelope does not contain a data key.");
    }
}
