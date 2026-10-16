using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Owned client-only epoch/address keys; neither private byte array participates in service metadata serialization.</summary>
public sealed class HouseholdEpochMaterial : IDisposable
{
    private readonly byte[] epochKey;
    private readonly byte[] addressKey;
    private bool disposed;

    private HouseholdEpochMaterial(long epoch, byte[] encryption, byte[] addresses)
    { Epoch = epoch; epochKey = encryption; addressKey = addresses; }

    /// <summary>Exact approved encryption generation, never a date or local account number.</summary>
    public long Epoch { get; }
    /// <summary>Public commitment checked independently after opening each approved recipient grant.</summary>
    public string KeyCommitment { get { ThrowIfDisposed(); return Commitment(epochKey, "Zanance household epoch commitment v1\0"u8); } }
    /// <summary>Stable public commitment prevents an owner/server from silently changing opaque record addressing across generations.</summary>
    public string AddressKeyCommitment { get { ThrowIfDisposed(); return Commitment(addressKey, "Zanance household address commitment v1\0"u8); } }

    /// <summary>Creates independent encryption/address keys for one newly reviewed household, never from an account password or invitation capability.</summary>
    public static HouseholdEpochMaterial Create()
        => new(1, RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32));

    /// <summary>Rotates encryption while preserving independently keyed record addresses; the new key never goes to removed recipients.</summary>
    public HouseholdEpochMaterial Next()
    {
        ThrowIfDisposed();
        if (Epoch == long.MaxValue) throw new CryptographicException("Household generation bound reached.");
        return new(Epoch + 1, RandomNumberGenerator.GetBytes(32), addressKey.ToArray());
    }

    /// <summary>Hides table/key guesses using standard HMAC-SHA256 over length-delimited exact identity components.</summary>
    public string RecordAddress(string table, string key)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(table) || table.Length > 128 || string.IsNullOrEmpty(key) || key.Length > 1024)
            throw new ArgumentException("Bounded household record identity required.");
        var tableBytes = Encoding.UTF8.GetBytes(table); var keyBytes = Encoding.UTF8.GetBytes(key);
        var buffer = new byte[8 + tableBytes.Length + keyBytes.Length];
        try
        {
            BinaryPrimitives.WriteInt32BigEndian(buffer, tableBytes.Length); tableBytes.CopyTo(buffer, 4);
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(4 + tableBytes.Length), keyBytes.Length); keyBytes.CopyTo(buffer, 8 + tableBytes.Length);
            using var hash = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, addressKey);
            hash.AppendData("Zanance household record address v1\0"u8); hash.AppendData(buffer);
            return Convert.ToHexStringLower(hash.GetHashAndReset());
        }
        finally { CryptographicOperations.ZeroMemory(tableBytes); CryptographicOperations.ZeroMemory(keyBytes); CryptographicOperations.ZeroMemory(buffer); }
    }

    /// <summary>Seals one signed financial/blob piece for the exact current approved epoch and context.</summary>
    public string Encrypt(HouseholdCryptography cryptography, HouseholdProtectionContext context, ReadOnlySpan<byte> clear,
        SigningRecipientIdentity author)
    {
        ThrowIfDisposed(); RequireEpoch(context);
        return cryptography.EncryptForMembers(context, clear, author, epochKey);
    }

    /// <summary>Opens one piece only after the caller has established the exact trusted signing device and publication authority.</summary>
    public byte[] Decrypt(HouseholdCryptography cryptography, HouseholdProtectionContext context, string ciphertext,
        PublicSigningRecipientIdentity trustedAuthor)
    {
        ThrowIfDisposed(); RequireEpoch(context);
        return cryptography.DecryptForMembers(context, ciphertext, trustedAuthor, epochKey);
    }

    /// <summary>Wraps this generation only for one explicitly approved device, bound to the original signed membership operation.</summary>
    public string Grant(HouseholdCryptography cryptography, HouseholdMembershipStatement membership,
        SigningRecipientIdentity root, HouseholdDeviceDefinition device)
        => GrantHistorical(cryptography, membership, membership, root, device);

    /// <summary>Rewraps one original historical epoch for a newly approved current recipient; the old epoch commitment stays independently verifiable.</summary>
    public string GrantHistorical(HouseholdCryptography cryptography, HouseholdMembershipStatement currentMembership,
        HouseholdMembershipStatement epochOrigin, SigningRecipientIdentity currentRoot, HouseholdDeviceDefinition device)
    {
        ThrowIfDisposed();
        if (currentMembership.SpaceId != epochOrigin.SpaceId || epochOrigin.KeyEpoch != Epoch || currentMembership.KeyEpoch < Epoch ||
            currentMembership.Version < epochOrigin.Version || epochOrigin.KeyCommitment != KeyCommitment ||
            epochOrigin.AddressKeyCommitment != AddressKeyCommitment || currentMembership.AddressKeyCommitment != AddressKeyCommitment ||
            !currentMembership.Members.SelectMany(member => member.Devices).Contains(device) ||
            currentMembership.OwnerKeys != new HouseholdPublicKeys(currentRoot.Public.SigningKey, currentRoot.Public.WrappingKey))
            throw new CryptographicException("Household grant authority was refused.");
        var recipient = new PublicSigningRecipientIdentity(device.Keys.SigningKey, device.Keys.WrappingKey);
        var clear = ExportForProtectedStorage();
        try { return cryptography.EncryptForDevice(new(currentMembership.SpaceId, Epoch, currentMembership.OperationId,
            HouseholdProtectionPurpose.DeviceGrant, recipient.Fingerprint), clear, currentRoot, recipient); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Opens a grant against already verified membership and confirms both public key commitments before returning any usable material.</summary>
    public static HouseholdEpochMaterial OpenGrant(HouseholdCryptography cryptography, HouseholdMembershipStatement trustedMembership,
        string ciphertext, Guid expectedDeviceId, SigningRecipientIdentity recipient)
        => OpenHistoricalGrant(cryptography, trustedMembership, trustedMembership, ciphertext, expectedDeviceId, recipient);

    /// <summary>Opens an explicitly current-authorized historical key and checks its original signed commitment; current keys cannot impersonate another epoch.</summary>
    public static HouseholdEpochMaterial OpenHistoricalGrant(HouseholdCryptography cryptography, HouseholdMembershipStatement trustedGrantMembership,
        HouseholdMembershipStatement trustedEpochOrigin, string ciphertext, Guid expectedDeviceId, SigningRecipientIdentity recipient)
    {
        var device = trustedGrantMembership.Members.SelectMany(member => member.Devices).SingleOrDefault(device => device.Id == expectedDeviceId);
        if (device is null || device.Keys != new HouseholdPublicKeys(recipient.Public.SigningKey, recipient.Public.WrappingKey) ||
            trustedGrantMembership.SpaceId != trustedEpochOrigin.SpaceId || trustedGrantMembership.KeyEpoch < trustedEpochOrigin.KeyEpoch ||
            trustedGrantMembership.Version < trustedEpochOrigin.Version || trustedGrantMembership.AddressKeyCommitment != trustedEpochOrigin.AddressKeyCommitment)
            throw new CryptographicException("Household grant authority was refused.");
        var author = new PublicSigningRecipientIdentity(trustedGrantMembership.OwnerKeys.SigningKey, trustedGrantMembership.OwnerKeys.WrappingKey);
        var clear = cryptography.DecryptForDevice(new(trustedGrantMembership.SpaceId, trustedEpochOrigin.KeyEpoch,
            trustedGrantMembership.OperationId, HouseholdProtectionPurpose.DeviceGrant, recipient.Public.Fingerprint), ciphertext, author, recipient);
        try
        {
            var result = ImportFromProtectedStorage(clear);
            if (result.Epoch != trustedEpochOrigin.KeyEpoch || result.KeyCommitment != trustedEpochOrigin.KeyCommitment ||
                result.AddressKeyCommitment != trustedEpochOrigin.AddressKeyCommitment)
            { result.Dispose(); throw new CryptographicException("Household grant commitments were refused."); }
            return result;
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Bounded private frame for existing protected device storage or standard recipient encryption only, never service JSON/logs.</summary>
    public byte[] ExportForProtectedStorage()
    {
        ThrowIfDisposed();
        var clear = new byte[76]; "ZHE1"u8.CopyTo(clear); BinaryPrimitives.WriteInt64BigEndian(clear.AsSpan(4), Epoch);
        epochKey.CopyTo(clear, 12); addressKey.CopyTo(clear, 44); return clear;
    }

    /// <summary>Imports only exact protected material with version/size/generation checks; persistence protection is a separate mandatory boundary.</summary>
    public static HouseholdEpochMaterial ImportFromProtectedStorage(ReadOnlySpan<byte> clear)
    {
        if (clear.Length != 76 || !clear[..4].SequenceEqual("ZHE1"u8) || BinaryPrimitives.ReadInt64BigEndian(clear[4..]) < 1)
            throw new CryptographicException("Household private material was refused.");
        return new(BinaryPrimitives.ReadInt64BigEndian(clear[4..]), clear.Slice(12, 32).ToArray(), clear.Slice(44, 32).ToArray());
    }

    /// <summary>Disposes only the owned in-memory key copies, without claiming erasure of prior device/exported copies.</summary>
    public void Dispose()
    {
        if (disposed) return;
        CryptographicOperations.ZeroMemory(epochKey); CryptographicOperations.ZeroMemory(addressKey); disposed = true;
    }

    /// <summary>No encryption/decryption can silently select a different generation from incoming metadata.</summary>
    private void RequireEpoch(HouseholdProtectionContext context)
    { if (context.Epoch != Epoch) throw new CryptographicException("Household epoch mismatch."); }
    /// <summary>Standard domain-separated digest of independent random key material, not a password verifier.</summary>
    private static string Commitment(byte[] key, ReadOnlySpan<byte> domain)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); hash.AppendData(domain); hash.AppendData(key);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    /// <summary>Disposed material cannot be reused as an all-zero key.</summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
