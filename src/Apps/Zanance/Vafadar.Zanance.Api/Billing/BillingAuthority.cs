using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Independent private service receipt signer; missing existing authority is never silently replaced.</summary>
internal sealed class BillingAuthority(ServiceStorage storage, IDataProtectionProvider protection,
    IDbContextFactory<ServiceDbContext> factory)
{
    /// <summary>Fixed JWS purpose, distinct from every household, identity and transport message.</summary>
    internal const string SnapshotPurpose = "zanance.billing.snapshot.v1";
    /// <summary>Fixed original registration proof purpose.</summary>
    internal const string RegistrationPurpose = "zanance.billing.registration.v1";
    /// <summary>Fixed original device operation purpose.</summary>
    internal const string CommandPurpose = "zanance.billing.command.v1";
    /// <summary>Clock proofs are nonce-bound and carry no purchase authority.</summary>
    internal const string TimePurpose = "zanance.billing.time.v1";

    /// <summary>Fixed signed operation completion purpose; it never grants current cache authority.</summary>
    internal const string ReceiptPurpose = "zanance.billing.receipt.v1";

    /// <summary>Authenticates exact original finish/outcome flags separately from replayable purchase snapshots.</summary>
    internal static BillingCommandResult CommandResult(BillingSnapshot snapshot, SigningRecipientIdentity signer,
        string original, bool mayFinish, BillingPurchaseState? state, Guid? nativeIntent = null, BillingProduct? completedProduct = null)
    {
        string? receipt = null;
        if (snapshot.Kind == BillingSnapshotKind.Operation)
        {
            var facts = new BillingOperationReceipt(snapshot.CustomerId, snapshot.RequestId, BillingAccountBinding.CommandHash(original),
                snapshot.DeviceFingerprint, snapshot.Distribution, snapshot.Environment, snapshot.IdentityHash, mayFinish, state, snapshot.ServerTime, nativeIntent, completedProduct);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(facts, BillingJson.Default.BillingOperationReceipt);
            try { receipt = StandardJoseSignatures.Sign(bytes, signer, ReceiptPurpose); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        return new(snapshot.RequestId, Sign(snapshot, signer), mayFinish, state, receipt);
    }

    /// <summary>Opens an immutable protected signer under a process-independent private file lock.</summary>
    internal async Task<SigningRecipientIdentity> OpenAsync(CancellationToken ct)
    {
        var file = Path.Combine(storage.Root, "billing-authority.protected");
        var pending = file + ".pending";
        var protector = protection.CreateProtector("zanance-billing-authority-v1");
        try
        {
            await using var guard = new FileStream(file + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (!File.Exists(file))
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                if (await db.Set<BillingCustomerRow>().AnyAsync(ct))
                    throw new BillingRefusal(503, "OriginalBillingAuthorityUnavailable");
                if (!File.Exists(pending))
                {
                    using var candidate = SigningRecipientIdentity.Create();
                    var clear = candidate.ExportForProtectedStorage();
                    byte[]? encrypted = null;
                    try
                    {
                        encrypted = protector.Protect(clear);
                        await using var stage = new FileStream(pending, new FileStreamOptions
                        { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.WriteThrough });
                        await stage.WriteAsync(encrypted, ct); stage.Flush(flushToDisk: true);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(clear);
                        if (encrypted is not null) CryptographicOperations.ZeroMemory(encrypted);
                    }
                }
                // Validate the retained candidate before publishing. A truncated pending key stays closed for recovery.
                using (await ReadAsync(pending, protector, ct)) { }
                File.Move(pending, file, overwrite: false);
            }
            return await ReadAsync(file, protector, ct);
        }
        catch (BillingRefusal) { throw; }
        catch (Exception error) when (error is IOException or CryptographicException or UnauthorizedAccessException)
        { throw new BillingRefusal(503, "OriginalBillingAuthorityUnavailable"); }
    }

    /// <summary>Signs a complete device-bound state only after the transaction's accepted purchase facts are known.</summary>
    internal static string Sign(BillingSnapshot snapshot, SigningRecipientIdentity signer)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, BillingJson.Default.BillingSnapshot);
        try { return StandardJoseSignatures.Sign(bytes, signer, SnapshotPurpose); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Signs public time only for the caller's original fresh nonce, without reading a purchase or identity.</summary>
    internal static string SignTime(BillingTimeProof proof, SigningRecipientIdentity signer)
    {
        if (proof.RequestId == Guid.Empty) throw new BillingRefusal(422, "BillingTimeNonceRequired");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(proof, BillingJson.Default.BillingTimeProof);
        try { return StandardJoseSignatures.Sign(bytes, signer, TimePurpose); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Public identity serialization has no secret key material.</summary>
    internal static string Public(SigningRecipientIdentity signer) => JsonSerializer.Serialize(signer.Public, BillingProviderJson.Default.PublicSigningRecipientIdentity);

    /// <summary>Strict bounded original public key, never an arbitrary key resolver or a household signing authority.</summary>
    internal static PublicSigningRecipientIdentity ReadPublic(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > 16384) throw new BillingRefusal(422, "BillingDeviceProofRefused");
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            var key = StrictJsonPayload.Deserialize(bytes, BillingProviderJson.Default.PublicSigningRecipientIdentity, 16384);
            using var signing = key.OpenSigningKey(); using var wrapping = key.OpenWrappingKey();
            return key;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>Protocol/domain-separated commitments never expose a provider reference or raw identity in keys/logs.</summary>
    internal static string Hash(string purpose, string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(purpose + "\0" + value)));
    /// <summary>Shared public account binding, not a credential or an email address.</summary>
    internal static string IdentityHash(string user) => BillingAccountBinding.Identity(user);
    /// <summary>Google's obfuscated account id is an irreversible opaque binding, independent of local profiles.</summary>
    internal static string GoogleAccount(Guid account, bool identity = false) => BillingAccountBinding.Google(account, identity);
    /// <summary>Global provider purchase uniqueness includes issuer and sandbox/production.</summary>
    internal static string PurchaseId(BillingIssuer issuer, BillingEnvironment environment, string reference)
        => BillingAccountBinding.PurchaseId(issuer, environment, reference);

    /// <summary>Reads only finite original private authority; retained symlinks or corrupt bytes fail closed.</summary>
    private static async Task<SigningRecipientIdentity> ReadAsync(string path, IDataProtector protector, CancellationToken ct)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length is < 1 or > 65536 || (info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new BillingRefusal(503, "OriginalBillingAuthorityUnavailable");
        var encrypted = await File.ReadAllBytesAsync(path, ct); byte[]? clear = null;
        try { clear = protector.Unprotect(encrypted); return SigningRecipientIdentity.ImportFromProtectedStorage(clear); }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
            if (clear is not null) CryptographicOperations.ZeroMemory(clear);
        }
    }
}
