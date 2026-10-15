using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>An authenticated owner's personal space, never a shared household or a local device profile id.</summary>
public sealed class SyncSpaceRow
{
    /// <summary>Server-created identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Authenticated identity owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Framework-protected owner-visible label.</summary>
    public string ProtectedName { get; set; } = "";
    /// <summary>Immutable negotiated financial model contract.</summary>
    public string SchemaHash { get; set; } = "";
    /// <summary>Last committed immutable event sequence.</summary>
    public long Cursor { get; set; }
    /// <summary>No other device can consume a partial initial publication.</summary>
    public bool Initializing { get; set; } = true;
    /// <summary>Actual device allowed to complete the initial staged capture.</summary>
    public Guid BootstrapDeviceId { get; set; }
    /// <summary>Owner-scoped stable creation request identity.</summary>
    public Guid CreationOperationId { get; set; }
}

/// <summary>A personal device grant checked with its actual identity session on every request.</summary>
public sealed class SyncDeviceRow
{
    /// <summary>Server-created public reference.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Actual current authenticated session, independent of client labels.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Framework-protected owner label; no hardware identifier.</summary>
    public string ProtectedName { get; set; } = "";
    /// <summary>Explicit revocation; old queued edits cannot bypass it.</summary>
    public bool Revoked { get; set; }
    /// <summary>Last accepted request time, unrelated to financial dates.</summary>
    public DateTimeOffset LastSeenAt { get; set; }
}

/// <summary>Durable complete-round authority; replay cannot create another posting.</summary>
public sealed class SyncRoundRow
{
    /// <summary>Server staging identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual personal space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Actual device, not just a caller-provided operation id.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Stable device request identity.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Negotiated schema; incompatible clients cannot write.</summary>
    public string SchemaHash { get; set; } = "";
    /// <summary>Atomic outcome, committed with financial rows and events.</summary>
    public bool Committed { get; set; }
    /// <summary>Explicit abandoned operation tombstone; delayed retries cannot revive it.</summary>
    public bool Abandoned { get; set; }
    /// <summary>Durable feed boundary returned after an acknowledgement is lost.</summary>
    public long CommitCursor { get; set; }
    /// <summary>Frozen ordered content digest after successful commit.</summary>
    public string? ContentHash { get; set; }
    /// <summary>Frozen successful completion request, including explicit review choices.</summary>
    public string? CompletionHash { get; set; }
    /// <summary>Staging creation time for bounded operations.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Immutable encrypted staging part, invisible to financial readers before complete publication.</summary>
public sealed class SyncPartRow
{
    /// <summary>Actual owning round.</summary>
    public Guid RoundId { get; set; }
    /// <summary>Ordered bounded index.</summary>
    public int Index { get; set; }
    /// <summary>Standard digest detects altered replay.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Bounded original serialized size.</summary>
    public int Bytes { get; set; }
    /// <summary>Complete proposed row count.</summary>
    public int Rows { get; set; }
    /// <summary>Framework-protected financial proposal body.</summary>
    public string ProtectedBody { get; set; } = "";
}

/// <summary>Current authoritative row or tombstone; stored financial scalars are encrypted.</summary>
public sealed class SyncRecordRow
{
    /// <summary>Actual personal space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Allowlisted table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Canonical original key.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Authentic immutable event version.</summary>
    public long Version { get; set; }
    /// <summary>Protected complete body; null is a durable tombstone.</summary>
    public string? ProtectedBody { get; set; }
}

/// <summary>Authentic immutable history supports baseline comparison and interrupted feed recovery.</summary>
public sealed class SyncEventRow
{
    /// <summary>Actual personal space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Unique monotonically increasing space sequence.</summary>
    public long Sequence { get; set; }
    /// <summary>Allowlisted table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Canonical original key.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Protected original body, not a client-supplied historical baseline.</summary>
    public string? ProtectedBody { get; set; }
}

/// <summary>Space-private bounded content manifest; staged bytes cannot become a financial attachment until verified.</summary>
public sealed class SyncBlobRow
{
    /// <summary>Actual owning space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Standard complete content digest.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Complete expected byte length.</summary>
    public long Length { get; set; }
    /// <summary>Ordered bounded part count.</summary>
    public int Parts { get; set; }
    /// <summary>True only after complete length and digest verification.</summary>
    public bool Complete { get; set; }
}

/// <summary>Immutable protected piece published before its manifest, with no readable partial file.</summary>
public sealed class SyncBlobPieceRow
{
    /// <summary>Actual owning space.</summary>
    public Guid SpaceId { get; set; }
    /// <summary>Expected complete blob digest.</summary>
    public string BlobHash { get; set; } = "";
    /// <summary>Ordered piece number.</summary>
    public int Index { get; set; }
    /// <summary>Server-created filename identity; no caller path is accepted.</summary>
    public Guid FileId { get; set; }
    /// <summary>Actual original byte length.</summary>
    public int Length { get; set; }
    /// <summary>Digest for altered retry and storage-integrity refusal.</summary>
    public string Hash { get; set; } = "";
}

/// <summary>Maps personal ownership, immutable replay/history and private transfer metadata.</summary>
internal static class SyncServiceModel
{
    /// <summary>Extends only the independent service database, never mobile identity or purchase enforcement.</summary>
    internal static void Configure(ModelBuilder model)
    {
        var spaces = model.Entity<SyncSpaceRow>(); spaces.ToTable("SyncSpaces"); spaces.HasKey(row => row.Id);
        spaces.Property(row => row.UserId).HasMaxLength(450); spaces.Property(row => row.ProtectedName).HasMaxLength(2048);
        spaces.Property(row => row.SchemaHash).HasMaxLength(64);
        spaces.HasOne<ServiceUser>().WithMany().HasForeignKey(row => row.UserId).OnDelete(DeleteBehavior.Restrict);
        spaces.HasIndex(row => row.UserId);
        spaces.HasIndex(row => new { row.UserId, row.CreationOperationId }).IsUnique();
        var devices = model.Entity<SyncDeviceRow>(); devices.ToTable("SyncDevices"); devices.HasKey(row => row.Id);
        devices.Property(row => row.UserId).HasMaxLength(450); devices.Property(row => row.ProtectedName).HasMaxLength(2048);
        devices.HasOne<ServiceUser>().WithMany().HasForeignKey(row => row.UserId).OnDelete(DeleteBehavior.Restrict);
        // Identity sessions expire independently. Retain revoked device/round history after session cleanup;
        // every request still resolves the actual live session before accepting a device reference.
        devices.HasIndex(row => new { row.UserId, row.Revoked });
        devices.HasIndex(row => row.SessionId).IsUnique();
        var rounds = model.Entity<SyncRoundRow>(); rounds.ToTable("SyncRounds"); rounds.HasKey(row => row.Id);
        rounds.HasOne<SyncSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Cascade);
        rounds.HasOne<SyncDeviceRow>().WithMany().HasForeignKey(row => row.DeviceId).OnDelete(DeleteBehavior.Restrict);
        rounds.HasIndex(row => new { row.SpaceId, row.DeviceId, row.OperationId }).IsUnique();
        rounds.Property(row => row.SchemaHash).HasMaxLength(64); rounds.Property(row => row.ContentHash).HasMaxLength(64);
        rounds.Property(row => row.CompletionHash).HasMaxLength(64);
        var parts = model.Entity<SyncPartRow>(); parts.ToTable("SyncParts"); parts.HasKey(row => new { row.RoundId, row.Index });
        parts.HasOne<SyncRoundRow>().WithMany().HasForeignKey(row => row.RoundId).OnDelete(DeleteBehavior.Cascade);
        parts.Property(row => row.Hash).HasMaxLength(64);
        var records = model.Entity<SyncRecordRow>(); records.ToTable("SyncRecords");
        records.HasKey(row => new { row.SpaceId, row.TableName, row.RecordKey });
        records.HasOne<SyncSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Cascade);
        records.Property(row => row.TableName).HasMaxLength(128); records.Property(row => row.RecordKey).HasMaxLength(2048);
        var events = model.Entity<SyncEventRow>(); events.ToTable("SyncEvents"); events.HasKey(row => new { row.SpaceId, row.Sequence });
        events.HasOne<SyncSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Cascade);
        events.Property(row => row.TableName).HasMaxLength(128); events.Property(row => row.RecordKey).HasMaxLength(2048);
        events.HasIndex(row => new { row.SpaceId, row.TableName, row.RecordKey, row.Sequence });
        var blobs = model.Entity<SyncBlobRow>(); blobs.ToTable("SyncBlobs"); blobs.HasKey(row => new { row.SpaceId, row.Hash });
        blobs.HasOne<SyncSpaceRow>().WithMany().HasForeignKey(row => row.SpaceId).OnDelete(DeleteBehavior.Cascade);
        blobs.Property(row => row.Hash).HasMaxLength(64);
        var pieces = model.Entity<SyncBlobPieceRow>(); pieces.ToTable("SyncBlobPieces"); pieces.HasKey(row => new { row.SpaceId, row.BlobHash, row.Index });
        pieces.HasOne<SyncBlobRow>().WithMany().HasForeignKey(row => new { row.SpaceId, row.BlobHash }).OnDelete(DeleteBehavior.Cascade);
        pieces.HasIndex(row => row.FileId).IsUnique(); pieces.Property(row => row.BlobHash).HasMaxLength(64); pieces.Property(row => row.Hash).HasMaxLength(64);
    }
}
