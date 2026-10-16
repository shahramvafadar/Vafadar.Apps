using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Exact captured protected file/setup/peer/identity; a restored or switched profile cannot adopt another queue's private authority.</summary>
public sealed record HouseholdJournalAuthority(string GenerationPath, Guid SetupOperation, string PeerKey, string IdentityId)
{
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdJournalAuthority);
}

/// <summary>Public-only owned local setup observation; private material is read through separate disposable key handles.</summary>
public sealed record HouseholdJournalState(HouseholdJournalAuthority Authority, bool OwnerCreation, Guid SpaceId, Guid DeviceId,
    PublicSigningRecipientIdentity PinnedRoot, string? SignedInvitation, string DeviceFingerprint, long MembershipVersion,
    string MembershipHead, long Cursor, long FinancialCursor, string PublicationHead, bool SendingDisabled);

/// <summary>Owned imported signing handles from captured protected storage; no private byte array or credential is serialized by this object.</summary>
public sealed class HouseholdJournalKeys : IDisposable
{
    internal HouseholdJournalKeys(SigningRecipientIdentity device, SigningRecipientIdentity? owner) { Device = device; Owner = owner; }
    /// <summary>Only this exact local device's existing key; missing storage never generates a replacement.</summary>
    public SigningRecipientIdentity Device { get; }
    /// <summary>Current owner root only when its exact verified public identity has an existing owned private frame.</summary>
    public SigningRecipientIdentity? Owner { get; }
    /// <inheritdoc />
    public void Dispose() { Device.Dispose(); Owner?.Dispose(); }
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdJournalKeys);
}

/// <summary>Protected original SQLite household lifecycle/ciphertext journal. Construction performs no database or network work.</summary>
public sealed partial class HouseholdJournal(IDbContextFactory<ZananceDbContext> factory, TimeProvider? time = null)
{
    private readonly HouseholdCryptography cryptography = new(time ?? TimeProvider.System);

    /// <summary>Persists the original generated keys/setup operation before reservation/claim; another call cannot replace a retained original key.</summary>
    public async Task<HouseholdJournalState> BeginDraftAsync(LocalSyncPeer peer, string actualIdentity,
        PublicSigningRecipientIdentity independentlyPinnedRoot, bool ownerCreation, SigningRecipientIdentity device,
        SigningRecipientIdentity? ownerRoot, string? signedInvitation, CancellationToken ct = default, bool requireEmptyFinancial = false,
        string? expectedGenerationPath = null, HouseholdProjectionReview? selectedSource = null)
    {
        if (string.IsNullOrWhiteSpace(actualIdentity) || actualIdentity.Length > 128 || ownerCreation != (ownerRoot is not null) ||
            ownerCreation == (signedInvitation is not null) || ownerRoot is not null && ownerRoot.Public != independentlyPinnedRoot)
            throw Refusal();
        using var signing = independentlyPinnedRoot.OpenSigningKey(); using var wrapping = independentlyPinnedRoot.OpenWrappingKey();
        if (independentlyPinnedRoot.Fingerprint == device.Public.Fingerprint) throw Refusal();
        await using var context = await OpenAsync(ct); var db = context.Db;
        if (expectedGenerationPath is not null && context.Path != Path.GetFullPath(expectedGenerationPath)) throw Refusal();
        var existing = await db.Set<HouseholdLocalBinding>().SingleOrDefaultAsync(ct);
        var peerKey = peer.BindingKey(actualIdentity);
        if (existing is not null)
        {
            if (existing.GenerationPath != context.Path || existing.PeerKey != peerKey || existing.IdentityId != actualIdentity ||
                existing.OwnerCreation != ownerCreation || existing.DeviceFingerprint != device.Public.Fingerprint ||
                existing.PinnedSigningKey != independentlyPinnedRoot.SigningKey || existing.PinnedWrappingKey != independentlyPinnedRoot.WrappingKey ||
                existing.SignedInvitation != signedInvitation) throw Refusal();
            if (selectedSource is not null)
            {
                selectedSource.Check();
                if (existing.ProjectionReviewId != selectedSource.Id || existing.ProjectionSourceDigest != selectedSource.SourceDigest) throw Refusal();
            }
            return State(existing);
        }
        if (await db.Set<SyncBindingRow>().AnyAsync(ct) || await db.Set<HouseholdLocalSecret>().AnyAsync(ct) ||
            await db.Set<HouseholdLocalMembership>().AnyAsync(ct) || await db.Set<HouseholdLocalAttempt>().AnyAsync(ct)) throw Refusal();
        if (requireEmptyFinancial)
        {
            // App setup is only for an independent empty destination. Selected source copying has
            // its own immutable review/atomic receipt and cannot be replaced by adopting this ledger.
            foreach (var table in Core.Sharing.HouseholdFinancialSchema.Manifest.Tables.Where(table => table.Name is not ("Settings" or "Categories")))
            {
                using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                // Identifiers come only from the embedded shipped manifest; SQL parameters cannot name a table.
                command.CommandText = $"SELECT EXISTS(SELECT 1 FROM \"{table.Name}\" LIMIT 1)";
                if (Convert.ToInt64(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) != 0) throw Refusal();
            }
            if (await db.Categories.AnyAsync(row => row.SystemKey == null || row.Name != null || row.IsArchived, ct)) throw Refusal();
        }
        var binding = new HouseholdLocalBinding { SetupOperation = Guid.NewGuid(), GenerationPath = context.Path, PeerKey = peerKey,
            IdentityId = actualIdentity, OwnerCreation = ownerCreation, PinnedSigningKey = independentlyPinnedRoot.SigningKey,
            PinnedWrappingKey = independentlyPinnedRoot.WrappingKey, SignedInvitation = signedInvitation, DeviceFingerprint = device.Public.Fingerprint };
        if (selectedSource is not null)
        {
            selectedSource.Check();
            if (!ownerCreation || !requireEmptyFinancial || !selectedSource.Rechecked || context.Path == selectedSource.SourcePath) throw Refusal();
            SetProjectionIntent(binding, selectedSource);
        }
        db.Add(binding); var frames = new List<byte[]>();
        try
        {
            var deviceFrame = device.ExportForProtectedStorage(); frames.Add(deviceFrame); db.Add(new HouseholdLocalSecret { Slot = "device", Frame = deviceFrame });
            if (ownerRoot is not null)
            { var ownerFrame = ownerRoot.ExportForProtectedStorage(); frames.Add(ownerFrame); db.Add(new HouseholdLocalSecret { Slot = RootSlot(ownerRoot.Public.Fingerprint), Frame = ownerFrame }); }
            await context.CommitAsync(ct); return State(binding);
        }
        finally { foreach (var frame in frames) CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Captures only the actual protected peer/identity file; absent setup is not repaired or replaced automatically.</summary>
    public async Task<HouseholdJournalState?> CaptureAsync(LocalSyncPeer peer, string actualIdentity, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var row = await context.Db.Set<HouseholdLocalBinding>().AsNoTracking().SingleOrDefaultAsync(ct);
        if (row is null) return null;
        if (row.GenerationPath != context.Path || row.IdentityId != actualIdentity || row.PeerKey != peer.BindingKey(actualIdentity)) throw Refusal();
        context.Check(); return State(row);
    }

    /// <summary>Records only original acknowledged server ids; altered reservation/claim retries cannot redirect retained keys.</summary>
    public async Task BindRemoteAsync(HouseholdJournalAuthority authority, Guid space, Guid device, CancellationToken ct = default)
    {
        if (space == Guid.Empty || device == Guid.Empty) throw Refusal();
        await using var context = await OpenAsync(ct); var row = await RequireBindingAsync(context, authority, ct);
        if (row.ProjectionReviewId is not null && ProjectionReceipt(row)?.Completed != true) throw Refusal();
        if (row.SpaceId != Guid.Empty && row.SpaceId != space || row.DeviceId != Guid.Empty && row.DeviceId != device) throw Refusal();
        row.SpaceId = space; row.DeviceId = device; await context.CommitAsync(ct);
    }

    /// <summary>Reopens only existing original device/current-owner handles; missing/corrupt frames never cause replacement generation.</summary>
    public async Task<HouseholdJournalKeys> ReadKeysAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var row = await RequireBindingAsync(context, authority, ct);
        var device = await ReadIdentityAsync(context.Db, "device", ct);
        SigningRecipientIdentity? owner = null;
        try
        {
            if (device.Public.Fingerprint != row.DeviceFingerprint) throw Refusal();
            var root = row.MembershipVersion == 0 ? new PublicSigningRecipientIdentity(row.PinnedSigningKey, row.PinnedWrappingKey)
                : (await ReadMembershipAsync(context.Db, row, device, ct)).Current.Root;
            var slot = RootSlot(root.Fingerprint);
            if (await context.Db.Set<HouseholdLocalSecret>().AnyAsync(secret => secret.Slot == slot, ct))
            {
                owner = await ReadIdentityAsync(context.Db, slot, ct);
                if (owner.Public != root) { owner.Dispose(); throw Refusal(); }
            }
            context.Check(); return new(device, owner);
        }
        catch { device.Dispose(); owner?.Dispose(); throw; }
    }

    /// <summary>Atomically persists only a fully independently verified original chain, preserving exact previous signed bytes.</summary>
    public async Task<HouseholdVerifiedMembership> RecordMembershipAsync(HouseholdJournalAuthority authority,
        IReadOnlyList<HouseholdMembershipProof> completeHistory, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var verified = VerifyMembership(binding, completeHistory, device.Public);
        var existing = await context.Db.Set<HouseholdLocalMembership>().OrderBy(row => row.Version).ToArrayAsync(ct);
        if (completeHistory.Count < existing.Length) throw Refusal();
        for (var index = 0; index < completeHistory.Count; index++)
        {
            var json = SerializeProof(completeHistory[index]); var hash = HouseholdTrustChain.Digest(completeHistory[index].SignedStatement);
            if (index < existing.Length)
            { if (existing[index].Version != index + 1 || existing[index].ProofJson != json || existing[index].Hash != hash) throw Refusal(); }
            else context.Db.Add(new HouseholdLocalMembership { Version = index + 1, Hash = hash, ProofJson = json });
        }
        if (binding.MembershipVersion != verified.Current.Version || binding.MembershipHead != verified.Current.Hash)
        {
            // A local signed proposal is not authenticated host acceptance. A new roster/epoch
            // must obtain its own actual checkpoint and current grants before financial writes.
            binding.AccessObservedAtUnixSeconds = null; binding.RotationRequired = true;
        }
        binding.MembershipVersion = verified.Current.Version; binding.MembershipHead = verified.Current.Hash;
        var own = verified.Current.Snapshot().Members.SingleOrDefault(member => member.IdentityId == binding.IdentityId);
        if (own is null || !own.Devices.Any(item => item.Id == binding.DeviceId && item.Keys == new HouseholdPublicKeys(device.Public.SigningKey, device.Public.WrappingKey)))
            binding.RemoteAccessRefused = true;
        await context.CommitAsync(ct); return verified.Current;
    }

    /// <summary>Validates both original epoch commitment and actual granting roster before storing an imported key in the protected file.</summary>
    public async Task RecordEpochGrantAsync(HouseholdJournalAuthority authority, HouseholdDeviceGrant grant, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (grant.DeviceId != binding.DeviceId || !membership.ByVersion.TryGetValue(grant.MembershipVersion, out var approval) ||
            !membership.ByEpoch.TryGetValue(grant.Epoch, out var origin) || HouseholdPublicationDigest.Ciphertext(grant.Ciphertext) != grant.Digest) throw Refusal();
        using var material = approval.OpenHistoricalGrant(cryptography, origin, grant.Ciphertext, binding.DeviceId, device);
        var frame = material.ExportForProtectedStorage();
        try { await AddImmutableSecretAsync(context.Db, EpochSlot(material.Epoch), frame, ct); await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Stores an original newly generated owner epoch after checking the complete already-verified local roster commitments.</summary>
    public async Task RecordOwnedEpochAsync(HouseholdJournalAuthority authority, HouseholdEpochMaterial material, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        if (!membership.ByEpoch.TryGetValue(material.Epoch, out var origin) || origin.Snapshot().KeyCommitment != material.KeyCommitment ||
            origin.Snapshot().AddressKeyCommitment != material.AddressKeyCommitment || origin.Snapshot().OwnerId != binding.IdentityId) throw Refusal();
        var frame = material.ExportForProtectedStorage();
        try { await AddImmutableSecretAsync(context.Db, EpochSlot(material.Epoch), frame, ct); await context.CommitAsync(ct); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }

    /// <summary>Loads only existing exact bound epoch material, never a new key after loss or corruption.</summary>
    public async Task<HouseholdEpochMaterial> ReadEpochAsync(HouseholdJournalAuthority authority, long epoch, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct); var membership = await ReadMembershipAsync(context.Db, binding, device, ct);
        var result = await ReadEpochCoreAsync(context.Db, membership, epoch, ct);
        try { context.Check(); return result; } catch { result.Dispose(); throw; }
    }

    /// <summary>Validates full captured native ownership before opening any private-material reader or writer.</summary>
    private async Task<JournalContext> OpenAsync(CancellationToken ct)
    {
        var db = await factory.CreateDbContextAsync(ct);
        try
        {
            if (db.Database.GetDbConnection() is not SqliteConnection connection) throw Refusal();
            ProtectedSqliteAccess.Require(connection);
            await db.Database.OpenConnectionAsync(ct);
            var transaction = await db.Database.BeginTransactionAsync(ct);
            return new(db, transaction, Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db)), connection);
        }
        catch { await db.DisposeAsync(); throw; }
    }

    /// <summary>Actual native factory/generation check surrounds persistence and transaction publication, without retaining a key-bearing global connection.</summary>
    private sealed class JournalContext(ZananceDbContext db, IDbContextTransaction transaction, string path, SqliteConnection connection) : IAsyncDisposable
    {
        internal ZananceDbContext Db { get; } = db;
        internal string Path { get; } = path;
        internal void Check() => ProtectedSqliteAccess.Require(connection);
        internal async Task CommitAsync(CancellationToken ct)
        { Check(); await Db.SaveChangesAsync(ct); Check(); await transaction.CommitAsync(ct); }
        public async ValueTask DisposeAsync() { await transaction.DisposeAsync(); await Db.DisposeAsync(); }
    }

    /// <summary>Captured setup/file/peer/identity are all rechecked inside the actual private transaction.</summary>
    private static async Task<HouseholdLocalBinding> RequireBindingAsync(JournalContext context, HouseholdJournalAuthority authority, CancellationToken ct)
    {
        context.Check(); var row = await context.Db.Set<HouseholdLocalBinding>().SingleOrDefaultAsync(ct) ?? throw Refusal();
        if (context.Path != authority.GenerationPath || row.GenerationPath != context.Path || row.SetupOperation != authority.SetupOperation ||
            row.PeerKey != authority.PeerKey || row.IdentityId != authority.IdentityId) throw Refusal();
        return row;
    }

    /// <summary>Owned imported private buffers are cleared after creating native key handles.</summary>
    private static async Task<SigningRecipientIdentity> ReadIdentityAsync(ZananceDbContext db, string slot, CancellationToken ct)
    {
        var row = await db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == slot, ct) ?? throw Refusal();
        try { return SigningRecipientIdentity.ImportFromProtectedStorage(row.Frame); }
        finally { CryptographicOperations.ZeroMemory(row.Frame); }
    }
    /// <summary>Lost acknowledgements retain the exact old material; altered key replay cannot overwrite it.</summary>
    private static async Task AddImmutableSecretAsync(ZananceDbContext db, string slot, byte[] frame, CancellationToken ct)
    {
        var prior = await db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == slot, ct);
        if (prior is not null)
        { try { if (!CryptographicOperations.FixedTimeEquals(prior.Frame, frame)) throw Refusal(); } finally { CryptographicOperations.ZeroMemory(prior.Frame); } }
        else db.Add(new HouseholdLocalSecret { Slot = slot, Frame = frame });
    }
    /// <summary>Standard private epoch import is bound to its exact verified historical public commitments.</summary>
    private static async Task<HouseholdEpochMaterial> ReadEpochCoreAsync(ZananceDbContext db, MembershipState membership, long epoch, CancellationToken ct)
    {
        if (!membership.ByEpoch.TryGetValue(epoch, out var origin)) throw Refusal();
        var row = await db.Set<HouseholdLocalSecret>().AsNoTracking().SingleOrDefaultAsync(row => row.Slot == EpochSlot(epoch), ct) ?? throw Refusal();
        HouseholdEpochMaterial result;
        try { result = HouseholdEpochMaterial.ImportFromProtectedStorage(row.Frame); } finally { CryptographicOperations.ZeroMemory(row.Frame); }
        if (result.Epoch != epoch || result.KeyCommitment != origin.Snapshot().KeyCommitment || result.AddressKeyCommitment != origin.Snapshot().AddressKeyCommitment)
        { result.Dispose(); throw Refusal(); }
        return result;
    }
    /// <summary>Internal version-indexed verified authority, never reconstructed from an unverified service directory.</summary>
    private sealed record MembershipState(HouseholdVerifiedMembership Current, IReadOnlyDictionary<long, HouseholdVerifiedMembership> ByVersion,
        IReadOnlyDictionary<long, HouseholdVerifiedMembership> ByEpoch);
    /// <summary>Public-only local state has no private frame or authentication credential.</summary>
    private static HouseholdJournalState State(HouseholdLocalBinding row) => new(new(row.GenerationPath, row.SetupOperation, row.PeerKey, row.IdentityId),
        row.OwnerCreation, row.SpaceId, row.DeviceId, new(row.PinnedSigningKey, row.PinnedWrappingKey), row.SignedInvitation, row.DeviceFingerprint,
        row.MembershipVersion, row.MembershipHead, row.Cursor, row.FinancialCursor, row.PublicationHead, row.SendingDisabled || row.RemoteAccessRefused);
    private static string EpochSlot(long epoch) => epoch >= 1 ? "epoch:" + epoch.ToString(System.Globalization.CultureInfo.InvariantCulture) : throw Refusal();
    private static string RootSlot(string fingerprint) => HouseholdProtectionContext.IsFingerprint(fingerprint) ? "root:" + fingerprint : throw Refusal();
    /// <summary>Content-free failure without file credentials, native errors, private bytes or financial content.</summary>
    private static CryptographicException Refusal() => new("Household protected journal authority was refused.");
}
