using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Backup.Security;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Explicit off-device kit, distinct from identity credentials and the local database's own recovery policy.</summary>
public sealed class HouseholdRecoveryKit : IDisposable
{
    private readonly SigningRecipientIdentity identity;
    private bool disposed;
    private HouseholdRecoveryKit(Guid space, Guid enrollment, string owner, LocalSyncPeer peer,
        HouseholdPublicKeys anchor, SigningRecipientIdentity secret)
    { SpaceId = space; EnrollmentId = enrollment; OwnerId = owner; Peer = peer; InitialOwnerAnchor = anchor; identity = secret; }
    /// <summary>Exact original household, never selected from an untrusted service directory.</summary>
    public Guid SpaceId { get; }
    /// <summary>Exact independent recovery enrollment.</summary>
    public Guid EnrollmentId { get; }
    /// <summary>Original owning identity; another signed-in identity cannot silently adopt this kit.</summary>
    public string OwnerId { get; }
    /// <summary>Original verified public service origin/pin, protected inside the exported kit.</summary>
    public LocalSyncPeer Peer { get; }
    /// <summary>Independent original proposed owner root, retained through future signed root transitions.</summary>
    public HouseholdPublicKeys InitialOwnerAnchor { get; }
    /// <summary>Public kit identity only; importing the file grants no service membership.</summary>
    public PublicSigningRecipientIdentity Public { get { ObjectDisposedException.ThrowIf(disposed, this); return identity.Public; } }
    internal SigningRecipientIdentity Secret { get { ObjectDisposedException.ThrowIf(disposed, this); return identity; } }

    /// <summary>Creates fresh independent kit material only for explicit current-owner enrollment; persistence/consent/publication remain separate.</summary>
    public static HouseholdRecoveryKit Create(Guid space, Guid enrollment, string owner, LocalSyncPeer peer, HouseholdPublicKeys proposedOwnerRoot)
    {
        Validate(space, enrollment, owner, proposedOwnerRoot);
        ArgumentNullException.ThrowIfNull(peer);
        return new(space, enrollment, owner, peer, proposedOwnerRoot, SigningRecipientIdentity.Create());
    }

    /// <summary>Exports only with an explicit password using the existing maintained backup protection; the password is never retained.</summary>
    public byte[] ExportEncrypted(string password)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (string.IsNullOrEmpty(password) || password.Length < 8) throw new ArgumentException("An explicit recovery-file password of at least eight characters is required.", nameof(password));
        var clear = ExportForProtectedStorage();
        try { return BackupEncryption.Encrypt(clear, password); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Original resumable kit only inside the existing protected local native journal, never a public service DTO.</summary>
    internal byte[] ExportForProtectedStorage()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var secret = identity.ExportForProtectedStorage();
        var frame = new HouseholdRecoveryKitFrame(1, "zanance.household.recovery-kit.v1", SpaceId, EnrollmentId, OwnerId,
            Peer.Address.AbsoluteUri, Peer.Fingerprint, InitialOwnerAnchor, secret);
        byte[]? clear = null;
        try
        {
            clear = JsonSerializer.SerializeToUtf8Bytes(frame, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryKitFrame);
            if (clear.Length > 16384) throw Refusal();
            var result = clear; clear = null; return result;
        }
        finally { CryptographicOperations.ZeroMemory(secret); if (clear is not null) CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Opens only a bounded encrypted kit and checks exact types/algorithms before any local binding or service request.</summary>
    public static HouseholdRecoveryKit ImportEncrypted(ReadOnlySpan<byte> encrypted, string password)
    {
        if (encrypted.Length is < 64 or > 32768 || !BackupEncryption.IsEncrypted(encrypted)) throw Refusal();
        var clear = BackupEncryption.Decrypt(encrypted, password);
        try { return ImportFromProtectedStorage(clear); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Imports only an original bounded protected-native frame; callers may not expose this clear representation.</summary>
    internal static HouseholdRecoveryKit ImportFromProtectedStorage(byte[] clear)
    {
        HouseholdRecoveryKitFrame? frame = null;
        try
        {
            if (clear.Length > 16384) throw Refusal();
            frame = StrictJsonPayload.Deserialize(clear, HouseholdRecoveryPrivateJsonContext.Default.HouseholdRecoveryKitFrame, 16384);
            if (frame.Format != 1 || frame.Kind != "zanance.household.recovery-kit.v1" || frame.PrivateKit is not { Length: > 0 and <= 8192 } ||
                frame.PeerAddress is not { Length: > 0 and <= 2048 } || frame.PeerFingerprint is null) throw Refusal();
            Validate(frame.SpaceId, frame.EnrollmentId, frame.OwnerId, frame.InitialOwnerAnchor);
            var peer = new LocalSyncPeer(frame.PeerAddress, frame.PeerFingerprint);
            var secret = SigningRecipientIdentity.ImportFromProtectedStorage(frame.PrivateKit);
            return new(frame.SpaceId, frame.EnrollmentId, frame.OwnerId, peer, frame.InitialOwnerAnchor, secret);
        }
        finally
        {
            if (frame?.PrivateKit is { } secret) CryptographicOperations.ZeroMemory(secret);
        }
    }

    private static void Validate(Guid space, Guid enrollment, string owner, HouseholdPublicKeys root)
    {
        if (space == Guid.Empty || enrollment == Guid.Empty || string.IsNullOrWhiteSpace(owner) || owner.Length > 128 ||
            root is not { SigningKey.Length: > 0 and <= 512, WrappingKey.Length: > 0 and <= 2048 }) throw Refusal();
        var key = new PublicSigningRecipientIdentity(root.SigningKey, root.WrappingKey);
        using var signing = key.OpenSigningKey(); using var wrapping = key.OpenWrappingKey();
    }
    private static CryptographicException Refusal() => new("The household recovery kit is invalid or unavailable.");
    /// <inheritdoc />
    public override string ToString() => "Household recovery kit";
    /// <inheritdoc />
    public void Dispose() { if (disposed) return; disposed = true; identity.Dispose(); }
}

/// <summary>Private reflection-free kit frame; serialized bytes may leave protected memory only inside password encryption.</summary>
internal sealed record HouseholdRecoveryKitFrame([property: JsonRequired] int Format, [property: JsonRequired] string Kind,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid EnrollmentId, [property: JsonRequired] string OwnerId,
    [property: JsonRequired] string PeerAddress, [property: JsonRequired] string PeerFingerprint,
    [property: JsonRequired] HouseholdPublicKeys InitialOwnerAnchor, [property: JsonRequired] byte[] PrivateKit)
{
    /// <inheritdoc />
    public override string ToString() => "Private household recovery frame";
}

/// <summary>Private local/client serialization only; these types never enter the service's metadata contract.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(HouseholdRecoveryKitFrame))]
[JsonSerializable(typeof(HouseholdPrivateRootFrame))]
[JsonSerializable(typeof(HouseholdRecoveryPreparationFrame))]
[JsonSerializable(typeof(HouseholdOwnerRecoveryFrame))]
internal partial class HouseholdRecoveryPrivateJsonContext : JsonSerializerContext;
